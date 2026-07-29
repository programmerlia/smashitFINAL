using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;

namespace Smash_IT.adminpage
{
    public partial class admin_manage_queue : System.Web.UI.Page
    {
        string connString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;
        private DataTable dtAllMatches;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadActiveEvents();
                LoadEquipmentDropdown();
                LoadRegisteredUsers();
            }
        }

        // ==========================================
        // DATA POPULATION
        // ==========================================
        private void LoadActiveEvents()
        {
            try
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT EventID, Title + ' (' + FORMAT(EventDate, 'MMM dd') + ')' as DisplayText FROM tblEvent WHERE IsActive = 1 ORDER BY EventDate DESC", c);
                    DataTable dt = new DataTable(); da.Fill(dt);
                    ddlActiveEvents.DataSource = dt; ddlActiveEvents.DataTextField = "DisplayText"; ddlActiveEvents.DataValueField = "EventID"; ddlActiveEvents.DataBind();
                    ddlActiveEvents.Items.Insert(0, new ListItem("-- Select Event Board --", "0"));
                }
            }
            catch (Exception ex) { ShowAlert("error", "Initialization Error", ex.ToString()); }
        }

        private void LoadEquipmentDropdown()
        {
            try
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    string sql = @"
                        SELECT ModelID, 
                        EquipmentType + ' (P' + CAST(ISNULL(DefaultRentalPrice, DefaultSellPrice) AS VARCHAR) + ') - ' + 
                        CASE 
                            WHEN ItemCategory = 'Rental' THEN CAST((SELECT COUNT(*) FROM tblEquipmentItem ei WHERE ei.ModelID = m.ModelID AND ei.IsDeleted = 0 AND ei.ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL)) AS VARCHAR) + ' Available'
                            ELSE CAST(ISNULL(ConsumableQty, 0) AS VARCHAR) + ' in Stock'
                        END AS DisplayName
                        FROM tblEquipmentModel m WHERE IsDeleted = 0";

                    SqlDataAdapter da = new SqlDataAdapter(sql, c);
                    DataTable dt = new DataTable(); da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        ddlEquipment.DataSource = dt; ddlEquipment.DataTextField = "DisplayName"; ddlEquipment.DataValueField = "ModelID"; ddlEquipment.DataBind();
                        phEquipmentInputs.Visible = true;
                    }
                    else { phEquipmentInputs.Visible = false; }
                }
            }
            catch (Exception ex) { ShowAlert("error", "Equipment Load Error", ex.ToString()); }
        }

        private void LoadRegisteredUsers()
        {
            ddlRegisteredUsers.Items.Clear();
            ddlRegisteredUsers.Items.Insert(0, new ListItem("-- Search Members --", "0"));

            if (!int.TryParse(ddlActiveEvents.SelectedValue, out int eventId) || eventId <= 0)
                return;

            using (SqlConnection c = new SqlConnection(connString))
            {
                string sql = @"
            SELECT pa.UserID,
                   pa.Firstname + ' ' + pa.Lastname AS DisplayName
            FROM tblPlayerAccount pa
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM tblEventParticipant ep
                LEFT JOIN tblPlayerAccount epa ON ep.UserID = epa.UserID
                LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID
                WHERE ep.EventID = @EventID
                  AND ep.StatusName != 'Cancelled'
                  AND LTRIM(RTRIM(
                        COALESCE(epa.Firstname, pw.Firstname, '') + ' ' +
                        COALESCE(epa.Lastname,  pw.Lastname,  '')
                  )) = LTRIM(RTRIM(pa.Firstname + ' ' + pa.Lastname))
            )
            ORDER BY pa.Firstname, pa.Lastname";

                SqlDataAdapter da = new SqlDataAdapter(sql, c);
                da.SelectCommand.Parameters.AddWithValue("@EventID", eventId);

                DataTable dt = new DataTable();
                da.Fill(dt);

                ddlRegisteredUsers.DataSource = dt;
                ddlRegisteredUsers.DataTextField = "DisplayName";
                ddlRegisteredUsers.DataValueField = "UserID";
                ddlRegisteredUsers.DataBind();

                ddlRegisteredUsers.Items.Insert(0, new ListItem("-- Search Members --", "0"));
            }
        }
        private void LoadFilteredDropdowns(int eventId)
        {
            using (SqlConnection c = new SqlConnection(connString))
            {
                DataTable dtCourts = new DataTable();
                new SqlDataAdapter($"SELECT c.CourtID, 'Court ' + CAST(c.CourtNumber AS VARCHAR) as CourtName FROM tblCourt c JOIN tblEventCourtPool p ON c.CourtID = p.CourtID WHERE p.EventID={eventId}", c).Fill(dtCourts);
                ddlManualCourt.DataSource = dtCourts; ddlManualCourt.DataTextField = "CourtName"; ddlManualCourt.DataValueField = "CourtID"; ddlManualCourt.DataBind();

                // ONLY PAID & NOT CURRENTLY SCHEDULED OR PENDING
                DataTable dtAvailable = new DataTable();
                string sqlPlayers = $@"
                    SELECT ep.EventParticipantID, COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname) AS PlayerName 
                    FROM tblEventParticipant ep 
                    LEFT JOIN tblPlayerAccount pa ON ep.UserID = pa.UserID LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID 
                    WHERE ep.EventID = {eventId} AND ep.StatusName = 'Completed' 
                    AND ep.EventParticipantID NOT IN (
                        SELECT Player1_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player1_ParticipantID IS NOT NULL AND EventID={eventId}
                        UNION SELECT Player2_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player2_ParticipantID IS NOT NULL AND EventID={eventId}
                        UNION SELECT Player3_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player3_ParticipantID IS NOT NULL AND EventID={eventId}
                        UNION SELECT Player4_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player4_ParticipantID IS NOT NULL AND EventID={eventId}
                    ) ORDER BY PlayerName";

                new SqlDataAdapter(sqlPlayers, c).Fill(dtAvailable);
                var dropDowns = new[] { ddlT1P1, ddlT1P2, ddlT2P1, ddlT2P2 };
                foreach (var ddl in dropDowns)
                {
                    ddl.DataSource = dtAvailable; ddl.DataTextField = "PlayerName"; ddl.DataValueField = "EventParticipantID"; ddl.DataBind();
                    ddl.Items.Insert(0, new ListItem("-- Select Player --", ""));
                }

                // ALL PAID PLAYERS (For Champion Selection)
                DataTable dtAllPaid = new DataTable();
                new SqlDataAdapter($@"SELECT ep.EventParticipantID, COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname) AS PlayerName FROM tblEventParticipant ep LEFT JOIN tblPlayerAccount pa ON ep.UserID = pa.UserID LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID WHERE ep.EventID = {eventId} AND ep.StatusName = 'Completed' ORDER BY PlayerName", c).Fill(dtAllPaid);
                ddlChampion.DataSource = dtAllPaid; ddlChampion.DataTextField = "PlayerName"; ddlChampion.DataValueField = "EventParticipantID"; ddlChampion.DataBind(); ddlChampion.Items.Insert(0, new ListItem("-- Select Champion --", ""));
                ddlRunnerUp.DataSource = dtAllPaid; ddlRunnerUp.DataTextField = "PlayerName"; ddlRunnerUp.DataValueField = "EventParticipantID"; ddlRunnerUp.DataBind(); ddlRunnerUp.Items.Insert(0, new ListItem("-- Select Runner Up --", ""));
            }
        }

        // ==========================================
        // WORKSPACE REFRESH
        // ==========================================
        protected void ddlActiveEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ddlActiveEvents.SelectedValue, out int eventId) && eventId > 0)
            {
                pnlEventWorkspace.Visible = true; RefreshWorkspace(eventId);
            }
            else { pnlEventWorkspace.Visible = false; }
        }

        private void RefreshWorkspace(int eventId)
        {
            try
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();

                    // Stats & Champion
                    SqlCommand cmdEvt = new SqlCommand(@"
                        SELECT e.MaxPlayers, e.RegistrationFee, 
                               COALESCE(pa1.Firstname + ' ' + pa1.Lastname, pw1.Firstname + ' ' + pw1.Lastname) AS ChampName,
                               COALESCE(pa2.Firstname + ' ' + pa2.Lastname, pw2.Firstname + ' ' + pw2.Lastname) AS RunName
                        FROM tblEvent e
                        LEFT JOIN tblEventParticipant ep1 ON e.Champion_ParticipantID = ep1.EventParticipantID
                        LEFT JOIN tblPlayerAccount pa1 ON ep1.UserID = pa1.UserID LEFT JOIN tblPlayerWalkIn pw1 ON ep1.WalkInID = pw1.WalkInID
                        LEFT JOIN tblEventParticipant ep2 ON e.RunnerUp_ParticipantID = ep2.EventParticipantID
                        LEFT JOIN tblPlayerAccount pa2 ON ep2.UserID = pa2.UserID LEFT JOIN tblPlayerWalkIn pw2 ON ep2.WalkInID = pw2.WalkInID
                        WHERE e.EventID = @EID", c);
                    cmdEvt.Parameters.AddWithValue("@EID", eventId);
                    using (SqlDataReader dr = cmdEvt.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            litMaxPlayers.Text = dr["MaxPlayers"].ToString();
                            litTotalRevenue.Text = Convert.ToDecimal(dr["RegistrationFee"]).ToString("0");
                            string champ = dr["ChampName"].ToString();
                            if (!string.IsNullOrEmpty(champ))
                            {
                                pnlCrown.Visible = true; litChampName.Text = champ; litRunnerUpName.Text = dr["RunName"].ToString() == "" ? "None" : dr["RunName"].ToString();
                            }
                            else { pnlCrown.Visible = false; }
                        }
                    }

                    litJoined.Text = new SqlCommand($"SELECT COUNT(*) FROM tblEventParticipant WHERE EventID={eventId} AND StatusName != 'Cancelled'", c).ExecuteScalar().ToString();
                    litPaid.Text = new SqlCommand($"SELECT COUNT(*) FROM tblEventParticipant WHERE EventID={eventId} AND StatusName = 'Completed'", c).ExecuteScalar().ToString();

                    // Roster
                    DataTable dtParts = new DataTable();
                    new SqlDataAdapter($"SELECT ep.EventParticipantID, ep.WalkInID, ep.UserID, ep.StatusName, ep.OrderNo, COALESCE(pa.Firstname + ' ' + pa.Lastname, pw.Firstname + ' ' + pw.Lastname) AS PlayerName FROM tblEventParticipant ep LEFT JOIN tblPlayerAccount pa ON ep.UserID = pa.UserID LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID WHERE ep.EventID = {eventId} AND ep.StatusName != 'Cancelled' ORDER BY ep.OrderNo", c).Fill(dtParts);
                    gvParticipants.DataSource = dtParts; gvParticipants.DataBind();

                    // Matches
                    dtAllMatches = new DataTable();
                    new SqlDataAdapter($@"SELECT m.MatchID, m.CourtID, c.CourtNumber, m.MatchStatus, m.MatchOrder, m.MatchType, m.BracketPhase, m.Player1_ParticipantID, m.Player2_ParticipantID, m.Player3_ParticipantID, m.Player4_ParticipantID,
                           COALESCE(pa1.Firstname, pw1.Firstname) AS T1P1Name, COALESCE(pa3.Firstname, pw3.Firstname) AS T1P2Name, COALESCE(pa2.Firstname, pw2.Firstname) AS T2P1Name, COALESCE(pa4.Firstname, pw4.Firstname) AS T2P2Name, 
                           COALESCE(paW.Firstname, pwW.Firstname) AS WinnerName, m.Winner_ParticipantID, m.Team1_Score, m.Team2_Score
                           FROM tblMatch m INNER JOIN tblCourt c ON m.CourtID = c.CourtID
                           LEFT JOIN tblEventParticipant ep1 ON m.Player1_ParticipantID = ep1.EventParticipantID LEFT JOIN tblPlayerAccount pa1 ON ep1.UserID = pa1.UserID LEFT JOIN tblPlayerWalkIn pw1 ON ep1.WalkInID = pw1.WalkInID
                           LEFT JOIN tblEventParticipant ep2 ON m.Player2_ParticipantID = ep2.EventParticipantID LEFT JOIN tblPlayerAccount pa2 ON ep2.UserID = pa2.UserID LEFT JOIN tblPlayerWalkIn pw2 ON ep2.WalkInID = pw2.WalkInID
                           LEFT JOIN tblEventParticipant ep3 ON m.Player3_ParticipantID = ep3.EventParticipantID LEFT JOIN tblPlayerAccount pa3 ON ep3.UserID = pa3.UserID LEFT JOIN tblPlayerWalkIn pw3 ON ep3.WalkInID = pw3.WalkInID
                           LEFT JOIN tblEventParticipant ep4 ON m.Player4_ParticipantID = ep4.EventParticipantID LEFT JOIN tblPlayerAccount pa4 ON ep4.UserID = pa4.UserID LEFT JOIN tblPlayerWalkIn pw4 ON ep4.WalkInID = pw4.WalkInID
                           LEFT JOIN tblEventParticipant epW ON m.Winner_ParticipantID = epW.EventParticipantID LEFT JOIN tblPlayerAccount paW ON epW.UserID = paW.UserID LEFT JOIN tblPlayerWalkIn pwW ON epW.WalkInID = pwW.WalkInID
                           WHERE m.EventID = {eventId} AND m.MatchStatus != 'Cancelled' ORDER BY m.MatchOrder ASC", c).Fill(dtAllMatches);

                    DataTable dtCourts = new DataTable();
                    new SqlDataAdapter($"SELECT CourtID, CourtNumber FROM tblCourt WHERE CourtID IN (SELECT CourtID FROM tblEventCourtPool WHERE EventID={eventId})", c).Fill(dtCourts);
                    rptCourts.DataSource = dtCourts; rptCourts.DataBind();

                    pnlNoMatches.Visible = dtAllMatches.Select("MatchStatus IN ('Scheduled', 'Pending')").Length == 0;
                    LoadFilteredDropdowns(eventId);

                    // Leaderboard & Match History Builders
                    DataTable dtHist = new DataTable();
                    dtHist.Columns.Add("MatchOrder"); dtHist.Columns.Add("MatchDesc"); dtHist.Columns.Add("Score"); dtHist.Columns.Add("WinnerName"); dtHist.Columns.Add("Phase");

                    var teamWins = new Dictionary<string, int>();

                    foreach (DataRow r in dtAllMatches.Select("MatchStatus = 'Completed'", "MatchOrder DESC"))
                    {
                        string t1 = r["T1P1Name"].ToString() + (r["T1P2Name"].ToString() != "" ? " & " + r["T1P2Name"] : "");
                        string t2 = r["T2P1Name"].ToString() + (r["T2P2Name"].ToString() != "" ? " & " + r["T2P2Name"] : "");
                        string matchDesc = $"{t1} vs {(t2 == "" ? "BYE" : t2)}";

                        string scoreStr = $"{r["Team1_Score"]} - {r["Team2_Score"]}";
                        if (scoreStr == " - " || scoreStr.Trim() == "-") scoreStr = "N/A";

                        string winTeamStr = "";
                        if (r["Winner_ParticipantID"] != DBNull.Value)
                        {
                            int winId = Convert.ToInt32(r["Winner_ParticipantID"]);
                            if (winId == Convert.ToInt32(r["Player1_ParticipantID"])) winTeamStr = t1;
                            else if (r["Player2_ParticipantID"] != DBNull.Value && winId == Convert.ToInt32(r["Player2_ParticipantID"])) winTeamStr = t2;
                            else winTeamStr = r["WinnerName"].ToString();

                            if (!string.IsNullOrEmpty(winTeamStr))
                            {
                                if (teamWins.ContainsKey(winTeamStr)) teamWins[winTeamStr]++;
                                else teamWins[winTeamStr] = 1;
                            }
                        }

                        dtHist.Rows.Add(r["MatchOrder"], matchDesc, scoreStr, winTeamStr != "" ? "Team " + winTeamStr : "", r["BracketPhase"].ToString());
                    }
                    gvMatchSummary.DataSource = dtHist.Rows.Count > 0 ? dtHist : null; gvMatchSummary.DataBind();

                    DataTable dtLeader = new DataTable(); dtLeader.Columns.Add("PlayerName"); dtLeader.Columns.Add("Wins", typeof(int));
                    foreach (var kvp in teamWins.OrderByDescending(x => x.Value)) { dtLeader.Rows.Add(kvp.Key, kvp.Value); }
                    gvLeaderboard.DataSource = dtLeader.Rows.Count > 0 ? dtLeader : null; gvLeaderboard.DataBind();
                }
            }
            catch (Exception ex) { ShowAlert("error", "Refresh Failed", ex.ToString()); }
        }

        protected void gvParticipants_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                Literal litStatus = (Literal)e.Row.FindControl("litStatus");
                litStatus.Text = drv["StatusName"].ToString() == "Completed" ? "<span class='status-badge status-paid'>PAID</span>" : "<span class='status-badge status-pend'>PENDING</span>";
            }
        }

        protected void rptCourts_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView courtRow = (DataRowView)e.Item.DataItem;
                int courtId = Convert.ToInt32(courtRow["CourtID"]);

                Panel pnlActiveMatch = (Panel)e.Item.FindControl("pnlActiveMatch"); Panel pnlNoActive = (Panel)e.Item.FindControl("pnlNoActive");
                Label lblMatchType = (Label)e.Item.FindControl("lblMatchType"); Label lblPhase = (Label)e.Item.FindControl("lblPhase");
                Literal litT1P1 = (Literal)e.Item.FindControl("litT1P1"); Literal litT1P2 = (Literal)e.Item.FindControl("litT1P2");
                Literal litT2P1 = (Literal)e.Item.FindControl("litT2P1"); Literal litT2P2 = (Literal)e.Item.FindControl("litT2P2");
                Panel pnlT1P2 = (Panel)e.Item.FindControl("pnlT1P2"); Panel pnlT2P2 = (Panel)e.Item.FindControl("pnlT2P2");
                HiddenField hfActiveMatchID = (HiddenField)e.Item.FindControl("hfActiveMatchID"); DropDownList ddlActiveWinner = (DropDownList)e.Item.FindControl("ddlActiveWinner");

                DataRow[] scheduledMatches = dtAllMatches.Select($"CourtID = {courtId} AND MatchStatus IN ('Scheduled', 'Pending')", "MatchOrder ASC");

                if (scheduledMatches.Length > 0 && scheduledMatches[0]["MatchStatus"].ToString() == "Scheduled")
                {
                    DataRow cm = scheduledMatches[0];
                    pnlActiveMatch.Visible = true; pnlNoActive.Visible = false;

                    bool isDoubles = cm["MatchType"].ToString() == "Doubles";
                    lblMatchType.Text = isDoubles ? "DOUBLES" : "SINGLES";
                    lblPhase.Text = cm["BracketPhase"].ToString();
                    lblPhase.Visible = lblPhase.Text != "Casual";

                    litT1P1.Text = cm["T1P1Name"].ToString();
                    if (isDoubles && cm["T1P2Name"].ToString() != "") { litT1P2.Text = cm["T1P2Name"].ToString(); pnlT1P2.Visible = true; } else { pnlT1P2.Visible = false; }

                    litT2P1.Text = cm["T2P1Name"].ToString() == "" ? "BYE" : cm["T2P1Name"].ToString();
                    if (isDoubles && cm["T2P2Name"].ToString() != "") { litT2P2.Text = cm["T2P2Name"].ToString(); pnlT2P2.Visible = true; } else { pnlT2P2.Visible = false; }

                    hfActiveMatchID.Value = cm["MatchID"].ToString();

                    string t1Str = litT1P1.Text + (pnlT1P2.Visible ? " & " + litT1P2.Text : "");
                    string t2Str = litT2P1.Text + (pnlT2P2.Visible ? " & " + litT2P2.Text : "");

                    ddlActiveWinner.Items.Clear(); ddlActiveWinner.Items.Add(new ListItem("-- Declare Winner --", ""));
                    ddlActiveWinner.Items.Add(new ListItem("Team 1 (" + t1Str + ")", cm["Player1_ParticipantID"].ToString()));
                    if (cm["Player2_ParticipantID"].ToString() != "") ddlActiveWinner.Items.Add(new ListItem("Team 2 (" + t2Str + ")", cm["Player2_ParticipantID"].ToString()));
                }
                else { pnlActiveMatch.Visible = false; pnlNoActive.Visible = true; lblMatchType.Text = "OPEN"; lblPhase.Visible = false; }

                Repeater rptUpNext = (Repeater)e.Item.FindControl("rptUpNext"); Literal litNoQueue = (Literal)e.Item.FindControl("litNoQueue");
                int startIndex = (scheduledMatches.Length > 0 && scheduledMatches[0]["MatchStatus"].ToString() == "Scheduled") ? 1 : 0;

                if (scheduledMatches.Length > startIndex)
                {
                    DataTable dtNext = new DataTable(); dtNext.Columns.Add("Team1"); dtNext.Columns.Add("Team2");
                    for (int i = startIndex; i < scheduledMatches.Length; i++)
                    {
                        DataRow r = scheduledMatches[i];
                        string t1 = r["T1P1Name"].ToString() + (r["T1P2Name"].ToString() != "" ? " & " + r["T1P2Name"] : "");
                        string t2 = r["MatchStatus"].ToString() == "Pending" ? "TBD (Waiting)" : (r["T2P1Name"].ToString() + (r["T2P2Name"].ToString() != "" ? " & " + r["T2P2Name"] : ""));
                        dtNext.Rows.Add(t1, t2 == "" ? "BYE" : t2);
                    }
                    rptUpNext.DataSource = dtNext; rptUpNext.DataBind(); litNoQueue.Visible = false;
                }
                else { rptUpNext.DataSource = null; rptUpNext.DataBind(); litNoQueue.Visible = true; }
            }
        }

        // ==========================================
        // TOURNAMENT AUTOMATION ENGINE
        // ==========================================

        protected void ddlActiveWinner_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                DropDownList ddl = (DropDownList)sender; if (ddl.SelectedValue == "") return;
                RepeaterItem rItem = (RepeaterItem)ddl.NamingContainer;
                HiddenField hfMatch = (HiddenField)rItem.FindControl("hfActiveMatchID");
                TextBox txtScoreT1 = (TextBox)rItem.FindControl("txtScoreT1");
                TextBox txtScoreT2 = (TextBox)rItem.FindControl("txtScoreT2");

                int matchId = int.Parse(hfMatch.Value);
                int winId = int.Parse(ddl.SelectedValue);
                int eventId = int.Parse(ddlActiveEvents.SelectedValue);

                int loseP1 = 0; int loseP2 = 0; int winP2 = 0;
                string phase = ""; string matchType = "";

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();

                    SqlCommand cmdInfo = new SqlCommand("SELECT Player1_ParticipantID, Player2_ParticipantID, Player3_ParticipantID, Player4_ParticipantID, BracketPhase, MatchType FROM tblMatch WHERE MatchID=@M", c);
                    cmdInfo.Parameters.AddWithValue("@M", matchId);
                    using (SqlDataReader dr = cmdInfo.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            phase = dr["BracketPhase"].ToString(); matchType = dr["MatchType"].ToString();
                            int p1 = Convert.ToInt32(dr["Player1_ParticipantID"]); int p2 = dr["Player2_ParticipantID"] != DBNull.Value ? Convert.ToInt32(dr["Player2_ParticipantID"]) : 0;
                            int p3 = dr["Player3_ParticipantID"] != DBNull.Value ? Convert.ToInt32(dr["Player3_ParticipantID"]) : 0; int p4 = dr["Player4_ParticipantID"] != DBNull.Value ? Convert.ToInt32(dr["Player4_ParticipantID"]) : 0;
                            if (winId == p1) { loseP1 = p2; loseP2 = p4; winP2 = p3; } else { loseP1 = p1; loseP2 = p3; winP2 = p4; }
                        }
                    }

                    SqlCommand upMatch = new SqlCommand("UPDATE tblMatch SET Winner_ParticipantID=@WID, MatchStatus='Completed', Team1_Score=@T1S, Team2_Score=@T2S WHERE MatchID=@MID", c);
                    upMatch.Parameters.AddWithValue("@WID", winId); upMatch.Parameters.AddWithValue("@MID", matchId);
                    upMatch.Parameters.AddWithValue("@T1S", txtScoreT1.Text.Trim()); upMatch.Parameters.AddWithValue("@T2S", txtScoreT2.Text.Trim());
                    upMatch.ExecuteNonQuery();

                    // ADVANCE ENGINE
                    if (phase != "Casual" && phase != "Finals")
                    {
                        AdvanceTeam(eventId, matchType, phase, winId, winP2, c);
                        if (phase == "Winners Bracket" && loseP1 > 0) { AdvanceTeam(eventId, matchType, "Losers Bracket", loseP1, loseP2, c); }
                        MergeToGrandFinals(eventId, c);
                    }
                    else if (phase == "Finals")
                    {
                        SqlCommand cmdC = new SqlCommand("UPDATE tblEvent SET Champion_ParticipantID=@C, RunnerUp_ParticipantID=@R WHERE EventID=@E", c);
                        cmdC.Parameters.AddWithValue("@C", winId); cmdC.Parameters.AddWithValue("@R", loseP1 > 0 ? (object)loseP1 : DBNull.Value); cmdC.Parameters.AddWithValue("@E", eventId); cmdC.ExecuteNonQuery();
                        ShowAlert("success", "Tournament Concluded", "The Grand Finals have finished! Champion crowned.");
                    }
                }
                RefreshWorkspace(eventId);
            }
            catch (Exception ex) { ShowAlert("error", "Engine Error", ex.ToString()); }
        }

        private void AdvanceTeam(int eventId, string matchType, string phase, int winP1, int winP2, SqlConnection c)
        {
            if (winP1 == 0) return;

            string sqlFind = "SELECT TOP 1 MatchID FROM tblMatch WHERE EventID=@E AND BracketPhase=@P AND MatchStatus='Pending' AND Player2_ParticipantID IS NULL ORDER BY MatchOrder ASC";
            SqlCommand cmdFind = new SqlCommand(sqlFind, c);
            cmdFind.Parameters.AddWithValue("@E", eventId); cmdFind.Parameters.AddWithValue("@P", phase);
            object pendingMatchId = cmdFind.ExecuteScalar();

            if (pendingMatchId != null)
            {
                SqlCommand cmdUpd = new SqlCommand("UPDATE tblMatch SET Player2_ParticipantID=@P2, Player4_ParticipantID=@P4, MatchStatus='Scheduled' WHERE MatchID=@M", c);
                cmdUpd.Parameters.AddWithValue("@P2", winP1); cmdUpd.Parameters.AddWithValue("@P4", winP2 > 0 ? (object)winP2 : DBNull.Value); cmdUpd.Parameters.AddWithValue("@M", pendingMatchId); cmdUpd.ExecuteNonQuery();
            }
            else
            {
                int order = Convert.ToInt32(new SqlCommand($"SELECT ISNULL(MAX(MatchOrder),0)+1 FROM tblMatch WHERE EventID={eventId}", c).ExecuteScalar());
                object courtObj = new SqlCommand($"SELECT TOP 1 CourtID FROM tblEventCourtPool WHERE EventID={eventId}", c).ExecuteScalar();
                int courtId = courtObj != null ? Convert.ToInt32(courtObj) : 0;

                if (courtId > 0)
                {
                    SqlCommand cmdIns = new SqlCommand("INSERT INTO tblMatch (EventID, CourtID, MatchType, BracketPhase, Player1_ParticipantID, Player3_ParticipantID, MatchStatus, MatchOrder) VALUES (@E, @C, @MT, @P, @P1, @P3, 'Pending', @O)", c);
                    cmdIns.Parameters.AddWithValue("@E", eventId); cmdIns.Parameters.AddWithValue("@C", courtId); cmdIns.Parameters.AddWithValue("@MT", matchType);
                    cmdIns.Parameters.AddWithValue("@P", phase); cmdIns.Parameters.AddWithValue("@P1", winP1); cmdIns.Parameters.AddWithValue("@P3", winP2 > 0 ? (object)winP2 : DBNull.Value); cmdIns.Parameters.AddWithValue("@O", order); cmdIns.ExecuteNonQuery();
                }
            }
        }

        private void MergeToGrandFinals(int eventId, SqlConnection c)
        {
            int activeMatches = Convert.ToInt32(new SqlCommand($"SELECT COUNT(*) FROM tblMatch WHERE EventID={eventId} AND MatchStatus='Scheduled' AND BracketPhase IN ('Winners Bracket', 'Losers Bracket')", c).ExecuteScalar());
            if (activeMatches == 0)
            {
                DataTable dtP = new DataTable();
                new SqlDataAdapter($"SELECT MatchID, MatchType, Player1_ParticipantID, Player3_ParticipantID, BracketPhase FROM tblMatch WHERE EventID={eventId} AND MatchStatus='Pending' AND Player2_ParticipantID IS NULL", c).Fill(dtP);
                if (dtP.Rows.Count == 2)
                {
                    bool hasWinners = dtP.Select("BracketPhase = 'Winners Bracket'").Length == 1;
                    bool hasLosers = dtP.Select("BracketPhase = 'Losers Bracket'").Length == 1;
                    if (hasWinners && hasLosers)
                    {
                        DataRow winRow = dtP.Select("BracketPhase = 'Winners Bracket'")[0];
                        DataRow losRow = dtP.Select("BracketPhase = 'Losers Bracket'")[0];
                        SqlCommand cmdUpd = new SqlCommand("UPDATE tblMatch SET BracketPhase='Finals', MatchStatus='Scheduled', Player2_ParticipantID=@P2, Player4_ParticipantID=@P4 WHERE MatchID=@M", c);
                        cmdUpd.Parameters.AddWithValue("@P2", losRow["Player1_ParticipantID"]);
                        cmdUpd.Parameters.AddWithValue("@P4", losRow["Player3_ParticipantID"] != DBNull.Value ? losRow["Player3_ParticipantID"] : DBNull.Value);
                        cmdUpd.Parameters.AddWithValue("@M", winRow["MatchID"]); cmdUpd.ExecuteNonQuery();
                        new SqlCommand($"DELETE FROM tblMatch WHERE MatchID={losRow["MatchID"]}", c).ExecuteNonQuery();
                    }
                }
            }
        }

        // ==========================================
        // ROSTER & BOARD ACTIONS
        // ==========================================
        protected void btnAddMember_Click(object sender, EventArgs e)
        {
            try
            {
                if (ddlRegisteredUsers.SelectedValue == "0")
                {
                    ShowAlert("warning", "No Member Selected", "Please select a member first.");
                    return;
                }

                if (!int.TryParse(ddlActiveEvents.SelectedValue, out int eventId) || eventId <= 0)
                {
                    ShowAlert("warning", "No Event Selected", "Please select an active event first.");
                    return;
                }

                int userId = int.Parse(ddlRegisteredUsers.SelectedValue);

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();

                    string firstName = "";
                    string lastName = "";
                    string fullName = "";

                    // Get selected member name
                    using (SqlCommand cmdUser = new SqlCommand(@"
                SELECT Firstname, Lastname
                FROM tblPlayerAccount
                WHERE UserID = @UserID", c))
                    {
                        cmdUser.Parameters.AddWithValue("@UserID", userId);

                        using (SqlDataReader dr = cmdUser.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                ShowAlert("error", "Member Not Found", "The selected member could not be found.");
                                return;
                            }

                            firstName = dr["Firstname"].ToString().Trim();
                            lastName = dr["Lastname"].ToString().Trim();
                            fullName = (firstName + " " + lastName).Trim();
                        }
                    }

                    // Check duplicate FULL NAME in this event
                    using (SqlCommand cmdCheck = new SqlCommand(@"
                SELECT COUNT(*)
                FROM tblEventParticipant ep
                LEFT JOIN tblPlayerAccount pa ON ep.UserID = pa.UserID
                LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID
                WHERE ep.EventID = @EventID
                  AND ep.StatusName != 'Cancelled'
                  AND LTRIM(RTRIM(
                        COALESCE(pa.Firstname, pw.Firstname, '') + ' ' +
                        COALESCE(pa.Lastname,  pw.Lastname,  '')
                  )) = @FullName", c))
                    {
                        cmdCheck.Parameters.AddWithValue("@EventID", eventId);
                        cmdCheck.Parameters.AddWithValue("@FullName", fullName);

                        int existing = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (existing > 0)
                        {
                            ShowAlert("warning", "Duplicate Name Not Allowed", fullName + " is already added to this event.");
                            return;
                        }
                    }

                    // Next order number
                    int order;
                    using (SqlCommand cmdOrder = new SqlCommand(@"
                SELECT ISNULL(MAX(OrderNo), 0) + 1
                FROM tblEventParticipant
                WHERE EventID = @EventID", c))
                    {
                        cmdOrder.Parameters.AddWithValue("@EventID", eventId);
                        order = Convert.ToInt32(cmdOrder.ExecuteScalar());
                    }

                    // Insert member
                    using (SqlCommand cmdInsert = new SqlCommand(@"
                INSERT INTO tblEventParticipant (EventID, UserID, OrderNo, StatusName)
                VALUES (@EventID, @UserID, @OrderNo, 'Waiting')", c))
                    {
                        cmdInsert.Parameters.AddWithValue("@EventID", eventId);
                        cmdInsert.Parameters.AddWithValue("@UserID", userId);
                        cmdInsert.Parameters.AddWithValue("@OrderNo", order);
                        cmdInsert.ExecuteNonQuery();
                    }

                    ShowAlert("success", "Added", fullName + " was added successfully.");
                }

                RefreshWorkspace(eventId);
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Error", ex.Message);
            }
        }

        protected void btnAddMassPlayers_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtMassPlayers.Text))
                    return;

                int eventId = int.Parse(ddlActiveEvents.SelectedValue);

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();

                    foreach (string raw in txtMassPlayers.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string cleanName = raw.Trim();
                        if (string.IsNullOrWhiteSpace(cleanName))
                            continue;

                        string[] parts = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0)
                            continue;

                        string firstName = parts[0].Trim();
                        string lastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)).Trim() : "";
                        string fullName = (firstName + " " + lastName).Trim();

                        // Check duplicate FULL NAME in event
                        using (SqlCommand cmdCheck = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM tblEventParticipant ep
                    LEFT JOIN tblPlayerAccount pa ON ep.UserID = pa.UserID
                    LEFT JOIN tblPlayerWalkIn pw ON ep.WalkInID = pw.WalkInID
                    WHERE ep.EventID = @EventID
                      AND ep.StatusName != 'Cancelled'
                      AND LTRIM(RTRIM(
                            COALESCE(pa.Firstname, pw.Firstname, '') + ' ' +
                            COALESCE(pa.Lastname,  pw.Lastname,  '')
                      )) = @FullName", c))
                        {
                            cmdCheck.Parameters.AddWithValue("@EventID", eventId);
                            cmdCheck.Parameters.AddWithValue("@FullName", fullName);

                            int existing = Convert.ToInt32(cmdCheck.ExecuteScalar());
                            if (existing > 0)
                                continue; // skip duplicates
                        }

                        int walkInId;
                        using (SqlCommand cmdW = new SqlCommand(@"
                    INSERT INTO tblPlayerWalkIn (Firstname, Lastname)
                    VALUES (@F, @L);
                    SELECT SCOPE_IDENTITY();", c))
                        {
                            cmdW.Parameters.AddWithValue("@F", firstName);
                            cmdW.Parameters.AddWithValue("@L", lastName);
                            walkInId = Convert.ToInt32(cmdW.ExecuteScalar());
                        }

                        int order;
                        using (SqlCommand cmdOrder = new SqlCommand(@"
                    SELECT ISNULL(MAX(OrderNo), 0) + 1
                    FROM tblEventParticipant
                    WHERE EventID = @EventID", c))
                        {
                            cmdOrder.Parameters.AddWithValue("@EventID", eventId);
                            order = Convert.ToInt32(cmdOrder.ExecuteScalar());
                        }

                        using (SqlCommand cmdInsert = new SqlCommand(@"
                    INSERT INTO tblEventParticipant (EventID, WalkInID, OrderNo, StatusName)
                    VALUES (@EventID, @WalkInID, @OrderNo, 'Waiting')", c))
                        {
                            cmdInsert.Parameters.AddWithValue("@EventID", eventId);
                            cmdInsert.Parameters.AddWithValue("@WalkInID", walkInId);
                            cmdInsert.Parameters.AddWithValue("@OrderNo", order);
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                }

                txtMassPlayers.Text = "";
                RefreshWorkspace(eventId);
                ShowAlert("success", "Bulk Queue Complete", "Duplicate names were skipped.");
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Parse Error", ex.Message);
            }
        }

        protected void btnClearBoard_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    SqlCommand cmdDel = new SqlCommand("DELETE FROM tblMatch WHERE EventID=@EID", c);
                    cmdDel.Parameters.AddWithValue("@EID", ddlActiveEvents.SelectedValue); cmdDel.ExecuteNonQuery();
                    SqlCommand cmdUpd = new SqlCommand("UPDATE tblEvent SET Champion_ParticipantID=NULL, RunnerUp_ParticipantID=NULL WHERE EventID=@EID", c);
                    cmdUpd.Parameters.AddWithValue("@EID", ddlActiveEvents.SelectedValue); cmdUpd.ExecuteNonQuery();
                }
                RefreshWorkspace(int.Parse(ddlActiveEvents.SelectedValue));
                ShowAlert("success", "Board Cleared", "All matches have been wiped. Leaderboard reset.");
            }
            catch (Exception ex) { ShowAlert("error", "Deletion Error", ex.ToString()); }
        }

        protected void btnSaveWinners_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    SqlCommand cmd = new SqlCommand("UPDATE tblEvent SET Champion_ParticipantID=@C, RunnerUp_ParticipantID=@R WHERE EventID=@E", c);
                    cmd.Parameters.AddWithValue("@C", string.IsNullOrEmpty(ddlChampion.SelectedValue) ? (object)DBNull.Value : ddlChampion.SelectedValue);
                    cmd.Parameters.AddWithValue("@R", string.IsNullOrEmpty(ddlRunnerUp.SelectedValue) ? (object)DBNull.Value : ddlRunnerUp.SelectedValue);
                    cmd.Parameters.AddWithValue("@E", ddlActiveEvents.SelectedValue);
                    cmd.ExecuteNonQuery();
                }
                ScriptManager.RegisterStartupScript(this, GetType(), "c", "hideChampModal();", true);
                RefreshWorkspace(int.Parse(ddlActiveEvents.SelectedValue));
                ShowAlert("success", "Crowned!", "Tournament results manually published.");
            }
            catch (Exception ex) { ShowAlert("error", "Save Winners Error", ex.ToString()); }
        }

        protected void gvParticipants_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            try
            {
                int idx = Convert.ToInt32(e.CommandArgument);
                int pId = Convert.ToInt32(gvParticipants.DataKeys[idx].Values["EventParticipantID"]);

                object wObj = gvParticipants.DataKeys[idx].Values["WalkInID"];
                object uObj = gvParticipants.DataKeys[idx].Values["UserID"];
                string wId = (wObj == null || wObj == DBNull.Value) ? "" : wObj.ToString();
                string uId = (uObj == null || uObj == DBNull.Value) ? "" : uObj.ToString();

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    if (e.CommandName == "Pay")
                    {
                        SqlCommand cmdFee = new SqlCommand("SELECT RegistrationFee FROM tblEvent WHERE EventID=@EID", c);
                        cmdFee.Parameters.AddWithValue("@EID", ddlActiveEvents.SelectedValue);
                        decimal fee = (decimal)cmdFee.ExecuteScalar();
                        SqlCommand pay = new SqlCommand("INSERT INTO tblPayment (PaymentTypeName, WalkInID, UserID, Amount, PaymentDate) VALUES ('Queue', @W, @U, @A, GETDATE())", c);
                        pay.Parameters.AddWithValue("@W", string.IsNullOrEmpty(wId) ? (object)DBNull.Value : int.Parse(wId));
                        pay.Parameters.AddWithValue("@U", string.IsNullOrEmpty(uId) ? (object)DBNull.Value : int.Parse(uId));
                        pay.Parameters.AddWithValue("@A", fee); pay.ExecuteNonQuery();
                        SqlCommand cmdComplete = new SqlCommand("UPDATE tblEventParticipant SET StatusName='Completed' WHERE EventParticipantID=@PID", c);
                        cmdComplete.Parameters.AddWithValue("@PID", pId); cmdComplete.ExecuteNonQuery();
                    }
                    else if (e.CommandName == "Rent")
                    {
                        litRentPlayerName.Text = gvParticipants.Rows[idx].Cells[1].Text;
                        hfRentWalkInID.Value = wId; hfRentUserID.Value = uId;
                        LoadUserEquipmentHistory(c, hfRentWalkInID.Value, hfRentUserID.Value);
                        LoadEquipmentDropdown();
                        ScriptManager.RegisterStartupScript(this, GetType(), "openModal", "showEqModal();", true); return;
                    }
                    else if (e.CommandName == "RemovePlayer")
                    {
                        SqlCommand cmdRemove = new SqlCommand("UPDATE tblEventParticipant SET StatusName='Cancelled' WHERE EventParticipantID=@PID", c);
                        cmdRemove.Parameters.AddWithValue("@PID", pId); cmdRemove.ExecuteNonQuery();
                    }
                }
                RefreshWorkspace(int.Parse(ddlActiveEvents.SelectedValue));
            }
            catch (Exception ex) { ShowAlert("error", "Row Action Error", ex.ToString()); }
        }

        // ==========================================
        // EQUIPMENT RENTAL LOGIC & PAYMENT SYNC
        // ==========================================
        private void LoadUserEquipmentHistory(SqlConnection c, string wId, string uId)
        {
            try
            {
                bool hasW = !string.IsNullOrEmpty(wId);
                bool hasU = !string.IsNullOrEmpty(uId);

                if (!hasW && !hasU)
                {
                    rptActiveRentals.DataSource = null; rptActiveRentals.DataBind(); pnlActiveRentals.Visible = false;
                    rptConsumables.DataSource = null; rptConsumables.DataBind(); pnlConsumables.Visible = false;
                    return;
                }

                // 1. Load Active Rentals
                string sqlR = @"SELECT r.RentalID, ei.ItemID, m.EquipmentType, r.UnitPrice FROM tblRental r JOIN tblEquipmentItem ei ON r.ItemID = ei.ItemID JOIN tblEquipmentModel m ON ei.ModelID = m.ModelID WHERE r.ReturnedAt IS NULL AND ";
                sqlR += (hasW && hasU) ? "(r.WalkInID = @WID OR r.UserID = @UID)" : (hasW ? "r.WalkInID = @WID" : "r.UserID = @UID");

                SqlCommand cmdR = new SqlCommand(sqlR, c);
                if (hasW) cmdR.Parameters.AddWithValue("@WID", int.Parse(wId));
                if (hasU) cmdR.Parameters.AddWithValue("@UID", int.Parse(uId));

                DataTable dtR = new DataTable(); new SqlDataAdapter(cmdR).Fill(dtR);
                if (dtR.Rows.Count > 0) { rptActiveRentals.DataSource = dtR; rptActiveRentals.DataBind(); pnlActiveRentals.Visible = true; }
                else { rptActiveRentals.DataSource = null; rptActiveRentals.DataBind(); pnlActiveRentals.Visible = false; }

                // 2. Load Purchased Consumables
                string sqlC = @"SELECT c.Quantity, m.EquipmentType, (c.Quantity * c.UnitPrice) as Total FROM tblConsumable c JOIN tblEquipmentModel m ON c.ModelID = m.ModelID WHERE ";
                sqlC += (hasW && hasU) ? "(c.WalkInID = @WID OR c.UserID = @UID)" : (hasW ? "c.WalkInID = @WID" : "c.UserID = @UID");

                SqlCommand cmdC = new SqlCommand(sqlC, c);
                if (hasW) cmdC.Parameters.AddWithValue("@WID", int.Parse(wId));
                if (hasU) cmdC.Parameters.AddWithValue("@UID", int.Parse(uId));

                DataTable dtC = new DataTable(); new SqlDataAdapter(cmdC).Fill(dtC);
                if (dtC.Rows.Count > 0) { rptConsumables.DataSource = dtC; rptConsumables.DataBind(); pnlConsumables.Visible = true; }
                else { rptConsumables.DataSource = null; rptConsumables.DataBind(); pnlConsumables.Visible = false; }

            }
            catch (Exception ex) { ShowAlert("error", "Load History Error", ex.ToString()); }
        }

        protected void rptActiveRentals_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            try
            {
                if (e.CommandName == "ReturnItem")
                {
                    int rentalId = Convert.ToInt32(e.CommandArgument);
                    using (SqlConnection c = new SqlConnection(connString))
                    {
                        c.Open();

                        SqlCommand cmdInfo = new SqlCommand("SELECT UserID, WalkInID, UnitPrice FROM tblRental WHERE RentalID = @RID", c);
                        cmdInfo.Parameters.AddWithValue("@RID", rentalId);
                        int? uId = null; int? wId = null; decimal price = 0;
                        using (SqlDataReader dr = cmdInfo.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                uId = dr["UserID"] != DBNull.Value ? (int?)Convert.ToInt32(dr["UserID"]) : null;
                                wId = dr["WalkInID"] != DBNull.Value ? (int?)Convert.ToInt32(dr["WalkInID"]) : null;
                                price = Convert.ToDecimal(dr["UnitPrice"]);
                            }
                        }

                        SqlCommand cmdUpd = new SqlCommand("UPDATE tblRental SET ReturnedAt = GETDATE(), IsPaid = 1 WHERE RentalID = @RID", c);
                        cmdUpd.Parameters.AddWithValue("@RID", rentalId);
                        cmdUpd.ExecuteNonQuery();

                        SqlCommand cmdPay = new SqlCommand("INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, RentalID, Amount, PaymentDate) VALUES ('Rental', @U, @W, @RID, @A, GETDATE())", c);
                        cmdPay.Parameters.AddWithValue("@U", uId.HasValue ? (object)uId.Value : DBNull.Value);
                        cmdPay.Parameters.AddWithValue("@W", wId.HasValue ? (object)wId.Value : DBNull.Value);
                        cmdPay.Parameters.AddWithValue("@RID", rentalId);
                        cmdPay.Parameters.AddWithValue("@A", price);
                        cmdPay.ExecuteNonQuery();

                        LoadUserEquipmentHistory(c, hfRentWalkInID.Value, hfRentUserID.Value); LoadEquipmentDropdown();
                    }
                    ShowAlert("success", "Item Returned", "Equipment returned and payment officially logged.");
                }
            }
            catch (Exception ex) { ShowAlert("error", "Return Item Error", ex.ToString()); }
        }

        protected void btnSaveEquipment_Click(object sender, EventArgs e)
        {
            try
            {
                int mid = int.Parse(ddlEquipment.SelectedValue);
                int qty = 0;
                if (!int.TryParse(txtRentQty.Text, out qty) || qty <= 0) { ShowAlert("warning", "Invalid", "Enter valid quantity."); return; }

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    SqlCommand chk = new SqlCommand("SELECT ItemCategory, DefaultSellPrice FROM tblEquipmentModel WHERE ModelID=@M", c);
                    chk.Parameters.AddWithValue("@M", mid); SqlDataReader dr = chk.ExecuteReader(); dr.Read();
                    string cat = dr["ItemCategory"].ToString(); decimal price = dr["DefaultSellPrice"] != DBNull.Value ? (decimal)dr["DefaultSellPrice"] : 0; dr.Close();

                    int? wVal = string.IsNullOrEmpty(hfRentWalkInID.Value) ? (int?)null : int.Parse(hfRentWalkInID.Value);
                    int? uVal = string.IsNullOrEmpty(hfRentUserID.Value) ? (int?)null : int.Parse(hfRentUserID.Value);

                    if (cat == "Rental")
                    {
                        int rentedCount = 0;
                        for (int i = 0; i < qty; i++)
                        {
                            object itemObj = new SqlCommand($"SELECT TOP 1 ItemID FROM tblEquipmentItem WHERE ModelID={mid} AND IsDeleted=0 AND ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL)", c).ExecuteScalar();
                            if (itemObj != null)
                            {
                                SqlCommand rentCmd = new SqlCommand("INSERT INTO tblRental (WalkInID, UserID, ItemID, RentalDate) VALUES (@W, @U, @I, GETDATE())", c);
                                rentCmd.Parameters.AddWithValue("@W", wVal.HasValue ? (object)wVal.Value : DBNull.Value);
                                rentCmd.Parameters.AddWithValue("@U", uVal.HasValue ? (object)uVal.Value : DBNull.Value);
                                rentCmd.Parameters.AddWithValue("@I", Convert.ToInt32(itemObj));
                                rentCmd.ExecuteNonQuery(); rentedCount++;
                            }
                            else { ShowAlert("error", "Inventory Missing", "There are no physical Items remaining in the database for this model. Please add Item IDs via Inventory first."); return; }
                        }
                        LoadUserEquipmentHistory(c, hfRentWalkInID.Value, hfRentUserID.Value); LoadEquipmentDropdown();
                        ShowAlert("success", "Success", "Rentals issued to account.");
                    }
                    else
                    {
                        int currentStock = Convert.ToInt32(new SqlCommand($"SELECT ISNULL(ConsumableQty, 0) FROM tblEquipmentModel WHERE ModelID={mid}", c).ExecuteScalar());
                        if (currentStock < qty)
                        {
                            ShowAlert("error", "Not Enough Stock", $"You are trying to sell {qty}, but there are only {currentStock} left in stock."); return;
                        }
                        new SqlCommand($"UPDATE tblEquipmentModel SET ConsumableQty = ConsumableQty - {qty} WHERE ModelID={mid}", c).ExecuteNonQuery();

                        SqlCommand conCmd = new SqlCommand("INSERT INTO tblConsumable (WalkInID, UserID, ModelID, Quantity, UnitPrice, IsPaid) VALUES (@W, @U, @M, @Q, @P, 1); SELECT SCOPE_IDENTITY();", c);
                        conCmd.Parameters.AddWithValue("@W", wVal.HasValue ? (object)wVal.Value : DBNull.Value);
                        conCmd.Parameters.AddWithValue("@U", uVal.HasValue ? (object)uVal.Value : DBNull.Value);
                        conCmd.Parameters.AddWithValue("@M", mid); conCmd.Parameters.AddWithValue("@Q", qty); conCmd.Parameters.AddWithValue("@P", price);
                        int conId = Convert.ToInt32(conCmd.ExecuteScalar());

                        SqlCommand cmdPay = new SqlCommand("INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ConsumableID, Amount, PaymentDate) VALUES ('Consumable', @U, @W, @CID, @A, GETDATE())", c);
                        cmdPay.Parameters.AddWithValue("@U", uVal.HasValue ? (object)uVal.Value : DBNull.Value);
                        cmdPay.Parameters.AddWithValue("@W", wVal.HasValue ? (object)wVal.Value : DBNull.Value);
                        cmdPay.Parameters.AddWithValue("@CID", conId);
                        cmdPay.Parameters.AddWithValue("@A", price * qty);
                        cmdPay.ExecuteNonQuery();

                        LoadUserEquipmentHistory(c, hfRentWalkInID.Value, hfRentUserID.Value); LoadEquipmentDropdown();
                        ShowAlert("success", "Success", "Items paid and charged to account.");
                    }
                }
            }
            catch (Exception ex) { ShowAlert("error", "Transaction Error", ex.ToString()); }
        }

        // ==========================================
        // MANUAL MATCH & INITIAL DRAFT GENERATOR
        // ==========================================
        protected void btnSaveManualMatch_Click(object sender, EventArgs e)
        {
            try
            {
                int eid = int.Parse(ddlActiveEvents.SelectedValue); string matchType = ddlManualType.SelectedValue;
                string t1p1 = ddlT1P1.SelectedValue; string t1p2 = matchType == "Doubles" ? ddlT1P2.SelectedValue : "";
                string t2p1 = ddlT2P1.SelectedValue; string t2p2 = matchType == "Doubles" ? ddlT2P2.SelectedValue : "";

                if (string.IsNullOrEmpty(t1p1)) { ShowAlert("error", "Validation Error", "Team 1 must have at least one player."); return; }

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    int order = Convert.ToInt32(new SqlCommand($"SELECT ISNULL(MAX(MatchOrder),0)+1 FROM tblMatch WHERE EventID={eid}", c).ExecuteScalar());
                    SqlCommand cmd = new SqlCommand("INSERT INTO tblMatch (EventID, CourtID, MatchType, BracketPhase, Player1_ParticipantID, Player2_ParticipantID, Player3_ParticipantID, Player4_ParticipantID, MatchStatus, MatchOrder) VALUES (@E, @C, @MT, @BP, @P1, @P2, @P3, @P4, 'Scheduled', @O)", c);
                    cmd.Parameters.AddWithValue("@E", eid); cmd.Parameters.AddWithValue("@C", ddlManualCourt.SelectedValue); cmd.Parameters.AddWithValue("@MT", matchType); cmd.Parameters.AddWithValue("@BP", ddlManualPhase.SelectedValue); cmd.Parameters.AddWithValue("@O", order);
                    cmd.Parameters.AddWithValue("@P1", t1p1); cmd.Parameters.AddWithValue("@P2", string.IsNullOrEmpty(t2p1) ? (object)DBNull.Value : t2p1); cmd.Parameters.AddWithValue("@P3", string.IsNullOrEmpty(t1p2) ? (object)DBNull.Value : t1p2); cmd.Parameters.AddWithValue("@P4", string.IsNullOrEmpty(t2p2) ? (object)DBNull.Value : t2p2);
                    cmd.ExecuteNonQuery();
                }
                ScriptManager.RegisterStartupScript(this, GetType(), "closeM", "hideMatchModal();", true);
                RefreshWorkspace(eid);
            }
            catch (Exception ex) { ShowAlert("error", "Manual Queue Error", ex.ToString()); }
        }

        protected void btnExecuteDraft_Click(object sender, EventArgs e)
        {
            try
            {
                int eid = int.Parse(ddlActiveEvents.SelectedValue);
                string format = ddlDraftFormat.SelectedValue;
                string phase = "Casual";

                using (SqlConnection c = new SqlConnection(connString))
                {
                    c.Open();
                    string sql = $@"SELECT ep.EventParticipantID FROM tblEventParticipant ep WHERE ep.EventID={eid} AND ep.StatusName='Completed' AND ep.EventParticipantID NOT IN (SELECT Player1_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player1_ParticipantID IS NOT NULL AND EventID={eid} UNION SELECT Player2_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player2_ParticipantID IS NOT NULL AND EventID={eid} UNION SELECT Player3_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player3_ParticipantID IS NOT NULL AND EventID={eid} UNION SELECT Player4_ParticipantID FROM tblMatch WHERE MatchStatus IN ('Scheduled', 'Pending') AND Player4_ParticipantID IS NOT NULL AND EventID={eid})";
                    List<int> ids = new List<int>(); using (SqlDataReader dr = new SqlCommand(sql, c).ExecuteReader()) while (dr.Read()) ids.Add(dr.GetInt32(0));

                    DataTable courts = new DataTable(); new SqlDataAdapter($"SELECT CourtID FROM tblEventCourtPool WHERE EventID={eid}", c).Fill(courts);
                    if (courts.Rows.Count == 0 || ids.Count < 2) { ShowAlert("warning", "Draft Failed", "Not enough available players or courts to run a draft right now."); ScriptManager.RegisterStartupScript(this, GetType(), "cl", "hideDraftModal();", true); return; }

                    int order = Convert.ToInt32(new SqlCommand($"SELECT ISNULL(MAX(MatchOrder),0)+1 FROM tblMatch WHERE EventID={eid}", c).ExecuteScalar());
                    int step = format == "Doubles" ? 4 : 2;

                    for (int i = 0; i < ids.Count; i += step)
                    {
                        if (i + 1 >= ids.Count) break;
                        int cid = Convert.ToInt32(courts.Rows[(order - 1) % courts.Rows.Count]["CourtID"]);

                        string p1 = ids[i].ToString();
                        string p2 = format == "Doubles" && i + 2 < ids.Count ? ids[i + 2].ToString() : (format == "Singles" ? ids[i + 1].ToString() : "NULL");
                        string p3 = format == "Doubles" ? ids[i + 1].ToString() : "NULL";
                        string p4 = format == "Doubles" && i + 3 < ids.Count ? ids[i + 3].ToString() : "NULL";

                        new SqlCommand($"INSERT INTO tblMatch (EventID, CourtID, MatchType, BracketPhase, Player1_ParticipantID, Player2_ParticipantID, Player3_ParticipantID, Player4_ParticipantID, MatchStatus, MatchOrder) VALUES ({eid}, {cid}, '{format}', '{phase}', {p1}, {p2}, {p3}, {p4}, 'Scheduled', {order})", c).ExecuteNonQuery();
                        order++;
                    }
                }
                ScriptManager.RegisterStartupScript(this, GetType(), "cl", "hideDraftModal();", true);
                RefreshWorkspace(eid); ShowAlert("success", "Draft Complete", $"Generated {format} brackets for the active players.");
            }
            catch (Exception ex) { ShowAlert("error", "Draft Generation Error", ex.ToString()); }
        }

        private void ShowAlert(string i, string t, string tx) { ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"showSweetAlert('{i}','{t}','{tx.Replace("'", "").Replace("\r", "").Replace("\n", " ")}');", true); }
    }
}