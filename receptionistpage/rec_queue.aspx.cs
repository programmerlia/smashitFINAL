using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Web.UI;

namespace Smash_IT.receptionistpage
{
    public partial class rec_queue : System.Web.UI.Page
    {
        string connString = ConfigurationManager.ConnectionStrings["soapergandahannali"]?.ConnectionString
                            ?? ConfigurationManager.ConnectionStrings["dbsmashitFINAL"]?.ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                PopulateTimeDropdowns();
            }
            RefreshGrid();
            LoadAllEvents();
        }

        protected void btnToday_Click(object sender, EventArgs e)
        {
            txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
            RefreshGrid();
            LoadAllEvents();
        }

        protected void txtDate_TextChanged(object sender, EventArgs e)
        {
            RefreshGrid();
            LoadAllEvents();
        }

        protected void chkHideCancelled_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void PopulateTimeDropdowns()
        {
            ddlStartTime.Items.Clear(); ddlEndTime.Items.Clear();
            for (int h = 8; h <= 21; h++)
            {
                string v1 = new DateTime(2000, 1, 1, h, 0, 0).ToString("HH:mm");
                string t1 = new DateTime(2000, 1, 1, h, 0, 0).ToString("hh:mm tt");
                ddlStartTime.Items.Add(new ListItem(t1, v1));
                ddlEndTime.Items.Add(new ListItem(t1, v1));

                string v2 = new DateTime(2000, 1, 1, h, 30, 0).ToString("HH:mm");
                string t2 = new DateTime(2000, 1, 1, h, 30, 0).ToString("hh:mm tt");
                ddlStartTime.Items.Add(new ListItem(t2, v2));
                ddlEndTime.Items.Add(new ListItem(t2, v2));
            }
        }

        private void RefreshGrid()
        {
            if (DateTime.TryParse(txtDate.Text, out DateTime d))
            {
                lblDgvDate.Text = d.ToString("MMM dd, yyyy");
                LoadScheduleGrid(d);
            }
        }

        private void LoadAllEvents()
        {
            if (DateTime.TryParse(txtDate.Text, out DateTime targetDate))
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    string sql = @"
                        SELECT e.EventID, e.Title, e.SportName, e.EventDate, e.MaxPlayers, e.RegistrationFee 
                        FROM tblEvent e 
                        WHERE e.IsActive = 1 AND CAST(e.EventDate AS DATE) = @Date 
                        ORDER BY e.EventDate DESC";

                    SqlCommand cmd = new SqlCommand(sql, c);
                    cmd.Parameters.AddWithValue("@Date", targetDate);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAllEvents.DataSource = dt;
                    gvAllEvents.DataBind();
                }
            }
        }

        private void LoadScheduleGrid(DateTime targetDate)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Court");

            List<TimeSpan> slots = new List<TimeSpan>();
            for (int h = 8; h <= 22; h++)
            {
                slots.Add(new TimeSpan(h, 0, 0));
                if (h < 22) slots.Add(new TimeSpan(h, 30, 0));
            }

            foreach (TimeSpan st in slots)
            {
                DateTime t1 = new DateTime(2000, 1, 1).Add(st);
                dt.Columns.Add($"{t1:h:mm tt} - {t1.AddMinutes(60):h:mm tt}");
            }

            DataTable activeRecords = new DataTable();
            DataTable courtBlocks = new DataTable();

            using (SqlConnection c = new SqlConnection(connString))
            {
                // Unified Query merging Standard Reservations and Events for Visual Grid
                string sqlUnified = @"
                    SELECT 
                        'RES' AS Type, r.ReservationID AS ID, r.CourtID, r.StartTime, r.EndTime, r.SportName, 
                        r.ReservationStatusName AS Status, 
                        CASE 
                            WHEN r.UserID IS NOT NULL THEN ISNULL(pa.Firstname,'') + ' ' + ISNULL(pa.Lastname,'')
                            ELSE ISNULL(pw.Firstname,'') + ' ' + ISNULL(pw.Lastname,'') 
                        END AS Name,
                        CASE WHEN r.UserID IS NULL AND r.WalkInID IS NOT NULL THEN 1 ELSE 0 END AS IsWalkIn
                    FROM tblReservation r
                    LEFT JOIN tblPlayerAccount pa ON r.UserID = pa.UserID
                    LEFT JOIN tblPlayerWalkIn pw ON r.WalkInID = pw.WalkInID
                    WHERE CAST(r.ResDate AS DATE) = CAST(@Date AS DATE) 
                      AND r.ReservationStatusName IN ('Pending', 'Approved', 'Completed', 'Cancelled')
                    
                    UNION ALL
                    
                    SELECT 
                        'EVT' AS Type, e.EventID AS ID, s.CourtID, CAST(s.StartTime AS TIME), CAST(s.ExpectedEndTime AS TIME), 
                        e.SportName, 'Approved' AS Status, e.Title AS Name, 0 AS IsWalkIn
                    FROM tblEvent e
                    JOIN tblCourtQueue q ON e.EventID = q.EventID
                    JOIN tblActiveSession s ON q.QueueID = s.QueueID
                    WHERE CAST(s.StartTime AS DATE) = CAST(@Date AS DATE) AND s.StatusName = 'Active'";

                SqlCommand cmd1 = new SqlCommand(sqlUnified, c);
                cmd1.Parameters.AddWithValue("@Date", targetDate.Date);
                new SqlDataAdapter(cmd1).Fill(activeRecords);

                string blockQ = @"
                    SELECT CourtID, StartTime, EndTime, ModeName 
                    FROM tblCourtAvailability 
                    WHERE [Date] = CAST(@Date AS DATE) AND ModeName = 'Closed'";

                SqlCommand cmd2 = new SqlCommand(blockQ, c);
                cmd2.Parameters.AddWithValue("@Date", targetDate.Date);
                new SqlDataAdapter(cmd2).Fill(courtBlocks);
            }

            for (int cNum = 1; cNum <= 6; cNum++)
            {
                DataRow dr = dt.NewRow();
                dr["Court"] = "Court " + cNum;

                for (int i = 0; i < slots.Count; i++)
                {
                    TimeSpan slotStart = slots[i];
                    TimeSpan slotEnd = slotStart.Add(new TimeSpan(1, 0, 0)); // 1 HOUR window logic

                    // Check Hard Blocks First
                    var block = courtBlocks.AsEnumerable().FirstOrDefault(b =>
                        b.Field<int>("CourtID") == cNum &&
                        b.Field<TimeSpan>("StartTime") < slotEnd &&
                        b.Field<TimeSpan>("EndTime") > slotStart);

                    if (block != null)
                    {
                        dr[i + 1] = "BLOCKED|CLOSED";
                        continue;
                    }

                    // Gather all overlapping records for this specific 1-hour window
                    var overlappingRecords = activeRecords.AsEnumerable().Where(r =>
                        r.Field<int>("CourtID") == cNum &&
                        r.Field<TimeSpan>("StartTime") < slotEnd &&
                        r.Field<TimeSpan>("EndTime") > slotStart).ToList();

                    if (chkHideCancelled.Checked)
                    {
                        overlappingRecords = overlappingRecords.Where(r => r.Field<string>("Status") != "Cancelled").ToList();
                    }

                    if (overlappingRecords.Count > 0)
                    {
                        var activeRecs = overlappingRecords.Where(r => r.Field<string>("Status") != "Cancelled").ToList();

                        if (activeRecs.Count > 1)
                        {
                            dr[i + 1] = "BLOCKED|OVERLAP";
                        }
                        else if (activeRecs.Count == 1)
                        {
                            var rec = activeRecs[0];
                            TimeSpan rStart = rec.Field<TimeSpan>("StartTime");
                            TimeSpan rEnd = rec.Field<TimeSpan>("EndTime");

                            if (rStart <= slotStart && rEnd >= slotEnd)
                            {
                                dr[i + 1] = $"{rec.Field<string>("Type")}|{rec.Field<int>("ID")}|{rec.Field<string>("Status")}|{rec.Field<string>("SportName") ?? "Mixed"}|{rec.Field<string>("Name")}|{rec.Field<int>("IsWalkIn")}";
                            }
                            else
                            {
                                dr[i + 1] = "BLOCKED|OVERLAP";
                            }
                        }
                        else
                        {
                            // Render Cancelled
                            var cancelledRec = overlappingRecords.First();
                            TimeSpan rStart = cancelledRec.Field<TimeSpan>("StartTime");
                            TimeSpan rEnd = cancelledRec.Field<TimeSpan>("EndTime");

                            if (rStart <= slotStart && rEnd >= slotEnd)
                            {
                                dr[i + 1] = $"{cancelledRec.Field<string>("Type")}|{cancelledRec.Field<int>("ID")}|{cancelledRec.Field<string>("Status")}|{cancelledRec.Field<string>("SportName") ?? "Mixed"}|{cancelledRec.Field<string>("Name")}|{cancelledRec.Field<int>("IsWalkIn")}";
                            }
                            else
                            {
                                dr[i + 1] = $"OPEN|{slotStart.ToString(@"hh\:mm")}";
                            }
                        }
                    }
                    else
                    {
                        dr[i + 1] = $"OPEN|{slotStart.ToString(@"hh\:mm")}";
                    }
                }
                dt.Rows.Add(dr);
            }

            gvScheduleGrid.DataSource = dt;
            gvScheduleGrid.DataBind();
        }

        protected void gvScheduleGrid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                e.Row.Cells[0].CssClass = "event-court-col";
                int courtID = e.Row.RowIndex + 1;

                for (int i = 1; i < e.Row.Cells.Count; i++)
                {
                    string raw = Server.HtmlDecode(e.Row.Cells[i].Text).Trim();
                    e.Row.Cells[i].CssClass = "p-1";

                    if (raw.StartsWith("BLOCKED|"))
                    {
                        string textLabel = raw.Split('|').Length > 1 && !string.IsNullOrEmpty(raw.Split('|')[1]) ? raw.Split('|')[1] : "OVERLAP";
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell bg-gray event-cell-info'><span class='event-cell-title'>{textLabel}</span></div>";
                    }
                    else if (raw.StartsWith("EVT|"))
                    {
                        string[] p = raw.Split('|');
                        int eId = int.Parse(p[1]);
                        string title = p[4];
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell bg-blue event-cell-info' onclick=\"triggerSidebar('EVT', {courtID}, '', '{eId}')\"><span class='event-cell-title'>{title}</span><span class='event-cell-sub'>Event</span></div>";
                    }
                    else if (raw.StartsWith("OPEN|"))
                    {
                        string timeVal = raw.Split('|')[1];
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell bg-white event-cell-info' onclick=\"triggerSidebar('OPEN', {courtID}, '{timeVal}', '0')\"><span class='event-cell-title'>OPEN</span></div>";
                    }
                    else if (raw.StartsWith("RES|"))
                    {
                        string[] p = raw.Split('|');
                        string stat = p[2];
                        string sport = p[3].ToUpper();
                        string name = p[4];
                        bool isWalkIn = p[5] == "1";

                        string css = "bg-green";
                        if (stat == "Cancelled") css = "bg-red";
                        else if (stat == "Pending" && !isWalkIn) css = "bg-purple";
                        else css = sport.Contains("PICKLE") ? "bg-orange" : "bg-green";

                        e.Row.Cells[i].Text = $"<div class='event-slot-cell {css} event-cell-info' onclick=\"triggerSidebar('RES', 0, '', '0')\"><span class='event-cell-title'>{name}</span><span class='event-cell-sub'>{stat}</span></div>";
                    }
                }
            }
        }

        protected void btnHiddenTrigger_Click(object sender, EventArgs e)
        {
            int.TryParse(hfActionCourt.Value, out int cId);
            int.TryParse(hfActionEventID.Value, out int eId);

            pnlEmptyState.Visible = false;
            pnlEventForm.Visible = true;
            cblCourts.ClearSelection();

            if (hfActionType.Value == "OPEN")
            {
                litSidebarHeader.Text = "Create New Event";
                hfIsEdit.Value = "false";
                hfEventID.Value = "0";
                txtEventTitle.Text = "";
                txtFee.Text = "";
                txtMaxPlayers.Text = "12";

                if (cId > 0 && cId <= cblCourts.Items.Count)
                    cblCourts.Items[cId - 1].Selected = true;

                ddlSport.SelectedValue = cId > 4 ? "PICKLEBALL" : "BADMINTON";

                if (!string.IsNullOrEmpty(hfActionTime.Value))
                {
                    if (ddlStartTime.Items.FindByValue(hfActionTime.Value) != null)
                        ddlStartTime.SelectedValue = hfActionTime.Value;

                    int idx = ddlStartTime.SelectedIndex + 4; // default to 2 hours
                    ddlEndTime.SelectedIndex = Math.Min(idx, ddlEndTime.Items.Count - 1);
                }

                btnCancelEvent.Visible = false;
                btnRemoveSlot.Visible = false;
                pnlMatchSummary.Visible = false;
            }
            else if (hfActionType.Value == "EVT")
            {
                litSidebarHeader.Text = (cId > 0) ? $"Manage Event (Court {cId})" : "Manage Event Settings";
                hfIsEdit.Value = "true";
                hfEventID.Value = eId.ToString();

                btnCancelEvent.Visible = true;
                btnRemoveSlot.Visible = (cId > 0);
                pnlMatchSummary.Visible = true;

                try
                {
                    using (SqlConnection c = new SqlConnection(connString))
                    {
                        string sql = @"
                            SELECT TOP 1 e.Title, e.SportName, e.EventDate, e.RegistrationFee, e.MaxPlayers, CAST(s.StartTime AS TIME) as ST, CAST(s.ExpectedEndTime AS TIME) as ET 
                            FROM tblEvent e 
                            JOIN tblCourtQueue q ON e.EventID = q.EventID 
                            JOIN tblActiveSession s ON q.QueueID = s.QueueID 
                            WHERE e.EventID = @EID";

                        SqlCommand cmd = new SqlCommand(sql, c);
                        cmd.Parameters.AddWithValue("@EID", eId);
                        c.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtEventTitle.Text = dr["Title"].ToString();
                                ddlSport.SelectedValue = dr["SportName"].ToString().ToUpper();

                                string st = new DateTime(((TimeSpan)dr["ST"]).Ticks).ToString("HH:mm");
                                string et = new DateTime(((TimeSpan)dr["ET"]).Ticks).ToString("HH:mm");

                                if (ddlStartTime.Items.FindByValue(st) != null) ddlStartTime.SelectedValue = st;
                                if (ddlEndTime.Items.FindByValue(et) != null) ddlEndTime.SelectedValue = et;

                                txtFee.Text = dr["RegistrationFee"] != DBNull.Value ? Convert.ToDecimal(dr["RegistrationFee"]).ToString("0.00") : "0.00";
                                txtMaxPlayers.Text = dr["MaxPlayers"] != DBNull.Value ? dr["MaxPlayers"].ToString() : "12";

                                if (dr["EventDate"] != DBNull.Value)
                                {
                                    txtDate.Text = Convert.ToDateTime(dr["EventDate"]).ToString("yyyy-MM-dd");
                                }
                            }
                        }

                        SqlCommand cmdCourts = new SqlCommand("SELECT CourtID FROM tblEventCourtPool WHERE EventID = @EID", c);
                        cmdCourts.Parameters.AddWithValue("@EID", eId);
                        using (SqlDataReader drC = cmdCourts.ExecuteReader())
                        {
                            while (drC.Read())
                            {
                                int assignedCourt = Convert.ToInt32(drC["CourtID"]);
                                if (assignedCourt > 0 && assignedCourt <= cblCourts.Items.Count)
                                    cblCourts.Items[assignedCourt - 1].Selected = true;
                            }
                        }
                    }

                    LoadMatchSummary(eId);
                }
                catch (Exception ex)
                {
                    ShowAlert("error", "Data Error", ex.Message);
                }
            }
        }

        private void LoadMatchSummary(int eventId)
        {
            using (SqlConnection c = new SqlConnection(connString))
            {
                string sql = @"
                    SELECT m.CourtID, crt.CourtNumber, m.MatchOrder, m.MatchStatus,
                           COALESCE(pa1.Firstname, pw1.Firstname) AS P1Name,
                           COALESCE(pa2.Firstname, pw2.Firstname) AS P2Name,
                           COALESCE(paW.Firstname, pwW.Firstname) AS WinnerName
                    FROM tblMatch m
                    JOIN tblCourt crt ON m.CourtID = crt.CourtID
                    LEFT JOIN tblEventParticipant ep1 ON m.Player1_ParticipantID = ep1.EventParticipantID
                    LEFT JOIN tblPlayerAccount pa1 ON ep1.UserID = pa1.UserID
                    LEFT JOIN tblPlayerWalkIn pw1 ON ep1.WalkInID = pw1.WalkInID
                    LEFT JOIN tblEventParticipant ep2 ON m.Player2_ParticipantID = ep2.EventParticipantID
                    LEFT JOIN tblPlayerAccount pa2 ON ep2.UserID = pa2.UserID
                    LEFT JOIN tblPlayerWalkIn pw2 ON ep2.WalkInID = pw2.WalkInID
                    LEFT JOIN tblEventParticipant epW ON m.Winner_ParticipantID = epW.EventParticipantID
                    LEFT JOIN tblPlayerAccount paW ON epW.UserID = paW.UserID
                    LEFT JOIN tblPlayerWalkIn pwW ON epW.WalkInID = pwW.WalkInID
                    WHERE m.EventID = @EID AND m.MatchStatus != 'Cancelled'
                    ORDER BY crt.CourtNumber, m.MatchOrder";

                SqlCommand cmd = new SqlCommand(sql, c);
                cmd.Parameters.AddWithValue("@EID", eventId);
                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    rptSummary.DataSource = dt;
                    rptSummary.DataBind();
                    litNoSummary.Visible = false;
                }
                else
                {
                    rptSummary.DataSource = null;
                    rptSummary.DataBind();
                    litNoSummary.Visible = true;
                }
            }
        }

        protected void gvAllEvents_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditEvent")
            {
                hfActionType.Value = "EVT";
                hfActionCourt.Value = "0";
                hfActionEventID.Value = e.CommandArgument.ToString();
                btnHiddenTrigger_Click(null, null);
            }
        }

        protected void btnSaveEvent_Click(object sender, EventArgs e)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    if (string.IsNullOrWhiteSpace(txtEventTitle.Text))
                        throw new Exception("Event Title is required.");

                    DateTime dP = DateTime.Parse(txtDate.Text);
                    TimeSpan sT = TimeSpan.Parse(ddlStartTime.SelectedValue);
                    TimeSpan eT = TimeSpan.Parse(ddlEndTime.SelectedValue);

                    decimal.TryParse(txtFee.Text, out decimal fee);
                    int.TryParse(txtMaxPlayers.Text, out int maxP);

                    if (eT <= sT)
                        throw new Exception("End time must be after start time.");

                    List<int> selectedCourts = cblCourts.Items.Cast<ListItem>()
                                                .Where(i => i.Selected)
                                                .Select(i => int.Parse(i.Value))
                                                .ToList();

                    if (selectedCourts.Count == 0)
                        throw new Exception("Please select at least one court.");

                    // Validation for court restrictions
                    foreach (int cID in selectedCourts)
                    {
                        if (cID >= 1 && cID <= 4 && ddlSport.SelectedValue == "PICKLEBALL")
                        {
                            throw new Exception("Courts 1 to 4 are restricted strictly for Badminton use.");
                        }
                    }

                    int eID = hfIsEdit.Value == "true" ? int.Parse(hfEventID.Value) : 0;
                    DateTime finalStart = dP.Add(sT);
                    DateTime finalEnd = dP.Add(eT);

                    foreach (int cID in selectedCourts)
                    {
                        string checkSql = @"
                            SELECT COUNT(*) FROM tblActiveSession s 
                            LEFT JOIN tblCourtQueue q ON s.QueueID = q.QueueID 
                            WHERE s.CourtID = @CID 
                            AND s.StatusName = 'Active' 
                            AND s.StartTime < @EndT 
                            AND s.ExpectedEndTime > @StartT 
                            AND (@E = 0 OR q.EventID != @E)";

                        SqlCommand checkCmd = new SqlCommand(checkSql, conn, trans);
                        checkCmd.Parameters.AddWithValue("@CID", cID);
                        checkCmd.Parameters.AddWithValue("@EndT", finalEnd);
                        checkCmd.Parameters.AddWithValue("@StartT", finalStart);
                        checkCmd.Parameters.AddWithValue("@E", eID);

                        if ((int)checkCmd.ExecuteScalar() > 0)
                        {
                            throw new Exception($"Time conflict on Court {cID}. Please adjust schedule.");
                        }
                    }

                    if (eID > 0)
                    {
                        SqlCommand updateEvt = new SqlCommand("UPDATE tblEvent SET Title = @Title, SportName = @Sport, RegistrationFee = @Fee, MaxPlayers = @Max WHERE EventID = @EID", conn, trans);
                        updateEvt.Parameters.AddWithValue("@Title", txtEventTitle.Text.Trim());
                        updateEvt.Parameters.AddWithValue("@Sport", ddlSport.SelectedValue.ToLower());
                        updateEvt.Parameters.AddWithValue("@Fee", fee);
                        updateEvt.Parameters.AddWithValue("@Max", maxP);
                        updateEvt.Parameters.AddWithValue("@EID", eID);
                        updateEvt.ExecuteNonQuery();

                        new SqlCommand("DELETE FROM tblActiveSession WHERE QueueID IN (SELECT QueueID FROM tblCourtQueue WHERE EventID = @EID)", conn, trans) { Parameters = { new System.Data.SqlClient.SqlParameter("@EID", eID) } }.ExecuteNonQuery();
                        new SqlCommand("DELETE FROM tblCourtQueue WHERE EventID = @EID", conn, trans) { Parameters = { new System.Data.SqlClient.SqlParameter("@EID", eID) } }.ExecuteNonQuery();
                        new SqlCommand("DELETE FROM tblEventCourtPool WHERE EventID = @EID", conn, trans) { Parameters = { new System.Data.SqlClient.SqlParameter("@EID", eID) } }.ExecuteNonQuery();
                    }
                    else
                    {
                        int staffID = Convert.ToInt32(Session["StaffID"] ?? 0);
                        if (staffID <= 0) staffID = 1;
                        string insertEvtSql = "INSERT INTO tblEvent (Title, SportName, EventDate, CreatedByStaffID, IsActive, RegistrationFee, MaxPlayers) VALUES (@Title, @Sport, @Date, @StaffID, 1, @Fee, @Max); SELECT SCOPE_IDENTITY();";
                        SqlCommand cmdEvt = new SqlCommand(insertEvtSql, conn, trans);
                        cmdEvt.Parameters.AddWithValue("@Title", txtEventTitle.Text.Trim());
                        cmdEvt.Parameters.AddWithValue("@Sport", ddlSport.SelectedValue.ToLower());
                        cmdEvt.Parameters.AddWithValue("@Date", dP);
                        cmdEvt.Parameters.AddWithValue("@StaffID", staffID);
                        cmdEvt.Parameters.AddWithValue("@Fee", fee);
                        cmdEvt.Parameters.AddWithValue("@Max", maxP);
                        eID = Convert.ToInt32(cmdEvt.ExecuteScalar());
                        hfEventID.Value = eID.ToString();
                    }

                    foreach (int cID in selectedCourts)
                    {
                        SqlCommand cmdPool = new SqlCommand("INSERT INTO tblEventCourtPool (EventID, CourtID) VALUES (@EID, @CID)", conn, trans);
                        cmdPool.Parameters.AddWithValue("@EID", eID);
                        cmdPool.Parameters.AddWithValue("@CID", cID);
                        cmdPool.ExecuteNonQuery();

                        string qSql = "INSERT INTO tblCourtQueue (CourtID, EventID, QueueDate, QueueTypeName, QueueNumber, StatusName, LastModifiedByStaffID) VALUES (@CID, @EID, @Date, 'Queue', 1, 'Active', 1); SELECT SCOPE_IDENTITY();";
                        SqlCommand cmdQueue = new SqlCommand(qSql, conn, trans);
                        cmdQueue.Parameters.AddWithValue("@CID", cID);
                        cmdQueue.Parameters.AddWithValue("@EID", eID);
                        cmdQueue.Parameters.AddWithValue("@Date", dP);
                        int qID = Convert.ToInt32(cmdQueue.ExecuteScalar());

                        string sSql = "INSERT INTO tblActiveSession (CourtID, QueueID, StartTime, ExpectedEndTime, StatusName) VALUES (@CID, @QID, @StartT, @EndT, 'Active')";
                        SqlCommand cmdSess = new SqlCommand(sSql, conn, trans);
                        cmdSess.Parameters.AddWithValue("@CID", cID);
                        cmdSess.Parameters.AddWithValue("@QID", qID);
                        cmdSess.Parameters.AddWithValue("@StartT", finalStart);
                        cmdSess.Parameters.AddWithValue("@EndT", finalEnd);
                        cmdSess.ExecuteNonQuery();
                    }

                    trans.Commit();
                    RefreshGrid();
                    LoadAllEvents();

                    if (hfIsEdit.Value == "true")
                    {
                        LoadMatchSummary(eID);
                        ShowAlert("success", "Saved!", "Event settings updated.");
                    }
                    else
                    {
                        hfActionType.Value = "EVT";
                        hfActionCourt.Value = "0";
                        hfActionEventID.Value = eID.ToString();
                        btnHiddenTrigger_Click(null, null);
                        ShowAlert("success", "Created!", "Event scheduled successfully. Courts assigned.");
                    }
                }
                catch (Exception ex)
                {
                    if (trans != null) trans.Rollback();
                    ShowAlert("error", "Failed to Save", ex.Message);
                }
            }
        }

        protected void btnRemoveSlot_Click(object sender, EventArgs e)
        {
            int.TryParse(hfEventID.Value, out int eId);
            int.TryParse(hfActionCourt.Value, out int cId);
            if (eId == 0 || cId == 0) return;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    SqlCommand cmd1 = new SqlCommand("DELETE FROM tblActiveSession WHERE CourtID = @CID AND QueueID IN (SELECT QueueID FROM tblCourtQueue WHERE EventID = @EID)", conn, trans);
                    cmd1.Parameters.AddWithValue("@CID", cId);
                    cmd1.Parameters.AddWithValue("@EID", eId);
                    cmd1.ExecuteNonQuery();
                    SqlCommand cmd2 = new SqlCommand("DELETE FROM tblCourtQueue WHERE CourtID = @CID AND EventID = @EID", conn, trans);
                    cmd2.Parameters.AddWithValue("@CID", cId);
                    cmd2.Parameters.AddWithValue("@EID", eId);
                    cmd2.ExecuteNonQuery();
                    SqlCommand cmd3 = new SqlCommand("DELETE FROM tblEventCourtPool WHERE CourtID = @CID AND EventID = @EID", conn, trans);
                    cmd3.Parameters.AddWithValue("@CID", cId);
                    cmd3.Parameters.AddWithValue("@EID", eId);
                    cmd3.ExecuteNonQuery();

                    trans.Commit();
                    RefreshGrid();
                    LoadAllEvents();

                    ShowAlert("success", "Court Removed", $"Court {cId} has been successfully removed from this event schedule.");

                    pnlEventForm.Visible = false;
                    pnlEmptyState.Visible = true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    ShowAlert("error", "Error", ex.Message);
                }
            }
        }

        protected void btnCancelEvent_Click(object sender, EventArgs e)
        {
            int.TryParse(hfEventID.Value, out int eId);
            if (eId == 0) return;

            using (SqlConnection c = new SqlConnection(connString))
            {
                c.Open();
                SqlTransaction t = c.BeginTransaction();
                try
                {
                    SqlCommand cmdSession = new SqlCommand("UPDATE tblActiveSession SET StatusName='Cancelled' WHERE QueueID IN (SELECT QueueID FROM tblCourtQueue WHERE EventID=@EID)", c, t);
                    cmdSession.Parameters.AddWithValue("@EID", eId);
                    cmdSession.ExecuteNonQuery();

                    SqlCommand cmdQueue = new SqlCommand("UPDATE tblCourtQueue SET StatusName='Cancelled' WHERE EventID=@EID", c, t);
                    cmdQueue.Parameters.AddWithValue("@EID", eId);
                    cmdQueue.ExecuteNonQuery();

                    SqlCommand cmdPart = new SqlCommand("UPDATE tblEventParticipant SET StatusName='Cancelled' WHERE EventID=@EID", c, t);
                    cmdPart.Parameters.AddWithValue("@EID", eId);
                    cmdPart.ExecuteNonQuery();

                    SqlCommand cmdMatch = new SqlCommand("UPDATE tblMatch SET MatchStatus='Cancelled' WHERE EventID=@EID", c, t);
                    cmdMatch.Parameters.AddWithValue("@EID", eId);
                    cmdMatch.ExecuteNonQuery();

                    SqlCommand cmdEvt = new SqlCommand("UPDATE tblEvent SET IsActive=0 WHERE EventID=@EID", c, t);
                    cmdEvt.Parameters.AddWithValue("@EID", eId);
                    cmdEvt.ExecuteNonQuery();

                    t.Commit();
                    ScriptManager.RegisterStartupScript(this, GetType(), "reloadDel", "Swal.fire({ icon: 'success', title: 'Deleted!', text: 'Event has been cancelled.', confirmButtonColor: '#1e3a8a'}).then(() => { window.location.href=window.location.href; });", true);
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    ShowAlert("error", "Error Cancelling Event", ex.Message);
                }
            }
        }

        private void ShowAlert(string icon, string title, string text)
        {
            string cleanText = text.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
            ScriptManager.RegisterStartupScript(this, GetType(), "swalTrigger", $"showSweetAlert('{icon}', '{title}', '{cleanText}');", true);
        }
    }
}