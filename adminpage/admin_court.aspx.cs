using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_court : System.Web.UI.Page
    {
        private readonly string connString =
            ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        private const int SYSTEM_STAFF_ID = 1;
        private static readonly TimeSpan DEFAULT_OPEN_TIME = new TimeSpan(8, 0, 0);
        private static readonly TimeSpan DEFAULT_CLOSE_TIME = new TimeSpan(22, 0, 0);
        private const int SLOT_MINUTES = 30;
        private const int WINDOW_DAYS_AHEAD = 7;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                PopulateTimeDropdowns();

                EnsureAvailabilityWindow();
                LoadCourtDashboard(DateTime.Now);
            }
        }

        private int GetCurrentStaffId()
        {
            int staffId;
            if (Session["StaffID"] != null &&
                int.TryParse(Convert.ToString(Session["StaffID"]), out staffId) &&
                staffId > 0)
            {
                return staffId;
            }

            return SYSTEM_STAFF_ID;
        }

        private void PopulateTimeDropdowns()
        {
            ddlTime.Items.Clear();

            for (TimeSpan t = DEFAULT_OPEN_TIME; t <= DEFAULT_CLOSE_TIME; t = t.Add(TimeSpan.FromMinutes(SLOT_MINUTES)))
            {
                DateTime dt = DateTime.Today.Add(t);
                string value = t.ToString(@"hh\:mm\:ss");
                string text = dt.ToString("h:mm tt");

                ddlTime.Items.Add(new ListItem(text, value));
            }

            TimeSpan nowSlot = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute < 30 ? 0 : 30, 0);
            ListItem li = ddlTime.Items.FindByValue(nowSlot.ToString(@"hh\:mm\:ss"));
            if (li != null)
                ddlTime.SelectedValue = li.Value;
        }

        protected void CourtRefreshTimer_Tick(object sender, EventArgs e)
        {
            EnsureAvailabilityWindow();
            LoadCourtDashboard(DateTime.Now);
            upDashboard.Update();
        }

        protected void btnApplyTime_Click(object sender, EventArgs e)
        {
            DateTime selectedDate;
            TimeSpan selectedTime;

            if (!DateTime.TryParse(txtDate.Text, out selectedDate))
                selectedDate = DateTime.Today;

            if (!TimeSpan.TryParse(ddlTime.SelectedValue, out selectedTime))
                selectedTime = DateTime.Now.TimeOfDay;

            CourtRefreshTimer.Enabled = false;
            EnsureAvailabilityWindow();
            LoadCourtDashboard(selectedDate.Date.Add(selectedTime));
            upDashboard.Update();
        }

        protected void btnLiveView_Click(object sender, EventArgs e)
        {
            txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");

            TimeSpan nowSlot = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute < 30 ? 0 : 30, 0);
            ListItem li = ddlTime.Items.FindByValue(nowSlot.ToString(@"hh\:mm\:ss"));
            if (li != null)
                ddlTime.SelectedValue = li.Value;

            CourtRefreshTimer.Enabled = true;
            EnsureAvailabilityWindow();
            LoadCourtDashboard(DateTime.Now);
            upDashboard.Update();
        }

        /* =========================================================
           SHARED AVAILABILITY ENGINE
           ========================================================= */

        private void EnsureAvailabilityWindow()
        {
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(WINDOW_DAYS_AHEAD);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        DeleteExpiredAvailability(conn, tx, startDate);
                        EnsureBaseSlots(conn, tx, startDate, endDate);
                        ResetSystemSlotsToPlayForAll(conn, tx, startDate, endDate);
                        ApplyApprovedReservations(conn, tx, startDate, endDate);
                        ApplyActiveSessionQueue(conn, tx, startDate, endDate);
                        ApplyActiveSessionClosed(conn, tx, startDate, endDate);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private void DeleteExpiredAvailability(SqlConnection conn, SqlTransaction tx, DateTime minDate)
        {
            string sql = @"
DELETE FROM tblCourtAvailability
WHERE [Date] < @MinDate;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@MinDate", minDate.Date);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureBaseSlots(SqlConnection conn, SqlTransaction tx, DateTime startDate, DateTime endDate)
        {
            List<int> activeCourtIds = new List<int>();

            string getCourtsSql = @"
SELECT CourtID
FROM tblCourt
WHERE IsActive = 1;";

            using (SqlCommand cmd = new SqlCommand(getCourtsSql, conn, tx))
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    activeCourtIds.Add(Convert.ToInt32(rdr["CourtID"]));
                }
            }

            for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                foreach (int courtId in activeCourtIds)
                {
                    for (TimeSpan t = DEFAULT_OPEN_TIME; t < DEFAULT_CLOSE_TIME; t = t.Add(TimeSpan.FromMinutes(SLOT_MINUTES)))
                    {
                        TimeSpan end = t.Add(TimeSpan.FromMinutes(SLOT_MINUTES));

                        string insertSql = @"
IF NOT EXISTS
(
    SELECT 1
    FROM tblCourtAvailability
    WHERE CourtID = @CourtID
      AND [Date] = @Date
      AND StartTime = @StartTime
      AND EndTime = @EndTime
)
BEGIN
    INSERT INTO tblCourtAvailability
    (
        CourtID,
        [Date],
        StartTime,
        EndTime,
        ModeName,
        CreatedByStaffID
    )
    VALUES
    (
        @CourtID,
        @Date,
        @StartTime,
        @EndTime,
        'PlayForAll',
        @SystemStaffID
    )
END";

                        using (SqlCommand cmd = new SqlCommand(insertSql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@CourtID", courtId);
                            cmd.Parameters.AddWithValue("@Date", date.Date);
                            cmd.Parameters.AddWithValue("@StartTime", t);
                            cmd.Parameters.AddWithValue("@EndTime", end);
                            cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void ResetSystemSlotsToPlayForAll(SqlConnection conn, SqlTransaction tx, DateTime startDate, DateTime endDate)
        {
            string sql = @"
UPDATE tblCourtAvailability
SET ModeName = 'PlayForAll'
WHERE [Date] >= @StartDate
  AND [Date] <= @EndDate
  AND CreatedByStaffID = @SystemStaffID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyApprovedReservations(SqlConnection conn, SqlTransaction tx, DateTime startDate, DateTime endDate)
        {
            string sql = @"
UPDATE ca
SET ca.ModeName = 'Reservation'
FROM tblCourtAvailability ca
INNER JOIN tblReservation r
    ON r.CourtID = ca.CourtID
   AND r.ResDate = ca.[Date]
   AND ca.StartTime >= r.StartTime
   AND ca.EndTime <= r.EndTime
WHERE ca.[Date] >= @StartDate
  AND ca.[Date] <= @EndDate
  AND r.ReservationStatusName = 'Approved'
  AND ca.CreatedByStaffID = @SystemStaffID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyActiveSessionQueue(SqlConnection conn, SqlTransaction tx, DateTime startDate, DateTime endDate)
        {
            string sql = @"
UPDATE ca
SET ca.ModeName = 'Queue'
FROM tblCourtAvailability ca
INNER JOIN tblActiveSession s
    ON s.CourtID = ca.CourtID
   AND ca.[Date] = CAST(s.StartTime AS DATE)
   AND ca.StartTime >= CAST(s.StartTime AS TIME)
   AND ca.EndTime <= CAST(
        CASE
            WHEN s.ActualEndTime IS NOT NULL THEN s.ActualEndTime
            ELSE s.ExpectedEndTime
        END AS TIME
   )
WHERE ca.[Date] >= @StartDate
  AND ca.[Date] <= @EndDate
  AND s.StatusName = 'Active'
  AND s.QueueID IS NOT NULL
  AND ca.CreatedByStaffID = @SystemStaffID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        private void ApplyActiveSessionClosed(SqlConnection conn, SqlTransaction tx, DateTime startDate, DateTime endDate)
        {
            string sql = @"
UPDATE ca
SET ca.ModeName = 'Closed'
FROM tblCourtAvailability ca
INNER JOIN tblActiveSession s
    ON s.CourtID = ca.CourtID
   AND ca.[Date] = CAST(s.StartTime AS DATE)
   AND ca.StartTime >= CAST(s.StartTime AS TIME)
   AND ca.EndTime <= CAST(
        CASE
            WHEN s.ActualEndTime IS NOT NULL THEN s.ActualEndTime
            ELSE s.ExpectedEndTime
        END AS TIME
   )
WHERE ca.[Date] >= @StartDate
  AND ca.[Date] <= @EndDate
  AND s.StatusName = 'Active'
  AND s.QueueID IS NULL
  AND s.ReservationID IS NULL
  AND s.PAYCID IS NULL
  AND ca.CreatedByStaffID = @SystemStaffID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        /* =========================================================
           LIVE COURT DASHBOARD
           ========================================================= */

        private void LoadCourtDashboard(DateTime targetTime)
        {
            EnsureAvailabilityWindow();

            List<CourtDisplay> dashboardData = new List<CourtDisplay>();
            List<CourtSeed> courts = new List<CourtSeed>();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string sql = @"
SELECT CourtID, CourtNumber, SportName
FROM tblCourt
WHERE IsActive = 1
ORDER BY CourtNumber;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        courts.Add(new CourtSeed
                        {
                            CourtID = Convert.ToInt32(rdr["CourtID"]),
                            CourtNumber = Convert.ToInt32(rdr["CourtNumber"]),
                            SportName = Convert.ToString(rdr["SportName"])
                        });
                    }
                }

                foreach (CourtSeed court in courts)
                {
                    dashboardData.Add(GetSmartCourtStatus(conn, court.CourtID, court.CourtNumber, court.SportName, targetTime));
                }
            }

            rptCourts.DataSource = dashboardData;
            rptCourts.DataBind();
        }

        private CourtDisplay GetSmartCourtStatus(SqlConnection conn, int courtId, int courtNumber, string sportName, DateTime targetTime)
        {
            CourtDisplay display = new CourtDisplay
            {
                CourtID = courtId,
                CourtNumber = courtNumber,
                SportName = sportName,
                CurrentMode = "PlayForAll",
                StatusCssClass = "available",
                CurrentPlayerName = "Open / No Assigned Player",
                CurrentTimeRange = FormatTimeRange(targetTime.TimeOfDay, targetTime.TimeOfDay.Add(TimeSpan.FromMinutes(SLOT_MINUTES))),
                NextPlayerName = "None",
                NextTimeRange = "--"
            };

            DashboardSlotState slotState = GetDashboardSlotState(conn, courtId, targetTime);

            if (slotState == null)
            {
                display.CurrentMode = "Closed";
                display.StatusCssClass = "closed";
                display.CurrentPlayerName = "Outside Hours";
                display.CurrentTimeRange = "--";
                display.NextPlayerName = "None";
                display.NextTimeRange = "--";
                return display;
            }

            display.CurrentMode = slotState.ModeName;
            display.SourceLabel = slotState.SourceLabel;
            display.CurrentTimeRange = slotState.TimeRange;

            if (slotState.HasActiveSession)
            {
                display.StatusCssClass = slotState.ModeName == "Closed" ? "closed" : "occupied";
                display.CurrentPlayerName = string.IsNullOrWhiteSpace(slotState.CurrentPlayerName)
                    ? "Active Session"
                    : slotState.CurrentPlayerName;
            }
            else
            {
                switch (slotState.ModeName)
                {
                    case "Closed":
                        display.StatusCssClass = "closed";
                        display.CurrentPlayerName = "Court Closed";
                        break;

                    case "Reservation":
                        display.StatusCssClass = "available";
                        display.CurrentPlayerName = string.IsNullOrWhiteSpace(slotState.CurrentPlayerName)
                            ? "Reserved Player"
                            : slotState.CurrentPlayerName;
                        break;

                    case "Queue":
                        display.StatusCssClass = "available";
                        display.CurrentPlayerName = "Open for Queue";
                        break;

                    default:
                        display.StatusCssClass = "available";
                        display.CurrentPlayerName = "Open / No Assigned Player";
                        break;
                }
            }

            FillNextScheduledInfo(conn, courtId, targetTime, display);

            return display;
        }

        private DashboardSlotState GetDashboardSlotState(SqlConnection conn, int courtId, DateTime targetTime)
        {
            string sql = @"
SELECT TOP 1
    ca.AvailabilityID,
    ca.ModeName,
    ca.StartTime,
    ca.EndTime,
    ca.CreatedByStaffID,

    r.ReservationID,
    s.SessionID,
    s.QueueID,
    s.ReservationID AS SessionReservationID,
    s.PAYCID,

    COALESCE(
        paRes.Firstname + ' ' + paRes.Lastname,
        pwRes.Firstname + ' ' + pwRes.Lastname,
        paQ.Firstname + ' ' + paQ.Lastname,
        pwQ.Firstname + ' ' + pwQ.Lastname,
        pwP.Firstname + ' ' + pwP.Lastname,
        ''
    ) AS CurrentPlayerName

FROM tblCourtAvailability ca
LEFT JOIN tblReservation r
    ON r.CourtID = ca.CourtID
   AND r.ResDate = ca.[Date]
   AND ca.StartTime >= r.StartTime
   AND ca.EndTime <= r.EndTime
   AND r.ReservationStatusName = 'Approved'

LEFT JOIN tblPlayerAccount paRes
    ON r.UserID = paRes.UserID
LEFT JOIN tblPlayerWalkIn pwRes
    ON r.WalkInID = pwRes.WalkInID

LEFT JOIN tblActiveSession s
    ON s.CourtID = ca.CourtID
   AND s.StatusName = 'Active'
   AND s.StartTime <= @TargetDateTime
   AND (s.ActualEndTime IS NULL OR s.ActualEndTime > @TargetDateTime)
   AND ca.[Date] = CAST(s.StartTime AS DATE)
   AND ca.StartTime >= CAST(s.StartTime AS TIME)
   AND ca.EndTime <= CAST(
        CASE
            WHEN s.ActualEndTime IS NOT NULL THEN s.ActualEndTime
            ELSE s.ExpectedEndTime
        END AS TIME
   )

LEFT JOIN tblCourtQueue q
    ON s.QueueID = q.QueueID
LEFT JOIN tblPlayerAccount paQ
    ON q.UserID = paQ.UserID
LEFT JOIN tblPlayerWalkIn pwQ
    ON q.WalkInID = pwQ.WalkInID

LEFT JOIN tblPlayAllYouCanRegistry p
    ON s.PAYCID = p.PAYCID
LEFT JOIN tblPlayerWalkIn pwP
    ON p.WalkInID = pwP.WalkInID

WHERE ca.CourtID = @CourtID
  AND ca.[Date] = @TargetDate
  AND ca.StartTime <= @TargetTime
  AND ca.EndTime > @TargetTime
ORDER BY ca.StartTime DESC;";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId);
                cmd.Parameters.AddWithValue("@TargetDate", targetTime.Date);
                cmd.Parameters.AddWithValue("@TargetTime", targetTime.TimeOfDay);
                cmd.Parameters.AddWithValue("@TargetDateTime", targetTime);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                        return null;

                    string modeName = Convert.ToString(rdr["ModeName"]);
                    bool hasSession = rdr["SessionID"] != DBNull.Value;
                    bool hasReservation = rdr["ReservationID"] != DBNull.Value;
                    bool isQueueSession = rdr["QueueID"] != DBNull.Value;
                    bool isReservationSession = rdr["SessionReservationID"] != DBNull.Value;
                    bool isPaycSession = rdr["PAYCID"] != DBNull.Value;

                    string sourceLabel;
                    if (hasSession && isQueueSession)
                        sourceLabel = "Active Queue";
                    else if (hasSession && isReservationSession)
                        sourceLabel = "Active Reservation";
                    else if (hasSession && isPaycSession)
                        sourceLabel = "Active PlayForAll";
                    else if (hasSession)
                        sourceLabel = "Active Closed Session";
                    else if (hasReservation)
                        sourceLabel = "Reservation";
                    else
                        sourceLabel = "System";

                    return new DashboardSlotState
                    {
                        AvailabilityID = Convert.ToInt32(rdr["AvailabilityID"]),
                        ModeName = modeName,
                        HasActiveSession = hasSession,
                        CurrentPlayerName = Convert.ToString(rdr["CurrentPlayerName"]),
                        SourceLabel = sourceLabel,
                        TimeRange = FormatTimeRange((TimeSpan)rdr["StartTime"], (TimeSpan)rdr["EndTime"])
                    };
                }
            }
        }

        private void FillNextScheduledInfo(SqlConnection conn, int courtId, DateTime targetTime, CourtDisplay display)
        {
            string sql = @"
SELECT TOP 1
    ca.ModeName,
    ca.StartTime,
    ca.EndTime,
    r.ReservationID,
    COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname, '') AS ReservationPlayer
FROM tblCourtAvailability ca
LEFT JOIN tblReservation r
    ON r.CourtID = ca.CourtID
   AND r.ResDate = ca.[Date]
   AND ca.StartTime >= r.StartTime
   AND ca.EndTime <= r.EndTime
   AND r.ReservationStatusName = 'Approved'
LEFT JOIN tblPlayerAccount pa
    ON r.UserID = pa.UserID
LEFT JOIN tblPlayerWalkIn pw
    ON r.WalkInID = pw.WalkInID
WHERE ca.CourtID = @CourtID
  AND ca.[Date] = @TargetDate
  AND ca.StartTime > @TargetTime
ORDER BY ca.StartTime ASC;";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId);
                cmd.Parameters.AddWithValue("@TargetDate", targetTime.Date);
                cmd.Parameters.AddWithValue("@TargetTime", targetTime.TimeOfDay);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                    {
                        display.NextPlayerName = "None";
                        display.NextTimeRange = "--";
                        return;
                    }

                    string modeName = Convert.ToString(rdr["ModeName"]);
                    string reservationPlayer = Convert.ToString(rdr["ReservationPlayer"]);
                    TimeSpan startTime = (TimeSpan)rdr["StartTime"];
                    TimeSpan endTime = (TimeSpan)rdr["EndTime"];

                    switch (modeName)
                    {
                        case "Reservation":
                            display.NextPlayerName = string.IsNullOrWhiteSpace(reservationPlayer) ? "Reserved Player" : reservationPlayer;
                            break;
                        case "Queue":
                            display.NextPlayerName = "Queue Window";
                            break;
                        case "Closed":
                            display.NextPlayerName = "Court Closed";
                            break;
                        default:
                            display.NextPlayerName = "PlayForAll";
                            break;
                    }

                    display.NextTimeRange = FormatTimeRange(startTime, endTime);
                }
            }
        }

        /* =========================================================
           QUICK ACTIONS
           ========================================================= */

        protected void rptCourts_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int courtId = Convert.ToInt32(e.CommandArgument);

            try
            {
                if (e.CommandName == "EndSession")
                {
                    EndActiveSession(courtId);
                }
                else if (e.CommandName == "SetMaintenance")
                {
                    ApplyImmediateClosedOverride(courtId, 2);
                }
                else if (e.CommandName == "StartSession")
                {
                    StartGenericSession(courtId);
                }

                EnsureAvailabilityWindow();
                LoadCourtDashboard(DateTime.Now);
                upDashboard.Update();
            }
            catch (Exception ex)
            {
                ShowAlert(ex.Message);
            }
        }

        private void EndActiveSession(int courtId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
UPDATE tblActiveSession
SET ActualEndTime = GETDATE(),
    StatusName = 'Completed'
WHERE CourtID = @CourtID
  AND StatusName = 'Active';";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CourtID", courtId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ApplyImmediateClosedOverride(int courtId, int hours)
        {
            DateTime now = DateTime.Now;
            DateTime roundedStart = RoundDownToSlot(now);
            DateTime roundedEnd = roundedStart.AddHours(hours);

            if (roundedEnd.Date != roundedStart.Date)
                roundedEnd = roundedStart.Date.Add(DEFAULT_CLOSE_TIME);

            TimeSpan startTime = roundedStart.TimeOfDay;
            TimeSpan endTime = roundedEnd.TimeOfDay;

            if (endTime > DEFAULT_CLOSE_TIME)
                endTime = DEFAULT_CLOSE_TIME;

            int staffId = GetCurrentStaffId();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
UPDATE tblCourtAvailability
SET ModeName = 'Closed',
    CreatedByStaffID = @StaffID
WHERE CourtID = @CourtID
  AND [Date] = @Date
  AND StartTime >= @StartTime
  AND EndTime <= @EndTime;";

                        using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@StaffID", staffId);
                            cmd.Parameters.AddWithValue("@CourtID", courtId);
                            cmd.Parameters.AddWithValue("@Date", roundedStart.Date);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows <= 0)
                                throw new InvalidOperationException("No availability slots were updated for maintenance.");
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private void StartGenericSession(int courtId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string checkSql = @"
SELECT COUNT(*)
FROM tblActiveSession
WHERE CourtID = @CourtID
  AND StatusName = 'Active';";

                using (SqlCommand cmdCheck = new SqlCommand(checkSql, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@CourtID", courtId);
                    if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                        throw new InvalidOperationException("This court already has an active session.");
                }

                string insertSql = @"
INSERT INTO tblActiveSession
(
    CourtID,
    StartTime,
    ExpectedEndTime,
    StatusName
)
VALUES
(
    @CourtID,
    GETDATE(),
    DATEADD(hour, 1, GETDATE()),
    'Active'
);";

                using (SqlCommand cmdInsert = new SqlCommand(insertSql, conn))
                {
                    cmdInsert.Parameters.AddWithValue("@CourtID", courtId);
                    cmdInsert.ExecuteNonQuery();
                }
            }
        }

        /* =========================================================
           HELPERS
           ========================================================= */

        private DateTime RoundDownToSlot(DateTime dt)
        {
            int minute = dt.Minute < 30 ? 0 : 30;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, minute, 0);
        }

        private string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            DateTime d1 = DateTime.Today.Add(start);
            DateTime d2 = DateTime.Today.Add(end);
            return d1.ToString("h:mm tt") + " - " + d2.ToString("h:mm tt");
        }

        private void ShowAlert(string message)
        {
            string safe = JsEncode(message ?? "");
            ScriptManager.RegisterStartupScript(
                this,
                GetType(),
                Guid.NewGuid().ToString("N"),
                "alert('" + safe + "');",
                true
            );
        }

        private string JsEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ");
        }

        /* =========================================================
           VIEW MODELS
           ========================================================= */

        private sealed class DashboardSlotState
        {
            public int AvailabilityID { get; set; }
            public string ModeName { get; set; }
            public bool HasActiveSession { get; set; }
            public string CurrentPlayerName { get; set; }
            public string SourceLabel { get; set; }
            public string TimeRange { get; set; }
        }

        private sealed class CourtSeed
        {
            public int CourtID { get; set; }
            public int CourtNumber { get; set; }
            public string SportName { get; set; }
        }

        public class CourtDisplay
        {
            public int CourtID { get; set; }
            public int CourtNumber { get; set; }
            public string SportName { get; set; }
            public string CurrentMode { get; set; }
            public string SourceLabel { get; set; }
            public string StatusCssClass { get; set; }
            public string CurrentPlayerName { get; set; }
            public string CurrentTimeRange { get; set; }
            public string NextPlayerName { get; set; }
            public string NextTimeRange { get; set; }
        }
    }
}