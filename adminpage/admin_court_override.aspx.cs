using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Smash_IT.adminpage
{
    public partial class admin_court_override : System.Web.UI.Page
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
                EnsureAvailabilityWindow();
                txtDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                LoadMatrix();
            }
        }

        protected void btnLoadSlots_Click(object sender, EventArgs e)
        {
            EnsureAvailabilityWindow();
            LoadMatrix();
        }


        protected void btnConfirmApply_Click(object sender, EventArgs e)
        {
            int availabilityId;
            if (!int.TryParse(hfAvailabilityID.Value, out availabilityId) || availabilityId <= 0)
            {
                ShowAlert("Invalid slot selected.");
                return;
            }

            string newMode = (ddlModalMode.SelectedValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(newMode))
            {
                ShowAlert("Please select a valid mode.");
                return;
            }

            int staffId = GetCurrentStaffId();

            try
            {
                UpdateSlotMode(availabilityId, newMode, staffId);
                EnsureAvailabilityWindow();
                LoadMatrix();

                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "closeModalAfterApply",
                    "closeEditModal(); alert('Court slot updated successfully.');",
                    true
                );
            }
            catch (Exception ex)
            {
                string safe = JsEncode(ex.Message ?? "Unable to update slot.");
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "applyErr",
                    "alert('" + safe + "');",
                    true
                );
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

            throw new InvalidOperationException("Unable to identify the current staff user.");
        }

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

                        // Reset only system-generated rows
                        ResetSystemSlotsToPlayForAll(conn, tx, startDate, endDate);

                        // Reapply authoritative computed state
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
  AND ca.CreatedByStaffID = @SystemStaffID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        private void LoadMatrix()
        {
            DateTime targetDate;
            if (!DateTime.TryParse(txtDate.Text, out targetDate))
            {
                targetDate = DateTime.Today;
                txtDate.Text = targetDate.ToString("yyyy-MM-dd");
            }

            DataTable courts = GetActiveCourts();
            DataTable slots = GetSlotsForDate(targetDate.Date);

            litMatrixTable.Text = BuildMatrixHtml(courts, slots);
        }

        private DataTable GetActiveCourts()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
SELECT CourtID, CourtNumber, SportName
FROM tblCourt
WHERE IsActive = 1
ORDER BY CourtNumber;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private DataTable GetSlotsForDate(DateTime targetDate)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
SELECT
    ca.AvailabilityID,
    ca.CourtID,
    c.CourtNumber,
    c.SportName,
    ca.[Date],
    ca.StartTime,
    ca.EndTime,
    LEFT(CONVERT(VARCHAR(8), ca.StartTime, 108), 5) + ' - ' + LEFT(CONVERT(VARCHAR(8), ca.EndTime, 108), 5) AS TimeRange,
    ca.ModeName,
    ca.CreatedByStaffID,

    r.ReservationID,
    s.SessionID,
    s.QueueID,

  CASE
    WHEN r.ReservationID IS NOT NULL THEN
        COALESCE(
            pa.Firstname + ' ' + pa.Lastname,
            pw.Firstname + ' ' + pw.Lastname,
            'Reserved Player'
        )
    WHEN s.SessionID IS NOT NULL AND s.QueueID IS NOT NULL THEN 'Queue Session'
    WHEN s.SessionID IS NOT NULL AND s.QueueID IS NULL THEN 'Closed / Active Session'
    WHEN ca.CreatedByStaffID = @SystemStaffID THEN 'Open / No Assigned Player'
    ELSE 'Modified by Staff'
END AS TakenByName,

 CASE
    WHEN s.SessionID IS NOT NULL AND s.QueueID IS NOT NULL THEN 'Locked: Active Queue'
    WHEN s.SessionID IS NOT NULL AND s.QueueID IS NULL THEN 'Locked: Active Session'
    WHEN r.ReservationID IS NOT NULL THEN 'Locked: Reservation'
    WHEN ca.CreatedByStaffID = @SystemStaffID THEN 'System Open Slot'
    ELSE 'Manual Override'
END AS SourceLabel

FROM tblCourtAvailability ca
INNER JOIN tblCourt c
    ON c.CourtID = ca.CourtID

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

LEFT JOIN tblActiveSession s
    ON s.CourtID = ca.CourtID
   AND ca.[Date] = CAST(s.StartTime AS DATE)
   AND ca.StartTime >= CAST(s.StartTime AS TIME)
   AND ca.EndTime <= CAST(
        CASE
            WHEN s.ActualEndTime IS NOT NULL THEN s.ActualEndTime
            ELSE s.ExpectedEndTime
        END AS TIME
   )
   AND s.StatusName = 'Active'

WHERE ca.[Date] = @TargetDate
  AND c.IsActive = 1
ORDER BY ca.StartTime, c.CourtNumber;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TargetDate", targetDate.Date);
                    cmd.Parameters.AddWithValue("@SystemStaffID", SYSTEM_STAFF_ID);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        conn.Open();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        private string BuildMatrixHtml(DataTable courts, DataTable slots)
        {
            StringBuilder sb = new StringBuilder();

            List<DataRow> courtRows = courts.AsEnumerable()
                .OrderBy(r => Convert.ToInt32(r["CourtNumber"]))
                .ToList();

            List<string> timeRanges = slots.AsEnumerable()
                .Select(r => Convert.ToString(r["TimeRange"]))
                .Distinct()
                .ToList();

            sb.Append("<div class='matrix-wrap'>");
            sb.Append("<table class='matrix-table'>");

            sb.Append("<thead><tr>");
            sb.Append("<th class='time-col'>Time Block</th>");

            foreach (DataRow court in courtRows)
            {
                string courtTitle = "Court " + Convert.ToString(court["CourtNumber"]) +
                                    " (" + Convert.ToString(court["SportName"]) + ")";
                sb.Append("<th>");
                sb.Append(HttpUtility.HtmlEncode(courtTitle));
                sb.Append("</th>");
            }

            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            foreach (string timeRange in timeRanges)
            {
                sb.Append("<tr>");
                sb.Append("<td class='time-cell'>");
                sb.Append(HttpUtility.HtmlEncode(timeRange));
                sb.Append("</td>");

                foreach (DataRow court in courtRows)
                {
                    int courtId = Convert.ToInt32(court["CourtID"]);

                    DataRow slot = slots.AsEnumerable().FirstOrDefault(r =>
                        Convert.ToInt32(r["CourtID"]) == courtId &&
                        Convert.ToString(r["TimeRange"]) == timeRange
                    );

                    if (slot == null)
                    {
                        sb.Append("<td><div class='slot-box empty-slot'>");
                        sb.Append("<div class='slot-mode'>No Slot</div>");
                        sb.Append("<div class='slot-name'>--</div>");
                        sb.Append("</div></td>");
                        continue;
                    }

                    int availabilityId = Convert.ToInt32(slot["AvailabilityID"]);
                    string modeName = Convert.ToString(slot["ModeName"]);
                    string takenByName = Convert.ToString(slot["TakenByName"]);
                    string sourceLabel = Convert.ToString(slot["SourceLabel"]);
                    int createdByStaffId = Convert.ToInt32(slot["CreatedByStaffID"]);

                    bool isLocked = IsSlotLockedForEdit(slot);
                    string slotClass = GetSlotCssClass(modeName, isLocked);

                    string safeTime = JsEncode(Convert.ToString(slot["TimeRange"]));
                    string safeCourt = JsEncode("Court " + Convert.ToString(court["CourtNumber"]) + " (" + Convert.ToString(court["SportName"]) + ")");
                    string safeMode = JsEncode(modeName);
                    string safeTaken = JsEncode(takenByName);
                    string safeSource = JsEncode(sourceLabel);

                    string click = "openEditModal(" +
                  availabilityId + ", '" +
                  safeTime + "', '" +
                  safeCourt + "', '" +
                  safeMode + "', '" +
                  safeTaken + "', '" +
                  createdByStaffId + "', '" +
                  safeSource + "', " +
                  (isLocked ? "true" : "false") + ")";

                    sb.Append("<td>");
                    sb.Append("<div class='slot-box " + slotClass + "' onclick=\"" + click + "\">");

                    sb.Append("<div class='slot-mode'>");
                    sb.Append(HttpUtility.HtmlEncode(modeName));
                    sb.Append("</div>");

                    sb.Append("<div class='slot-name'>");
                    sb.Append(HttpUtility.HtmlEncode(takenByName));
                    sb.Append("</div>");

                    sb.Append("<div class='slot-source'>");
                    sb.Append(HttpUtility.HtmlEncode(sourceLabel));
                    sb.Append("</div>");

                    sb.Append("</div>");
                    sb.Append("</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");
            sb.Append("</div>");

            return sb.ToString();
        }

        private bool IsSlotLockedForEdit(DataRow slot)
        {
            if (slot == null)
                return true;

            bool hasReservation = slot["ReservationID"] != DBNull.Value;
            bool hasSession = slot["SessionID"] != DBNull.Value;

            return hasReservation || hasSession;
        }
        private string GetSlotCssClass(string modeName, bool isLocked)
        {
            string baseClass;

            switch ((modeName ?? "").Trim())
            {
                case "Queue":
                    baseClass = "queue";
                    break;
                case "Reservation":
                    baseClass = "reservation";
                    break;
                case "Closed":
                    baseClass = "closed";
                    break;
                default:
                    baseClass = "pfa";
                    break;
            }

            if (isLocked)
                baseClass += " locked";

            return baseClass;
        }

        private void UpdateSlotMode(int availabilityId, string newMode, int staffId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        SlotEditState slot = GetSlotEditState(conn, tx, availabilityId);

                        if (slot == null)
                            throw new InvalidOperationException("The selected slot could not be found.");

                        if (slot.HasApprovedReservation)
                            throw new InvalidOperationException("This slot is occupied by an approved reservation and cannot be modified.");

                        if (slot.HasActiveSession)
                            throw new InvalidOperationException("This slot is occupied by an active session and cannot be modified.");

                        string sql = @"
UPDATE tblCourtAvailability
SET ModeName = @ModeName,
    CreatedByStaffID = @StaffID
WHERE AvailabilityID = @AvailabilityID;";

                        using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@ModeName", newMode);
                            cmd.Parameters.AddWithValue("@StaffID", staffId);
                            cmd.Parameters.AddWithValue("@AvailabilityID", availabilityId);

                            int rows = cmd.ExecuteNonQuery();
                            if (rows <= 0)
                                throw new InvalidOperationException("Unable to update the selected slot.");
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
        private SlotEditState GetSlotEditState(SqlConnection conn, SqlTransaction tx, int availabilityId)
        {
            string sql = @"
SELECT TOP 1
    ca.AvailabilityID,
    ca.CourtID,
    ca.[Date],
    ca.StartTime,
    ca.EndTime,
    ca.ModeName,
    ca.CreatedByStaffID,

    CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM tblReservation r
            WHERE r.CourtID = ca.CourtID
              AND r.ResDate = ca.[Date]
              AND ca.StartTime >= r.StartTime
              AND ca.EndTime <= r.EndTime
              AND r.ReservationStatusName = 'Approved'
        ) THEN 1 ELSE 0
    END AS HasApprovedReservation,

    CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM tblActiveSession s
            WHERE s.CourtID = ca.CourtID
              AND ca.[Date] = CAST(s.StartTime AS DATE)
              AND ca.StartTime >= CAST(s.StartTime AS TIME)
              AND ca.EndTime <= CAST(
                    CASE
                        WHEN s.ActualEndTime IS NOT NULL THEN s.ActualEndTime
                        ELSE s.ExpectedEndTime
                    END AS TIME
              )
              AND s.StatusName = 'Active'
        ) THEN 1 ELSE 0
    END AS HasActiveSession

FROM tblCourtAvailability ca
WHERE ca.AvailabilityID = @AvailabilityID;";

            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@AvailabilityID", availabilityId);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                        return null;

                    return new SlotEditState
                    {
                        AvailabilityID = Convert.ToInt32(rdr["AvailabilityID"]),
                        CourtID = Convert.ToInt32(rdr["CourtID"]),
                        Date = Convert.ToDateTime(rdr["Date"]).Date,
                        StartTime = (TimeSpan)rdr["StartTime"],
                        EndTime = (TimeSpan)rdr["EndTime"],
                        ModeName = Convert.ToString(rdr["ModeName"]),
                        CreatedByStaffID = Convert.ToInt32(rdr["CreatedByStaffID"]),
                        HasApprovedReservation = Convert.ToBoolean(rdr["HasApprovedReservation"]),
                        HasActiveSession = Convert.ToBoolean(rdr["HasActiveSession"])
                    };
                }
            }
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

        private sealed class SlotEditState
        {
            public int AvailabilityID { get; set; }
            public int CourtID { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string ModeName { get; set; }
            public int CreatedByStaffID { get; set; }
            public bool HasApprovedReservation { get; set; }
            public bool HasActiveSession { get; set; }
        }
    }
}