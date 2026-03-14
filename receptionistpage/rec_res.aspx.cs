using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.receptionistpage
{
    public partial class rec_res : System.Web.UI.Page
    {
        private string CS = ConfigurationManager.ConnectionStrings["soapergandahannali"]?.ConnectionString
                            ?? ConfigurationManager.ConnectionStrings["dbsmashitFINAL"]?.ConnectionString;

        private const decimal COURT_PRICE_PER_HOUR = 330.00m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                txtNewResDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                LoadCourtsAndUsers();
                PopulateTimeDropdown();
                LoadEquipmentModels();
                RefreshAllData();
            }
        }

        private void PopulateTimeDropdown()
        {
            ddlNewStartTime.Items.Clear();
            ddlEditStartTime.Items.Clear();

            for (int h = 8; h <= 21; h++) // Generate 8:00 AM to 9:30 PM
            {
                DateTime t1 = new DateTime(2000, 1, 1, h, 0, 0);
                ListItem item1 = new ListItem(t1.ToString("hh:mm tt"), t1.ToString("HH:mm"));
                ddlNewStartTime.Items.Add(item1);
                ddlEditStartTime.Items.Add(item1);

                DateTime t2 = new DateTime(2000, 1, 1, h, 30, 0);
                ListItem item2 = new ListItem(t2.ToString("hh:mm tt"), t2.ToString("HH:mm"));
                ddlNewStartTime.Items.Add(item2);
                ddlEditStartTime.Items.Add(item2);
            }
        }

        private void LoadEquipmentModels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    // Check available stock based on item type
                    string q = @"
                        SELECT 
                            m.ModelID, 
                            CONCAT(m.EquipmentType, ' (', m.ItemCategory, ') - P', 
                                   CAST(COALESCE(m.DefaultRentalPrice, m.DefaultSellPrice, 0) AS DECIMAL(10,2)),
                                   ' [',
                                   CASE 
                                        WHEN m.ItemCategory = 'Rental' THEN 
                                            (SELECT COUNT(*) FROM tblEquipmentItem ei WHERE ei.ModelID = m.ModelID AND ei.IsDeleted = 0 AND ei.ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL))
                                        WHEN m.ItemCategory = 'Consumable' THEN ISNULL(m.ConsumableQty, 0)
                                        ELSE 0 
                                   END,
                                   ' Available]'
                            ) as DisplayText 
                        FROM tblEquipmentModel m
                        WHERE m.IsDeleted = 0 AND m.IsArchived = 0";

                    using (SqlCommand cmd = new SqlCommand(q, con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        ddlEquipmentModel.DataSource = dt;
                        ddlEquipmentModel.DataTextField = "DisplayText";
                        ddlEquipmentModel.DataValueField = "ModelID";
                        ddlEquipmentModel.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Error Loading Items", ex.Message);
            }
        }

        protected void chkHideCancelled_CheckedChanged(object sender, EventArgs e)
        {
            RefreshAllData();
        }

        protected void ddlFilterStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshAllData();
        }

        protected void btnToday_Click(object sender, EventArgs e)
        {
            txtDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
            txtSearch.Text = "";
            ddlFilterStatus.SelectedValue = "All";
            RefreshAllData();
        }

        protected void txtDate_TextChanged(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            RefreshAllData();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            RefreshAllData();
        }

        private void RefreshAllData()
        {
            if (DateTime.TryParse(txtDate.Text, out DateTime targetDate))
            {
                lblDgvDate.Text = targetDate.ToString("MMM dd, yyyy");
                LoadScheduleGrid(targetDate);
                LoadGrid(targetDate, txtSearch.Text.Trim());
                ResetSidebar();
            }
        }

        private void LoadCourtsAndUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    con.Open();
                    using (SqlCommand cmdC = new SqlCommand("SELECT CourtID, CONCAT('Court ', CourtNumber, ' (', SportName, ')') as CourtDesc FROM tblCourt WHERE IsActive = 1", con))
                    {
                        SqlDataAdapter daC = new SqlDataAdapter(cmdC);
                        DataTable dtC = new DataTable();
                        daC.Fill(dtC);

                        ddlCourt.DataSource = dtC;
                        ddlCourt.DataTextField = "CourtDesc";
                        ddlCourt.DataValueField = "CourtID";
                        ddlCourt.DataBind();

                        ddlEditCourt.DataSource = dtC;
                        ddlEditCourt.DataTextField = "CourtDesc";
                        ddlEditCourt.DataValueField = "CourtID";
                        ddlEditCourt.DataBind();
                    }

                    using (SqlCommand cmdU = new SqlCommand("SELECT UserID, CONCAT(Firstname, ' ', Lastname, ' (', Email, ')') as FullName FROM tblPlayerAccount", con))
                    {
                        SqlDataAdapter daU = new SqlDataAdapter(cmdU);
                        DataTable dtU = new DataTable();
                        daU.Fill(dtU);

                        ddlUsers.DataSource = dtU;
                        ddlUsers.DataTextField = "FullName";
                        ddlUsers.DataValueField = "UserID";
                        ddlUsers.DataBind();

                        ddlEditUsers.DataSource = dtU;
                        ddlEditUsers.DataTextField = "FullName";
                        ddlEditUsers.DataValueField = "UserID";
                        ddlEditUsers.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Error Loading Setup", ex.Message);
            }
        }

        private void LoadScheduleGrid(DateTime targetDate)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Court");

            List<TimeSpan> slots = new List<TimeSpan>();
            for (int h = 8; h <= 22; h++) // 8am to 10pm
            {
                slots.Add(new TimeSpan(h, 0, 0));
                if (h < 22) slots.Add(new TimeSpan(h, 30, 0));
            }

            foreach (TimeSpan st in slots)
            {
                DateTime t1 = new DateTime(2000, 1, 1).Add(st);
                dt.Columns.Add($"{t1:h:mm tt} - {t1.AddMinutes(60):h:mm tt}");
            }

            DataTable activeResAndEvents = new DataTable();
            DataTable courtBlocks = new DataTable();

            using (SqlConnection c = new SqlConnection(CS))
            {
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
                new SqlDataAdapter(cmd1).Fill(activeResAndEvents);

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
                    TimeSpan slotEnd = slotStart.Add(new TimeSpan(1, 0, 0)); // 1 HOUR LONG visual block

                    var block = courtBlocks.AsEnumerable().FirstOrDefault(b =>
                        b.Field<int>("CourtID") == cNum &&
                        b.Field<TimeSpan>("StartTime") < slotEnd &&
                        b.Field<TimeSpan>("EndTime") > slotStart);

                    if (block != null)
                    {
                        dr[i + 1] = "BLOCKED|CLOSED";
                        continue;
                    }

                    var overlappingRecords = activeResAndEvents.AsEnumerable().Where(r =>
                        r.Field<int>("CourtID") == cNum &&
                        r.Field<TimeSpan>("StartTime") < slotEnd &&
                        r.Field<TimeSpan>("EndTime") > slotStart).ToList();

                    if (chkHideCancelled.Checked)
                    {
                        overlappingRecords = overlappingRecords.Where(r => r.Field<string>("Status") != "Cancelled").ToList();
                    }

                    if (overlappingRecords.Count > 0)
                    {
                        var activeRecords = overlappingRecords.Where(r => r.Field<string>("Status") != "Cancelled").ToList();

                        if (activeRecords.Count > 1)
                        {
                            dr[i + 1] = "BLOCKED|OVERLAP";
                        }
                        else if (activeRecords.Count == 1)
                        {
                            var res = activeRecords[0];
                            TimeSpan rStart = res.Field<TimeSpan>("StartTime");
                            TimeSpan rEnd = res.Field<TimeSpan>("EndTime");

                            if (rStart <= slotStart && rEnd >= slotEnd)
                            {
                                dr[i + 1] = $"{res.Field<string>("Type")}|{res.Field<int>("ID")}|{res.Field<string>("Status")}|{res.Field<string>("SportName") ?? "Mixed"}|{res.Field<string>("Name")}|{res.Field<int>("IsWalkIn")}";
                            }
                            else
                            {
                                dr[i + 1] = "BLOCKED|OVERLAP";
                            }
                        }
                        else
                        {
                            var cancelledRes = overlappingRecords.First();
                            TimeSpan rStart = cancelledRes.Field<TimeSpan>("StartTime");
                            TimeSpan rEnd = cancelledRes.Field<TimeSpan>("EndTime");

                            if (rStart <= slotStart && rEnd >= slotEnd)
                            {
                                dr[i + 1] = $"{cancelledRes.Field<string>("Type")}|{cancelledRes.Field<int>("ID")}|{cancelledRes.Field<string>("Status")}|{cancelledRes.Field<string>("SportName") ?? "Mixed"}|{cancelledRes.Field<string>("Name")}|{cancelledRes.Field<int>("IsWalkIn")}";
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
                int courtID = e.Row.RowIndex + 1; // Row 0 is Court 1

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
                        string title = p[4];
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell bg-blue event-cell-info'><span class='event-cell-title'>{title}</span><span class='event-cell-sub'>Event</span></div>";
                    }
                    else if (raw.StartsWith("OPEN|"))
                    {
                        string timeVal = raw.Split('|')[1];
                        string jsCall = $"triggerSidebarOpen({courtID}, '{timeVal}')";
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell bg-white event-cell-info' onclick=\"{jsCall}\"><span class='event-cell-title'>OPEN</span></div>";
                    }
                    else if (raw.StartsWith("RES|"))
                    {
                        string[] p = raw.Split('|');
                        string rId = p[1];
                        string stat = p[2];
                        string sport = p[3].ToUpper();
                        string name = p[4];
                        bool isWalkIn = p[5] == "1";

                        string css = "bg-green";
                        if (stat == "Cancelled") css = "bg-red";
                        else if (stat == "Pending" && !isWalkIn) css = "bg-purple";
                        else css = sport.Contains("PICKLE") ? "bg-orange" : "bg-green";

                        string jsCall = $"triggerSidebarRes({rId})";
                        e.Row.Cells[i].Text = $"<div class='event-slot-cell {css} event-cell-info' onclick=\"{jsCall}\"><span class='event-cell-title'>{name}</span><span class='event-cell-sub'>{stat}</span></div>";
                    }
                }
            }
        }

        private void LoadGrid(DateTime targetDate, string searchTerm = "")
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    string filterStatus = ddlFilterStatus.SelectedValue;

                    string query = @"
                        SELECT 
                            'RES' AS RecordType,
                            r.ReservationID AS RefID, 
                            CASE 
                                WHEN r.UserID IS NOT NULL THEN ISNULL(u.Firstname, '') + ' ' + ISNULL(u.Lastname, '')
                                ELSE ISNULL(w.Firstname, '') + ' ' + ISNULL(w.Lastname, '') 
                            END AS MainTitle,
                            CASE WHEN r.UserID IS NOT NULL THEN 'Registered User' ELSE 'Walk-In Customer' END AS SubTitle,
                            ISNULL(c.CourtNumber, 0) AS CourtNumber, 
                            ISNULL(r.SportName, c.SportName) AS SportName,
                            r.StartTime, 
                            r.EndTime, 
                            ISNULL(r.ReservationStatusName, 'Pending') AS StatusName, 
                            ISNULL(r.PaymentStatus, 'Unpaid') AS PaymentStatus, 
                            ISNULL(r.RequiredAmount, 0) AS RequiredAmount
                        FROM tblReservation r
                        LEFT JOIN tblPlayerAccount u ON r.UserID = u.UserID
                        LEFT JOIN tblPlayerWalkIn w ON r.WalkInID = w.WalkInID
                        LEFT JOIN tblCourt c ON r.CourtID = c.CourtID
                        WHERE CAST(r.ResDate AS DATE) = CAST(@Date AS DATE)
                          AND (@Status = 'All' OR r.ReservationStatusName = @Status)
                          AND (@Search = '' OR 
                               ISNULL(u.Firstname, '') LIKE '%' + @Search + '%' OR 
                               ISNULL(w.Firstname, '') LIKE '%' + @Search + '%' OR 
                               CAST(r.ReservationID AS VARCHAR) LIKE '%' + @Search + '%')
                        ORDER BY r.StartTime ASC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Date", targetDate.Date);
                        cmd.Parameters.AddWithValue("@Search", searchTerm);
                        cmd.Parameters.AddWithValue("@Status", filterStatus);

                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        gvReservations.DataSource = dt;
                        gvReservations.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Grid Error", "Could not load the Data Grid: " + ex.Message);
            }
        }

        protected void btnHiddenTrigger_Click(object sender, EventArgs e)
        {
            string action = hfActionType.Value;

            if (action == "OPEN")
            {
                btnShowAddForm_Click(null, null);

                if (int.TryParse(hfActionCourt.Value, out int cId))
                {
                    if (ddlCourt.Items.FindByValue(cId.ToString()) != null)
                        ddlCourt.SelectedValue = cId.ToString();
                }

                if (ddlNewStartTime.Items.FindByValue(hfActionTime.Value) != null)
                {
                    ddlNewStartTime.SelectedValue = hfActionTime.Value;
                }
            }
            else if (action == "RES")
            {
                int.TryParse(hfActionResID.Value, out int resId);
                if (resId > 0) LoadReservationDetails(resId);
            }
        }

        protected void gvReservations_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ReviewRes")
            {
                int refId = int.Parse(e.CommandArgument.ToString());
                LoadReservationDetails(refId);
                ScriptManager.RegisterStartupScript(this, GetType(), "ScrollUp", "window.scrollTo(0, 0);", true);
            }
        }

        private void LoadReservationDetails(int resId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    string query = @"
                        SELECT 
                            r.ReservationID, 
                            r.UserID,
                            u.Firstname as UFirst, u.Lastname as ULast,
                            r.WalkInID,
                            w.Firstname as WFirst, w.Lastname as WLast,
                            c.CourtID, 
                            r.SportName,
                            r.ResDate,
                            r.StartTime, 
                            r.EndTime, 
                            ISNULL(r.ReservationStatusName, 'Pending') AS ReservationStatusName, 
                            ISNULL(r.PaymentStatus, 'Unpaid') AS PaymentStatus, 
                            ISNULL(r.RequiredAmount, 0) AS RequiredAmount,
                            ISNULL(r.PaymongoCheckoutSessionID, 'N/A (Cash/Over the counter)') AS RefID,
                            ISNULL((SELECT SUM(UnitPrice) FROM tblRental WHERE ReservationID = r.ReservationID), 0) as RentalAmt,
                            ISNULL((SELECT SUM(UnitPrice * Quantity) FROM tblConsumable WHERE ReservationID = r.ReservationID), 0) as ConsumableAmt
                        FROM tblReservation r
                        LEFT JOIN tblPlayerAccount u ON r.UserID = u.UserID
                        LEFT JOIN tblPlayerWalkIn w ON r.WalkInID = w.WalkInID
                        JOIN tblCourt c ON r.CourtID = c.CourtID
                        WHERE r.ReservationID = @ID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ID", resId);
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                hfSelectedResID.Value = dr["ReservationID"].ToString();
                                lblSideResID.Text = dr["ReservationID"].ToString();
                                lblSideRef.Text = dr["RefID"].ToString();

                                // GRAND TOTAL CALCULATION
                                decimal baseAmt = Convert.ToDecimal(dr["RequiredAmount"]);
                                decimal rentAmt = Convert.ToDecimal(dr["RentalAmt"]);
                                decimal consAmt = Convert.ToDecimal(dr["ConsumableAmt"]);
                                lblSideAmount.Text = (baseAmt + rentAmt + consAmt).ToString("0.00");

                                // POPULATE EDITABLE CUSTOMER FIELDS
                                bool isWalkIn = dr["UserID"] == DBNull.Value;
                                ddlEditCustomerType.SelectedValue = isWalkIn ? "WalkIn" : "User";
                                phEditWalkIn.Visible = isWalkIn;
                                phEditRegisteredUser.Visible = !isWalkIn;

                                if (isWalkIn)
                                {
                                    txtEditFirstName.Text = dr["WFirst"].ToString();
                                    txtEditLastName.Text = dr["WLast"].ToString();
                                }
                                else
                                {
                                    string uId = dr["UserID"].ToString();
                                    if (ddlEditUsers.Items.FindByValue(uId) != null) ddlEditUsers.SelectedValue = uId;
                                }

                                // EDITABLE COURT AND DATE INFO
                                string courtId = dr["CourtID"].ToString();
                                if (ddlEditCourt.Items.FindByValue(courtId) != null) ddlEditCourt.SelectedValue = courtId;

                                string dbSport = dr["SportName"].ToString().ToLower();
                                if (ddlEditSport.Items.FindByValue(dbSport) != null) ddlEditSport.SelectedValue = dbSport;

                                txtEditDate.Text = Convert.ToDateTime(dr["ResDate"]).ToString("yyyy-MM-dd");

                                TimeSpan sTime = (TimeSpan)dr["StartTime"];
                                TimeSpan eTime = (TimeSpan)dr["EndTime"];

                                string timeStr = sTime.ToString(@"hh\:mm");
                                if (ddlEditStartTime.Items.FindByValue(timeStr) != null) ddlEditStartTime.SelectedValue = timeStr;

                                int durationHours = (int)(eTime - sTime).TotalHours;
                                if (ddlEditDuration.Items.FindByValue(durationHours.ToString()) != null) ddlEditDuration.SelectedValue = durationHours.ToString();

                                string dbStatus = dr["ReservationStatusName"].ToString();
                                if (ddlStatus.Items.FindByValue(dbStatus) != null) ddlStatus.SelectedValue = dbStatus;

                                string dbPay = dr["PaymentStatus"].ToString();
                                if (ddlPayment.Items.FindByValue(dbPay) != null) ddlPayment.SelectedValue = dbPay;
                                else ddlPayment.SelectedValue = "Unpaid";

                                if (dbStatus == "Pending")
                                {
                                    btnApproveRes.Visible = true;
                                    btnSaveUpdate.Text = "Save Details";
                                }
                                else
                                {
                                    btnApproveRes.Visible = false;
                                    btnSaveUpdate.Text = "Apply Updates";
                                }

                                pnlEmptyState.Visible = false;
                                pnlInputForm.Visible = false;
                                pnlExtrasForm.Visible = false;
                                pnlManageForm.Visible = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Error loading details", ex.Message);
            }
        }

        protected void ddlEditCustomerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isWalkIn = ddlEditCustomerType.SelectedValue == "WalkIn";
            phEditWalkIn.Visible = isWalkIn;
            phEditRegisteredUser.Visible = !isWalkIn;
        }

        protected void btnApproveRes_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfSelectedResID.Value)) return;
            int resId = Convert.ToInt32(hfSelectedResID.Value);
            int staffId = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    string updateQ = "UPDATE tblReservation SET ReservationStatusName = 'Approved', ApprovedByStaffID = @StaffID WHERE ReservationID = @ID";
                    using (SqlCommand cmd = new SqlCommand(updateQ, con))
                    {
                        cmd.Parameters.AddWithValue("@StaffID", staffId);
                        cmd.Parameters.AddWithValue("@ID", resId);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowAlert("success", "Approved!", $"Reservation #{resId} is now Approved.");
                RefreshAllData();
                LoadReservationDetails(resId);
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Approval Failed", ex.Message);
            }
        }

        protected void btnSaveUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfSelectedResID.Value)) return;
            int resId = Convert.ToInt32(hfSelectedResID.Value);

            try
            {
                DateTime newDate = DateTime.Parse(txtEditDate.Text);
                TimeSpan newStart = TimeSpan.Parse(ddlEditStartTime.SelectedValue);
                int duration = int.Parse(ddlEditDuration.SelectedValue);
                TimeSpan newEnd = newStart.Add(TimeSpan.FromHours(duration));
                int courtId = int.Parse(ddlEditCourt.SelectedValue);
                string sport = ddlEditSport.SelectedValue;

                string newStatus = ddlStatus.SelectedValue;
                string newPayment = ddlPayment.SelectedValue;
                int isPaid = newPayment == "FullyPaid" ? 1 : 0;
                int staffId = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

                decimal updatedAmount = duration * COURT_PRICE_PER_HOUR;

                using (SqlConnection con = new SqlConnection(CS))
                {
                    con.Open();

                    // --- CHECK UNRETURNED RENTALS IF COMPLETED ---
                    if (newStatus == "Completed")
                    {
                        using (SqlCommand cmdCheckRentals = new SqlCommand("SELECT COUNT(*) FROM tblRental WHERE ReservationID = @ResID AND ReturnedAt IS NULL", con))
                        {
                            cmdCheckRentals.Parameters.AddWithValue("@ResID", resId);
                            int unreturned = Convert.ToInt32(cmdCheckRentals.ExecuteScalar());
                            if (unreturned > 0)
                            {
                                ShowAlert("error", "Cannot Complete", $"There are {unreturned} unreturned rental items. Please manage extras and return them first.");
                                return;
                            }
                        }
                    }

                    // --- VALIDATE COURT RESTRICTIONS ---
                    int courtNum = 0;
                    using (SqlCommand cmdC = new SqlCommand("SELECT CourtNumber FROM tblCourt WHERE CourtID = @ID", con))
                    {
                        cmdC.Parameters.AddWithValue("@ID", courtId);
                        courtNum = Convert.ToInt32(cmdC.ExecuteScalar());
                    }
                    if ((courtNum >= 1 && courtNum <= 4) && sport.ToLower() == "pickleball")
                    {
                        ShowAlert("warning", "Court Restriction", "Courts 1 to 4 are restricted strictly for Badminton use.");
                        return;
                    }

                    // --- OVERLAP VALIDATION (Ignoring current res ID) ---
                    using (SqlCommand cmdOverlap = new SqlCommand(@"
                        SELECT COUNT(*) FROM tblReservation
                        WHERE CourtID = @CourtID AND ResDate = @ResDate AND ReservationID != @ResID
                          AND ReservationStatusName IN ('Pending','Approved')
                          AND (@StartTime < EndTime AND @EndTime > StartTime)", con))
                    {
                        cmdOverlap.Parameters.AddWithValue("@CourtID", courtId);
                        cmdOverlap.Parameters.AddWithValue("@ResDate", newDate.Date);
                        cmdOverlap.Parameters.AddWithValue("@StartTime", newStart);
                        cmdOverlap.Parameters.AddWithValue("@EndTime", newEnd);
                        cmdOverlap.Parameters.AddWithValue("@ResID", resId);

                        if (Convert.ToInt32(cmdOverlap.ExecuteScalar()) > 0 && newStatus != "Cancelled")
                        {
                            ShowAlert("warning", "Time Overlap", "These new times overlap with an existing active reservation on this court.");
                            return;
                        }
                    }

                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        try
                        {
                            // Resolve Editable IDs
                            int? finalUserId = null;
                            int? finalWalkInId = null;

                            if (ddlEditCustomerType.SelectedValue == "User")
                            {
                                finalUserId = int.Parse(ddlEditUsers.SelectedValue);
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(txtEditFirstName.Text) || string.IsNullOrWhiteSpace(txtEditLastName.Text))
                                {
                                    throw new Exception("First Name and Last Name are required for Walk-In customers.");
                                }

                                // Create a new Walk-in profile for the edit to avoid corrupting previous data history
                                using (SqlCommand cmdWalkIn = new SqlCommand(@"
                                    INSERT INTO tblPlayerWalkIn (Firstname, Lastname, BasePaid, QueuePaid) 
                                    VALUES (@FName, @LName, 1, 0); SELECT SCOPE_IDENTITY();", con, tx))
                                {
                                    cmdWalkIn.Parameters.AddWithValue("@FName", txtEditFirstName.Text.Trim());
                                    cmdWalkIn.Parameters.AddWithValue("@LName", txtEditLastName.Text.Trim());
                                    finalWalkInId = Convert.ToInt32(cmdWalkIn.ExecuteScalar());
                                }
                            }

                            // UPDATE 
                            string updateQ = @"
                                UPDATE tblReservation 
                                SET UserID = @UID,
                                    WalkInID = @WID,
                                    CourtID = @Court,
                                    SportName = @Sport,
                                    ResDate = @Date,
                                    StartTime = @Start,
                                    EndTime = @End,
                                    RequiredAmount = @Amt,
                                    ReservationStatusName = @Status, 
                                    PaymentStatus = @Pay, 
                                    IsPaid = @IsPaid,
                                    ApprovedByStaffID = CASE WHEN @Status = 'Approved' THEN @StaffID ELSE ApprovedByStaffID END
                                WHERE ReservationID = @ID";

                            using (SqlCommand cmd = new SqlCommand(updateQ, con, tx))
                            {
                                cmd.Parameters.AddWithValue("@UID", (object)finalUserId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@WID", (object)finalWalkInId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Court", courtId);
                                cmd.Parameters.AddWithValue("@Sport", sport);
                                cmd.Parameters.AddWithValue("@Date", newDate);
                                cmd.Parameters.AddWithValue("@Start", newStart);
                                cmd.Parameters.AddWithValue("@End", newEnd);
                                cmd.Parameters.AddWithValue("@Amt", updatedAmount);
                                cmd.Parameters.AddWithValue("@Status", newStatus);
                                cmd.Parameters.AddWithValue("@Pay", newPayment);
                                cmd.Parameters.AddWithValue("@IsPaid", isPaid);
                                cmd.Parameters.AddWithValue("@StaffID", staffId);
                                cmd.Parameters.AddWithValue("@ID", resId);

                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            ShowAlert("success", "Updated", $"Reservation #{resId} updated successfully.");
                            RefreshAllData();
                            LoadReservationDetails(resId);
                        }
                        catch (Exception innerEx)
                        {
                            tx.Rollback();
                            throw innerEx; // Pass out
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Save Failed", ex.Message);
            }
        }

        // ========================================================
        // EXTRAS (RENTALS & CONSUMABLES) LOGIC
        // ========================================================
        protected void btnManageExtras_Click(object sender, EventArgs e)
        {
            pnlManageForm.Visible = false;
            pnlExtrasForm.Visible = true;
            lblExtrasResID.Text = hfSelectedResID.Value;
            LoadEquipmentModels(); // Refresh available stock counts before showing
            LoadExtrasGrid(Convert.ToInt32(hfSelectedResID.Value));
        }

        protected void btnBackToRes_Click(object sender, EventArgs e)
        {
            pnlExtrasForm.Visible = false;
            pnlManageForm.Visible = true;
            LoadReservationDetails(Convert.ToInt32(hfSelectedResID.Value)); // Recalculate grand total
        }

        private void LoadExtrasGrid(int resId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    // 1. RENTALS QUERY
                    string qRentals = @"
                        SELECT r.RentalID as ExtraID, em.EquipmentType as ItemName, r.UnitPrice as Price, r.ReturnedAt
                        FROM tblRental r
                        JOIN tblEquipmentItem ei ON r.ItemID = ei.ItemID
                        JOIN tblEquipmentModel em ON ei.ModelID = em.ModelID
                        WHERE r.ReservationID = @ResID";

                    using (SqlCommand cmd = new SqlCommand(qRentals, con))
                    {
                        cmd.Parameters.AddWithValue("@ResID", resId);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dtRentals = new DataTable();
                        da.Fill(dtRentals);
                        gvRentals.DataSource = dtRentals;
                        gvRentals.DataBind();
                    }

                    // 2. CONSUMABLES QUERY
                    string qCons = @"
                        SELECT c.ConsumableID as ExtraID, em.EquipmentType as ItemName, c.Quantity as Qty, (c.Quantity * c.UnitPrice) as Price
                        FROM tblConsumable c
                        JOIN tblEquipmentModel em ON c.ModelID = em.ModelID
                        WHERE c.ReservationID = @ResID";

                    using (SqlCommand cmd = new SqlCommand(qCons, con))
                    {
                        cmd.Parameters.AddWithValue("@ResID", resId);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dtCons = new DataTable();
                        da.Fill(dtCons);
                        gvConsumables.DataSource = dtCons;
                        gvConsumables.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Grid Error", ex.Message);
            }
        }

        protected void gvRentals_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ReturnItem")
            {
                int rentalId = int.Parse(e.CommandArgument.ToString());
                try
                {
                    using (SqlConnection con = new SqlConnection(CS))
                    {
                        using (SqlCommand cmd = new SqlCommand("UPDATE tblRental SET ReturnedAt = GETDATE() WHERE RentalID = @ID", con))
                        {
                            cmd.Parameters.AddWithValue("@ID", rentalId);
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                    ShowAlert("success", "Returned", "Item successfully returned to stock.");
                    LoadExtrasGrid(Convert.ToInt32(hfSelectedResID.Value));
                    LoadEquipmentModels(); // update dropdown counts
                }
                catch (Exception ex)
                {
                    ShowAlert("error", "Return Error", ex.Message);
                }
            }
        }

        protected void btnAddExtra_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(hfSelectedResID.Value)) return;
            int resId = Convert.ToInt32(hfSelectedResID.Value);
            int modelId = Convert.ToInt32(ddlEquipmentModel.SelectedValue);
            int qty;

            if (!int.TryParse(txtExtraQty.Text, out qty) || qty <= 0)
            {
                ShowAlert("warning", "Invalid Input", "Please enter a valid quantity greater than zero.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(CS))
                {
                    con.Open();

                    string category = "";
                    decimal price = 0;
                    int actualStock = 0;

                    string checkStockQ = @"
                        SELECT ItemCategory, COALESCE(DefaultRentalPrice, DefaultSellPrice, 0) as Price,
                               CASE 
                                   WHEN ItemCategory = 'Rental' THEN (SELECT COUNT(*) FROM tblEquipmentItem ei WHERE ei.ModelID = m.ModelID AND ei.IsDeleted = 0 AND ei.ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL))
                                   WHEN ItemCategory = 'Consumable' THEN ISNULL(ConsumableQty, 0)
                                   ELSE 0 
                               END as AvailStock
                        FROM tblEquipmentModel m WHERE ModelID = @ID";

                    using (SqlCommand cmdCat = new SqlCommand(checkStockQ, con))
                    {
                        cmdCat.Parameters.AddWithValue("@ID", modelId);
                        using (SqlDataReader dr = cmdCat.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                category = dr.GetString(0);
                                price = dr.GetDecimal(1);
                                actualStock = dr.GetInt32(2);
                            }
                        }
                    }

                    if (qty > actualStock)
                    {
                        ShowAlert("warning", "Insufficient Stock", $"You cannot add {qty}. There are only {actualStock} currently available.");
                        return;
                    }

                    int? userId = null;
                    int? walkInId = null;
                    using (SqlCommand cmdUser = new SqlCommand("SELECT UserID, WalkInID FROM tblReservation WHERE ReservationID = @ID", con))
                    {
                        cmdUser.Parameters.AddWithValue("@ID", resId);
                        using (SqlDataReader drUser = cmdUser.ExecuteReader())
                        {
                            if (drUser.Read())
                            {
                                userId = drUser.IsDBNull(0) ? (int?)null : drUser.GetInt32(0);
                                walkInId = drUser.IsDBNull(1) ? (int?)null : drUser.GetInt32(1);
                            }
                        }
                    }

                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        try
                        {
                            if (category == "Consumable")
                            {
                                using (SqlCommand cmdCons = new SqlCommand(@"
                                    INSERT INTO tblConsumable (UserID, WalkInID, ReservationID, ModelID, Quantity, UnitPrice, IsPaid) 
                                    VALUES (@UID, @WID, @ResID, @ModID, @Qty, @Price, 0)", con, tx))
                                {
                                    cmdCons.Parameters.AddWithValue("@UID", (object)userId ?? DBNull.Value);
                                    cmdCons.Parameters.AddWithValue("@WID", (object)walkInId ?? DBNull.Value);
                                    cmdCons.Parameters.AddWithValue("@ResID", resId);
                                    cmdCons.Parameters.AddWithValue("@ModID", modelId);
                                    cmdCons.Parameters.AddWithValue("@Qty", qty);
                                    cmdCons.Parameters.AddWithValue("@Price", price);
                                    cmdCons.ExecuteNonQuery();
                                }

                                using (SqlCommand cmdReduce = new SqlCommand("UPDATE tblEquipmentModel SET ConsumableQty = ConsumableQty - @Qty WHERE ModelID = @ModID", con, tx))
                                {
                                    cmdReduce.Parameters.AddWithValue("@Qty", qty);
                                    cmdReduce.Parameters.AddWithValue("@ModID", modelId);
                                    cmdReduce.ExecuteNonQuery();
                                }
                            }
                            else if (category == "Rental")
                            {
                                DataTable dtAvailable = new DataTable();
                                using (SqlCommand cmdStock = new SqlCommand(@"
                                    SELECT TOP (@Qty) ItemID 
                                    FROM tblEquipmentItem 
                                    WHERE ModelID = @ModID AND IsDeleted = 0 
                                      AND ItemID NOT IN (SELECT ItemID FROM tblRental WHERE ReturnedAt IS NULL)", con, tx))
                                {
                                    cmdStock.Parameters.AddWithValue("@Qty", qty);
                                    cmdStock.Parameters.AddWithValue("@ModID", modelId);
                                    using (SqlDataAdapter daStock = new SqlDataAdapter(cmdStock))
                                    {
                                        daStock.Fill(dtAvailable);
                                    }
                                }

                                foreach (DataRow row in dtAvailable.Rows)
                                {
                                    using (SqlCommand cmdRent = new SqlCommand(@"
                                        INSERT INTO tblRental (UserID, WalkInID, ReservationID, ItemID, UnitPrice, IsPaid) 
                                        VALUES (@UID, @WID, @ResID, @ItemID, @Price, 0)", con, tx))
                                    {
                                        cmdRent.Parameters.AddWithValue("@UID", (object)userId ?? DBNull.Value);
                                        cmdRent.Parameters.AddWithValue("@WID", (object)walkInId ?? DBNull.Value);
                                        cmdRent.Parameters.AddWithValue("@ResID", resId);
                                        cmdRent.Parameters.AddWithValue("@ItemID", Convert.ToInt32(row["ItemID"]));
                                        cmdRent.Parameters.AddWithValue("@Price", price);
                                        cmdRent.ExecuteNonQuery();
                                    }
                                }
                            }

                            tx.Commit();
                            ShowAlert("success", "Added!", "Successfully added to reservation.");
                            LoadEquipmentModels(); // Refresh Dropdown numbers
                            LoadExtrasGrid(resId);
                        }
                        catch (Exception innerEx)
                        {
                            tx.Rollback();
                            ShowAlert("error", "Error Adding Item", innerEx.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Processing Error", ex.Message);
            }
        }

        // ========================================================
        // ADD NEW RESERVATION LOGIC
        // ========================================================

        protected void btnShowAddForm_Click(object sender, EventArgs e)
        {
            pnlEmptyState.Visible = false;
            pnlManageForm.Visible = false;
            pnlExtrasForm.Visible = false;
            pnlInputForm.Visible = true;

            txtNewResDate.Text = txtDate.Text;
            txtFirstName.Text = "";
            txtLastName.Text = "";
            ddlNewStartTime.SelectedIndex = 0;
            ddlDuration.SelectedIndex = 0;
            ddlNewPaymentStatus.SelectedIndex = 1; // Default to half paid DP
        }

        protected void ddlCustomerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isWalkIn = ddlCustomerType.SelectedValue == "WalkIn";
            phWalkIn.Visible = isWalkIn;
            phRegisteredUser.Visible = !isWalkIn;
        }

        protected void btnSaveNewRes_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNewResDate.Text))
                {
                    ShowAlert("warning", "Missing Info", "Reservation Date is required.");
                    return;
                }

                if (ddlCustomerType.SelectedValue == "WalkIn" && (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text)))
                {
                    ShowAlert("warning", "Missing Info", "First Name and Last Name are required for Walk-In customers.");
                    return;
                }

                DateTime resDate = DateTime.Parse(txtNewResDate.Text);
                TimeSpan startTime = TimeSpan.Parse(ddlNewStartTime.SelectedValue);
                int duration = int.Parse(ddlDuration.SelectedValue);
                TimeSpan endTime = startTime.Add(TimeSpan.FromHours(duration));

                int courtId = int.Parse(ddlCourt.SelectedValue);
                string sport = ddlSport.SelectedValue;

                decimal totalAmount = duration * COURT_PRICE_PER_HOUR;
                decimal paidAmount = 0m;
                string paymentStatus = ddlNewPaymentStatus.SelectedValue;

                if (paymentStatus == "HalfPaid") paidAmount = totalAmount * 0.50m;
                else if (paymentStatus == "FullyPaid") paidAmount = totalAmount;

                using (SqlConnection con = new SqlConnection(CS))
                {
                    con.Open();

                    int courtNum = 0;
                    using (SqlCommand cmdC = new SqlCommand("SELECT CourtNumber FROM tblCourt WHERE CourtID = @ID", con))
                    {
                        cmdC.Parameters.AddWithValue("@ID", courtId);
                        courtNum = Convert.ToInt32(cmdC.ExecuteScalar());
                    }

                    if ((courtNum >= 1 && courtNum <= 4) && sport.ToLower() == "pickleball")
                    {
                        ShowAlert("warning", "Court Restriction", "Courts 1 to 4 are restricted strictly for Badminton use.");
                        return;
                    }

                    using (SqlTransaction tx = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmdMode = new SqlCommand(@"
                                IF EXISTS (
                                    SELECT 1 FROM tblCourtAvailability a
                                    WHERE a.CourtID = @CourtID AND a.[Date] = @ResDate
                                      AND a.StartTime < @EndTime AND a.EndTime > @StartTime
                                      AND a.ModeName NOT IN ('Reservation', 'PlayForAll')
                                ) SELECT 1 ELSE SELECT 0", con, tx))
                            {
                                cmdMode.Parameters.AddWithValue("@CourtID", courtId);
                                cmdMode.Parameters.AddWithValue("@ResDate", resDate.Date);
                                cmdMode.Parameters.AddWithValue("@StartTime", startTime);
                                cmdMode.Parameters.AddWithValue("@EndTime", endTime);
                                if (Convert.ToInt32(cmdMode.ExecuteScalar()) == 1)
                                    throw new Exception("Court is closed or reserved for an event during this time.");
                            }

                            using (SqlCommand cmdOverlap = new SqlCommand(@"
                                SELECT COUNT(*) FROM tblReservation
                                WHERE CourtID = @CourtID AND ResDate = @ResDate
                                  AND ReservationStatusName IN ('Pending','Approved')
                                  AND (@StartTime < EndTime AND @EndTime > StartTime)", con, tx))
                            {
                                cmdOverlap.Parameters.AddWithValue("@CourtID", courtId);
                                cmdOverlap.Parameters.AddWithValue("@ResDate", resDate.Date);
                                cmdOverlap.Parameters.AddWithValue("@StartTime", startTime);
                                cmdOverlap.Parameters.AddWithValue("@EndTime", endTime);

                                if (Convert.ToInt32(cmdOverlap.ExecuteScalar()) > 0)
                                    throw new Exception("This slot visually overlaps with an existing reservation.");
                            }

                            int? userId = null;
                            int? walkInId = null;

                            if (ddlCustomerType.SelectedValue == "User")
                            {
                                userId = int.Parse(ddlUsers.SelectedValue);
                            }
                            else
                            {
                                using (SqlCommand cmdWalkIn = new SqlCommand(@"
                                    INSERT INTO tblPlayerWalkIn (Firstname, Lastname, BasePaid, QueuePaid) 
                                    VALUES (@FName, @LName, 1, 0); SELECT SCOPE_IDENTITY();", con, tx))
                                {
                                    cmdWalkIn.Parameters.AddWithValue("@FName", txtFirstName.Text.Trim());
                                    cmdWalkIn.Parameters.AddWithValue("@LName", txtLastName.Text.Trim());
                                    walkInId = Convert.ToInt32(cmdWalkIn.ExecuteScalar());
                                }
                            }

                            int staffId = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 1;

                            int reservationId = 0;
                            using (SqlCommand cmdRes = new SqlCommand(@"
                                INSERT INTO tblReservation 
                                (UserID, WalkInID, CourtID, ResDate, StartTime, EndTime, SportName, IsPaid, ReservationStatusName, PaymentStatus, RequiredAmount, ApprovedByStaffID)
                                VALUES (@UserID, @WalkInID, @CourtID, @ResDate, @StartTime, @EndTime, @SportName, @IsPaid, 'Approved', @PaymentStatus, @ReqAmt, @Staff);
                                SELECT SCOPE_IDENTITY();", con, tx))
                            {
                                cmdRes.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                                cmdRes.Parameters.AddWithValue("@WalkInID", (object)walkInId ?? DBNull.Value);
                                cmdRes.Parameters.AddWithValue("@CourtID", courtId);
                                cmdRes.Parameters.AddWithValue("@ResDate", resDate.Date);
                                cmdRes.Parameters.AddWithValue("@StartTime", startTime);
                                cmdRes.Parameters.AddWithValue("@EndTime", endTime);
                                cmdRes.Parameters.AddWithValue("@SportName", sport);
                                cmdRes.Parameters.AddWithValue("@IsPaid", paymentStatus == "FullyPaid" ? 1 : 0);
                                cmdRes.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                                cmdRes.Parameters.AddWithValue("@ReqAmt", totalAmount);
                                cmdRes.Parameters.AddWithValue("@Staff", staffId);

                                reservationId = Convert.ToInt32(cmdRes.ExecuteScalar());
                            }

                            if (paidAmount > 0)
                            {
                                using (SqlCommand cmdPay = new SqlCommand(@"
                                    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ReservationID, PaymentDate, Amount)
                                    VALUES ('Reservation', @UserID, @WalkInID, @ResID, CAST(GETDATE() AS DATE), @Amt)", con, tx))
                                {
                                    cmdPay.Parameters.AddWithValue("@UserID", (object)userId ?? DBNull.Value);
                                    cmdPay.Parameters.AddWithValue("@WalkInID", (object)walkInId ?? DBNull.Value);
                                    cmdPay.Parameters.AddWithValue("@ResID", reservationId);
                                    cmdPay.Parameters.AddWithValue("@Amt", paidAmount);
                                    cmdPay.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            ShowAlert("success", "Success", "Reservation created successfully.");

                            txtDate.Text = resDate.ToString("yyyy-MM-dd");
                            RefreshAllData();
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            ShowAlert("error", "Cannot Save", ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("error", "Validation Error", ex.Message);
            }
        }

        protected void btnCloseSidebar_Click(object sender, EventArgs e)
        {
            ResetSidebar();
        }

        private void ResetSidebar()
        {
            pnlEmptyState.Visible = true;
            pnlManageForm.Visible = false;
            pnlExtrasForm.Visible = false;
            pnlInputForm.Visible = false;
            hfSelectedResID.Value = "";
        }

        private void ShowAlert(string icon, string title, string text)
        {
            string cleanText = text.Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
            ScriptManager.RegisterStartupScript(this, GetType(), "swalTrigger", $"showSweetAlert('{icon}', '{title}', '{cleanText}');", true);
        }
    }
}