using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_court : System.Web.UI.Page
    {
        string connString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                PopulateTimeDropdowns();
                LoadCourtDashboard(DateTime.Now);

                txtSetupDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                BindCourtsDropdown();
                LoadSchedulesGrid(DateTime.Now.Date);
            }
        }

        // --- TIME DROPDOWNS & TIMER LOGIC REMAINS THE SAME ---
        private void PopulateTimeDropdowns()
        {
            ddlTime.Items.Clear(); ddlSetupStart.Items.Clear(); ddlSetupEnd.Items.Clear();
            for (int h = 6; h <= 23; h++)
            {
                string v1 = new DateTime(2000, 1, 1, h, 0, 0).ToString("HH:mm:ss");
                string t1 = new DateTime(2000, 1, 1, h, 0, 0).ToString("h:mm tt");
                ddlTime.Items.Add(new ListItem(t1, v1)); ddlSetupStart.Items.Add(new ListItem(t1, v1)); ddlSetupEnd.Items.Add(new ListItem(t1, v1));

                string v2 = new DateTime(2000, 1, 1, h, 30, 0).ToString("HH:mm:ss");
                string t2 = new DateTime(2000, 1, 1, h, 30, 0).ToString("h:mm tt");
                ddlTime.Items.Add(new ListItem(t2, v2)); ddlSetupStart.Items.Add(new ListItem(t2, v2)); ddlSetupEnd.Items.Add(new ListItem(t2, v2));
            }
        }

        protected void CourtRefreshTimer_Tick(object sender, EventArgs e) { LoadCourtDashboard(DateTime.Now); upDashboard.Update(); }
        protected void btnApplyTime_Click(object sender, EventArgs e) { if (DateTime.TryParse(txtDate.Text, out DateTime sd) && TimeSpan.TryParse(ddlTime.SelectedValue, out TimeSpan st)) { CourtRefreshTimer.Enabled = false; LoadCourtDashboard(sd.Add(st)); upDashboard.Update(); } }
        protected void btnLiveView_Click(object sender, EventArgs e) { txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd"); CourtRefreshTimer.Enabled = true; LoadCourtDashboard(DateTime.Now); upDashboard.Update(); }

        /* ==========================================
           ZONE 1: SMART DASHBOARD LOGIC
           ========================================== */

        private void LoadCourtDashboard(DateTime targetTime)
        {
            List<CourtDisplay> dashboardData = new List<CourtDisplay>();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                for (int i = 1; i <= 6; i++) // Assuming 6 courts
                {
                    dashboardData.Add(GetSmartCourtStatus(conn, i, targetTime));
                }
            }
            rptCourts.DataSource = dashboardData;
            rptCourts.DataBind();
        }

        private CourtDisplay GetSmartCourtStatus(SqlConnection conn, int courtNumber, DateTime targetTime)
        {
            CourtDisplay display = new CourtDisplay { CourtNumber = courtNumber, CurrentPlayerName = "Available", CurrentTimeRange = "--:--", NextPlayerName = "None", NextTimeRange = "--:--" };

            int courtId = 0;
            using (SqlCommand cmdGetId = new SqlCommand("SELECT CourtID FROM tblCourt WHERE CourtNumber = @CNum", conn))
            {
                cmdGetId.Parameters.AddWithValue("@CNum", courtNumber);
                object result = cmdGetId.ExecuteScalar();
                if (result != null) courtId = Convert.ToInt32(result);
            }

            display.CourtID = courtId;
            if (courtId == 0) return display;

            // 1. Check Highest Priority: Admin Overrides (Maintenance / Events)
            string availQuery = @"SELECT TOP 1 ModeName FROM tblCourtAvailability WHERE CourtID = @CourtID AND [Date] = @TargetDate AND StartTime <= @TargetTime AND EndTime > @TargetTime";
            using (SqlCommand cmdAvail = new SqlCommand(availQuery, conn))
            {
                cmdAvail.Parameters.AddWithValue("@CourtID", courtId); cmdAvail.Parameters.AddWithValue("@TargetDate", targetTime.Date); cmdAvail.Parameters.AddWithValue("@TargetTime", targetTime.TimeOfDay);
                object modeRes = cmdAvail.ExecuteScalar();
                if (modeRes != null)
                {
                    display.CurrentMode = modeRes.ToString();
                    if (display.CurrentMode == "Closed")
                    {
                        display.StatusCssClass = "closed"; display.CurrentPlayerName = "Court Maintenance"; return display;
                    }
                }
            }

            // 2. Check Second Priority: Pre-Booked Reservations
            if (string.IsNullOrEmpty(display.CurrentMode))
            {
                string resCheck = "SELECT COUNT(*) FROM tblReservation WHERE CourtID = @CourtID AND ResDate = @TargetDate AND StartTime <= @TargetTime AND EndTime > @TargetTime AND ReservationStatusName = 'Approved'";
                using (SqlCommand cmdRes = new SqlCommand(resCheck, conn))
                {
                    cmdRes.Parameters.AddWithValue("@CourtID", courtId); cmdRes.Parameters.AddWithValue("@TargetDate", targetTime.Date); cmdRes.Parameters.AddWithValue("@TargetTime", targetTime.TimeOfDay);
                    if ((int)cmdRes.ExecuteScalar() > 0) display.CurrentMode = "Reservation";
                }
            }

            // 3. Application Defaults (Fallback if no overrides or reservations exist)
            if (string.IsNullOrEmpty(display.CurrentMode))
            {
                TimeSpan t = targetTime.TimeOfDay;
                if (t >= new TimeSpan(8, 0, 0) && t < new TimeSpan(16, 0, 0)) display.CurrentMode = "PlayForAll"; // 8am to 4pm
                else if (t >= new TimeSpan(16, 0, 0) && t <= new TimeSpan(21, 0, 0)) display.CurrentMode = "Queue"; // 4pm to 9pm
                else { display.CurrentMode = "Closed"; display.StatusCssClass = "closed"; display.CurrentPlayerName = "Outside Hours"; return display; }
            }

            // Determine UI Colors based on determined Mode
            display.StatusCssClass = "available";
            if (display.CurrentMode == "PlayForAll") display.CurrentPlayerName = "Open Play Active";
            else if (display.CurrentMode == "Queue") display.CurrentPlayerName = "Open for Walk-ins";

            // Check if there is a LIVE active session taking place right now
            bool isActive = FetchActiveSessionDetails(conn, courtId, targetTime, display);
            if (isActive) display.StatusCssClass = "occupied";

            // Fetch who is next in line based on the mode
            if (display.CurrentMode == "Queue") FetchNextQueueDetails(conn, courtId, targetTime, display);
            else if (display.CurrentMode == "Reservation") FetchNextReservationDetails(conn, courtId, targetTime, display);
            else if (display.CurrentMode == "PlayForAll") { display.NextPlayerName = "Continuous Play"; display.NextTimeRange = "8:00 AM - 4:00 PM"; }

            return display;
        }

        private bool FetchActiveSessionDetails(SqlConnection conn, int courtId, DateTime targetTime, CourtDisplay display)
        {
            string sessionQuery = @"
                SELECT TOP 1 COALESCE(
                    paRes.Firstname + ' ' + paRes.Lastname, pwRes.Firstname + ' ' + pwRes.Lastname,
                    paQ.Firstname + ' ' + paQ.Lastname, pwQ.Firstname + ' ' + pwQ.Lastname,
                    pwP.Firstname + ' ' + pwP.Lastname, 'Player'
                ) AS PlayerName, s.StartTime, s.ExpectedEndTime
                FROM tblActiveSession s
                LEFT JOIN tblReservation r ON s.ReservationID = r.ReservationID LEFT JOIN tblPlayerAccount paRes ON r.UserID = paRes.UserID LEFT JOIN tblPlayerWalkIn pwRes ON r.WalkInID = pwRes.WalkInID
                LEFT JOIN tblCourtQueue q ON s.QueueID = q.QueueID LEFT JOIN tblPlayerAccount paQ ON q.UserID = paQ.UserID LEFT JOIN tblPlayerWalkIn pwQ ON q.WalkInID = pwQ.WalkInID
                LEFT JOIN tblPlayAllYouCanRegistry p ON s.PAYCID = p.PAYCID LEFT JOIN tblPlayerWalkIn pwP ON p.WalkInID = pwP.WalkInID
                WHERE s.CourtID = @CourtID AND s.StatusName = 'Active' AND s.StartTime <= @TargetTime AND (s.ActualEndTime IS NULL OR s.ActualEndTime > @TargetTime)
                ORDER BY s.StartTime DESC";

            using (SqlCommand cmd = new SqlCommand(sessionQuery, conn))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId); cmd.Parameters.AddWithValue("@TargetTime", targetTime);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        display.CurrentPlayerName = reader["PlayerName"].ToString();
                        display.CurrentTimeRange = $"{((DateTime)reader["StartTime"]):hh\\:mm tt} - {((DateTime)reader["ExpectedEndTime"]):hh\\:mm tt}";
                        return true;
                    }
                }
            }
            return false;
        }

        private void FetchNextQueueDetails(SqlConnection conn, int courtId, DateTime targetTime, CourtDisplay display)
        {
            string q = @"SELECT TOP 1 COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname) AS PlayerName FROM tblCourtQueue cq LEFT JOIN tblPlayerAccount pa ON cq.UserID = pa.UserID LEFT JOIN tblPlayerWalkIn pw ON cq.WalkInID = pw.WalkInID WHERE cq.CourtID = @CourtID AND cq.QueueDate = @TargetDate AND cq.StatusName = 'Waiting' ORDER BY cq.QueueNumber ASC";
            using (SqlCommand cmd = new SqlCommand(q, conn))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId); cmd.Parameters.AddWithValue("@TargetDate", targetTime.Date);
                object res = cmd.ExecuteScalar();
                if (res != null) { display.NextPlayerName = res.ToString(); display.NextTimeRange = "On Deck"; }
            }
        }

        private void FetchNextReservationDetails(SqlConnection conn, int courtId, DateTime targetTime, CourtDisplay display)
        {
            string q = @"SELECT TOP 1 COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname) AS PlayerName, r.StartTime, r.EndTime FROM tblReservation r LEFT JOIN tblPlayerAccount pa ON r.UserID = pa.UserID LEFT JOIN tblPlayerWalkIn pw ON r.WalkInID = pw.WalkInID WHERE r.CourtID = @CourtID AND r.ResDate = @TargetDate AND r.StartTime > @TargetTime AND r.ReservationStatusName = 'Approved' ORDER BY r.StartTime ASC";
            using (SqlCommand cmd = new SqlCommand(q, conn))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId); cmd.Parameters.AddWithValue("@TargetDate", targetTime.Date); cmd.Parameters.AddWithValue("@TargetTime", targetTime.TimeOfDay);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read()) { display.NextPlayerName = rdr["PlayerName"].ToString(); display.NextTimeRange = $"{((TimeSpan)rdr["StartTime"]):hh\\:mm} - {((TimeSpan)rdr["EndTime"]):hh\\:mm}"; }
                }
            }
        }

        /* ==========================================
           ZONE 2: QUICK ACTIONS LOGIC
           ========================================== */

        protected void rptCourts_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int courtId = Convert.ToInt32(e.CommandArgument);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                if (e.CommandName == "EndSession")
                {
                    // Forcibly end the active session on this court
                    string endSql = "UPDATE tblActiveSession SET ActualEndTime = GETDATE(), StatusName = 'Completed' WHERE CourtID = @CourtID AND StatusName = 'Active'";
                    using (SqlCommand cmd = new SqlCommand(endSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CourtID", courtId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else if (e.CommandName == "SetMaintenance")
                {
                    // Admin override: Insert a Closed block for the next 2 hours
                    string blockSql = "INSERT INTO tblCourtAvailability (CourtID, [Date], StartTime, EndTime, ModeName, CreatedByStaffID) VALUES (@CourtID, CAST(GETDATE() AS DATE), CAST(GETDATE() AS TIME), CAST(DATEADD(hour, 2, GETDATE()) AS TIME), 'Closed', 1)";
                    using (SqlCommand cmd = new SqlCommand(blockSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CourtID", courtId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else if (e.CommandName == "StartSession")
                {
                    // Check if there's someone in Queue/Res to start. If not, just start an empty block.
                    // For a robust system, this should ideally grab the QueueID of the "Next Up" player.
                    string startSql = "INSERT INTO tblActiveSession (CourtID, StartTime, ExpectedEndTime, StatusName) VALUES (@CourtID, GETDATE(), DATEADD(hour, 1, GETDATE()), 'Active')";
                    using (SqlCommand cmd = new SqlCommand(startSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CourtID", courtId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // Refresh visuals
            LoadCourtDashboard(DateTime.Now);
            LoadSchedulesGrid(DateTime.Now.Date);
            upDashboard.Update();
            upManagement.Update();
        }

        /* ==========================================
           ZONE 3: MANUAL OVERRIDE SETUP
           ========================================== */

        // --- OVERRIDE SETUP LOGIC REMAINS EXACTLY THE SAME AS PREVIOUS ---
        private void BindCourtsDropdown()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = "SELECT CourtID, 'Court ' + CAST(CourtNumber AS VARCHAR) + ' (' + SportName + ')' AS CourtName FROM tblCourt WHERE IsActive = 1 ORDER BY CourtNumber";
                using (SqlCommand cmd = new SqlCommand(sql, conn)) { conn.Open(); ddlSetupCourt.DataSource = cmd.ExecuteReader(); ddlSetupCourt.DataTextField = "CourtName"; ddlSetupCourt.DataValueField = "CourtID"; ddlSetupCourt.DataBind(); }
            }
        }

        protected void txtSetupDate_TextChanged(object sender, EventArgs e) { if (DateTime.TryParse(txtSetupDate.Text, out DateTime sd)) { LoadSchedulesGrid(sd); upManagement.Update(); } }

        private void LoadSchedulesGrid(DateTime targetDate)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"SELECT a.AvailabilityID, 'Court ' + CAST(c.CourtNumber AS VARCHAR) AS CourtName, CONVERT(VARCHAR(5), a.StartTime, 108) + ' - ' + CONVERT(VARCHAR(5), a.EndTime, 108) AS TimeRange, a.ModeName FROM tblCourtAvailability a INNER JOIN tblCourt c ON a.CourtID = c.CourtID WHERE a.[Date] = @Date ORDER BY c.CourtNumber, a.StartTime";
                using (SqlCommand cmd = new SqlCommand(sql, conn)) { cmd.Parameters.AddWithValue("@Date", targetDate.Date); SqlDataAdapter da = new SqlDataAdapter(cmd); DataTable dt = new DataTable(); da.Fill(dt); gvSchedules.DataSource = dt; gvSchedules.DataBind(); }
            }
        }

        protected void btnSaveSchedule_Click(object sender, EventArgs e)
        {
            if (!DateTime.TryParse(txtSetupDate.Text, out DateTime targetDate)) return;
            TimeSpan sTime = TimeSpan.Parse(ddlSetupStart.SelectedValue); TimeSpan eTime = TimeSpan.Parse(ddlSetupEnd.SelectedValue);

            if (sTime >= eTime) { ScriptManager.RegisterStartupScript(this, GetType(), "errTime", "alert('End Time must be later than Start Time.');", true); return; }

            int courtId = int.Parse(ddlSetupCourt.SelectedValue); string mode = ddlSetupMode.SelectedValue; int staffId = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string overlapCheck = "SELECT COUNT(*) FROM tblCourtAvailability WHERE CourtID = @CourtID AND [Date] = @Date AND StartTime < @EndTime AND EndTime > @StartTime";
                using (SqlCommand cmdCheck = new SqlCommand(overlapCheck, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@CourtID", courtId); cmdCheck.Parameters.AddWithValue("@Date", targetDate.Date); cmdCheck.Parameters.AddWithValue("@StartTime", sTime); cmdCheck.Parameters.AddWithValue("@EndTime", eTime);
                    if ((int)cmdCheck.ExecuteScalar() > 0) { ScriptManager.RegisterStartupScript(this, GetType(), "errOverlap", "alert('Error: Overlaps existing override.');", true); return; }
                }

                string insertSql = "INSERT INTO tblCourtAvailability (CourtID, [Date], StartTime, EndTime, ModeName, CreatedByStaffID) VALUES (@CourtID, @Date, @StartTime, @EndTime, @Mode, @Staff)";
                using (SqlCommand cmdInsert = new SqlCommand(insertSql, conn))
                {
                    cmdInsert.Parameters.AddWithValue("@CourtID", courtId); cmdInsert.Parameters.AddWithValue("@Date", targetDate.Date); cmdInsert.Parameters.AddWithValue("@StartTime", sTime); cmdInsert.Parameters.AddWithValue("@EndTime", eTime); cmdInsert.Parameters.AddWithValue("@Mode", mode); cmdInsert.Parameters.AddWithValue("@Staff", staffId);
                    cmdInsert.ExecuteNonQuery();
                }
            }

            LoadSchedulesGrid(targetDate); LoadCourtDashboard(DateTime.Now); upManagement.Update(); upDashboard.Update();
            ScriptManager.RegisterStartupScript(this, GetType(), "success", "alert('Override Applied Successfully.');", true);
        }

        protected void gvSchedules_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int availabilityId = Convert.ToInt32(gvSchedules.DataKeys[e.RowIndex].Value);
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = "DELETE FROM tblCourtAvailability WHERE AvailabilityID = @ID";
                using (SqlCommand cmd = new SqlCommand(sql, conn)) { cmd.Parameters.AddWithValue("@ID", availabilityId); conn.Open(); cmd.ExecuteNonQuery(); }
            }
            if (DateTime.TryParse(txtSetupDate.Text, out DateTime targetDate)) { LoadSchedulesGrid(targetDate); LoadCourtDashboard(DateTime.Now); }
            upManagement.Update(); upDashboard.Update();
        }

        public class CourtDisplay
        {
            public int CourtID { get; set; }
            public int CourtNumber { get; set; }
            public string CurrentMode { get; set; }
            public string StatusCssClass { get; set; }
            public string CurrentPlayerName { get; set; }
            public string CurrentTimeRange { get; set; }
            public string NextPlayerName { get; set; }
            public string NextTimeRange { get; set; }
        }
    }
}