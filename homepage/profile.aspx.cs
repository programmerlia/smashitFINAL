using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.homepage
{
    public partial class profile : System.Web.UI.Page
    {

        //---------------------------------HELPER CLASSES
        private string CS => ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect(ResolveUrl("~/homepage/home.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                int userId = Convert.ToInt32(Session["UserID"]);

                lblMsg.Text = "";
                lblEditMsg.Text = "";

                LoadProfile(userId);
                LoadStats(userId);

                DataTable dt = GetReservationsForUser(userId);
                AddComputedColumns(dt);

                BindTopReservation(dt);
                BindReservationLists(dt);
                BindPaymentsModal(userId);
                BindRentalsModal(userId);
                BindConsumablesModal(userId); // NEW
            }
        }

        // -------------------- PROFILE --------------------
        private void LoadProfile(int userId)
        {
            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT Firstname, Lastname, Email, PhoneNumber, Username, CreatedAt, ImgPath
FROM tblPlayerAccount
WHERE UserID = @ID;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;

                    string firstname = Convert.ToString(dr["Firstname"]);
                    string lastname = Convert.ToString(dr["Lastname"]);
                    string email = Convert.ToString(dr["Email"]);
                    string phone = Convert.ToString(dr["PhoneNumber"]);
                    string username = Convert.ToString(dr["Username"]);

                    DateTime created = DateTime.MinValue;
                    if (dr["CreatedAt"] != DBNull.Value) created = Convert.ToDateTime(dr["CreatedAt"]);

                    string imgPath = Convert.ToString(dr["ImgPath"]);

                    lblFirstname.Text = firstname;
                    lblFirstname2.Text = firstname;
                    lblLastname.Text = lastname;
                    lblLastname2.Text = lastname;

                    lblEmail.Text = email;
                    lblPhone.Text = string.IsNullOrWhiteSpace(phone) ? "—" : phone;
                    lblUsername.Text = username;
                    lblCreatedAt.Text = (created == DateTime.MinValue) ? "—" : created.ToString("yyyy-MM-dd");

                    imgAvatar.ImageUrl = ResolveImg(imgPath);
                    imgAvatar.AlternateText = "Profile";

                    txtPhone.Text = phone;
                    txtUsername.Text = username;
                }
            }
        }

        private string ResolveImg(string dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath))
                return ResolveUrl("~/images/person.png");

            dbPath = dbPath.Trim();
            if (dbPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return dbPath;

            return ResolveUrl("~/" + dbPath.TrimStart('~', '/'));
        }

        // -------------------- STATS --------------------
        private void LoadStats(int userId)
        {
            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT
  (SELECT COUNT(*) FROM tblReservation WHERE UserID=@ID) AS TotalBookings,
  (SELECT COUNT(*) FROM tblReservation WHERE UserID=@ID AND ReservationStatusName='Pending') AS PendingCount,
  (SELECT ISNULL(SUM(DATEDIFF(MINUTE, StartTime, EndTime)),0)
   FROM tblReservation
   WHERE UserID=@ID AND ReservationStatusName IN ('Approved','Completed')) AS MinutesPlayed;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;

                    int total = Convert.ToInt32(dr["TotalBookings"]);
                    int pending = Convert.ToInt32(dr["PendingCount"]);
                    int minutes = Convert.ToInt32(dr["MinutesPlayed"]);

                    lblTotalBookings.Text = total.ToString();
                    lblPendingCount.Text = pending.ToString();

                    double hours = minutes / 60.0;
                    lblHoursPlayed.Text = hours.ToString("0.#", CultureInfo.InvariantCulture);
                }
            }
        }

        // -------------------- RESERVATIONS: SINGLE SOURCE OF TRUTH --------------------
        private DataTable GetReservationsForUser(int userId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 200
  r.ReservationID,
  r.ResDate,
  r.StartTime,
  r.EndTime,
  r.SportName,
  r.ReservationStatusName,
  ISNULL(r.RequestStatus,'') AS RequestStatus,
  ISNULL(r.IsPaid,0) AS IsPaid,
  ISNULL(r.PaymentStatus,'') AS PaymentStatus,
  c.CourtNumber
FROM tblReservation r
JOIN tblCourt c ON c.CourtID = r.CourtID
WHERE r.UserID = @ID
ORDER BY r.ResDate DESC, r.StartTime DESC;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }

            return dt;
        }
        

        private void AddComputedColumns(DataTable dt)
        {
            if (!dt.Columns.Contains("StartStr")) dt.Columns.Add("StartStr", typeof(string));
            if (!dt.Columns.Contains("EndStr")) dt.Columns.Add("EndStr", typeof(string));
            if (!dt.Columns.Contains("StatusBadgeClass")) dt.Columns.Add("StatusBadgeClass", typeof(string));
            if (!dt.Columns.Contains("PaymentStatusDisplay")) dt.Columns.Add("PaymentStatusDisplay", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                TimeSpan st = (TimeSpan)row["StartTime"];
                TimeSpan et = (TimeSpan)row["EndTime"];

                row["StartStr"] = DateTime.Today.Add(st).ToString("hh:mm tt");
                row["EndStr"] = DateTime.Today.Add(et).ToString("hh:mm tt");

                string status = Convert.ToString(row["ReservationStatusName"] ?? "");
                row["StatusBadgeClass"] = StatusBadge(status);

                bool isPaid = row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]);
                string pay = Convert.ToString(row["PaymentStatus"] ?? "");

                row["PaymentStatusDisplay"] =
                    (isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase))
                    ? "Paid"
                    : (string.IsNullOrWhiteSpace(pay) ? "Unpaid" : pay);
            }
        }

        // -------------------- TOP 1 ON PROFILE (APPROVED UPCOMING FIRST) --------------------
        private void BindTopReservation(DataTable dt)
        {
            pnlPendingTop1.Controls.Clear();

            if (dt == null || dt.Rows.Count == 0)
            {
                pnlPendingTop1.Controls.Add(new Literal { Text = "<div class='muted'>No reservations yet.</div>" });
                return;
            }

            DateTime now = DateTime.Now;

            // ✅ Most upcoming APPROVED
            DataRow top = dt.AsEnumerable()
                .Where(r => string.Equals(r.Field<string>("ReservationStatusName"), "Approved", StringComparison.OrdinalIgnoreCase))
                .Select(r => new
                {
                    Row = r,
                    StartDT = r.Field<DateTime>("ResDate").Date.Add(r.Field<TimeSpan>("StartTime"))
                })
                .Where(x => x.StartDT >= now)
                .OrderBy(x => x.StartDT)
                .Select(x => x.Row)
                .FirstOrDefault();

            // fallback: most upcoming PENDING (if no approved upcoming)
            if (top == null)
            {
                top = dt.AsEnumerable()
                    .Where(r => string.Equals(r.Field<string>("ReservationStatusName"), "Pending", StringComparison.OrdinalIgnoreCase))
                    .Select(r => new
                    {
                        Row = r,
                        StartDT = r.Field<DateTime>("ResDate").Date.Add(r.Field<TimeSpan>("StartTime"))
                    })
                    .Where(x => x.StartDT >= now)
                    .OrderBy(x => x.StartDT)
                    .Select(x => x.Row)
                    .FirstOrDefault();
            }

            if (top == null)
            {
                pnlPendingTop1.Controls.Add(new Literal { Text = "<div class='muted'>No upcoming approved/pending reservations.</div>" });
                return;
            }

            pnlPendingTop1.Controls.Add(new Literal { Text = BuildTopReservationHtml(top) });
        }

        private string BuildTopReservationHtml(DataRow r)
        {
            string resId = HttpUtility.HtmlEncode(Convert.ToString(r["ReservationID"]));
            string court = HttpUtility.HtmlEncode(Convert.ToString(r["CourtNumber"]));
            string status = Convert.ToString(r["ReservationStatusName"]);
            string statusEncoded = HttpUtility.HtmlEncode(status);

            DateTime resDate = Convert.ToDateTime(r["ResDate"]);
            TimeSpan start = (TimeSpan)r["StartTime"];
            TimeSpan end = (TimeSpan)r["EndTime"];

            DateTime startDT = resDate.Date.Add(start);
            TimeSpan diff = startDT - DateTime.Now;

            string emphasis;
            if (diff.TotalSeconds <= 0)
                emphasis = "Ongoing / Passed";
            else if (diff.TotalDays >= 1)
                emphasis = ((int)Math.Floor(diff.TotalDays)) + " day(s) left";
            else
                emphasis = ((int)Math.Floor(diff.TotalHours)) + " hour(s) left";

            string badgeClass = StatusBadge(status);

            string dateStr = HttpUtility.HtmlEncode(resDate.ToString("yyyy-MM-dd"));
            string startStr = HttpUtility.HtmlEncode(DateTime.Today.Add(start).ToString("hh:mm tt"));
            string endStr = HttpUtility.HtmlEncode(DateTime.Today.Add(end).ToString("hh:mm tt"));
            string badgeClassEncoded = HttpUtility.HtmlEncode(badgeClass);
            string emphasisEncoded = HttpUtility.HtmlEncode(emphasis);

            return $@"
<div class='res-item'>
  <div class='res-top'>
    <div>
      <div style='font-weight:800;'>
        Reservation #{resId} • Court {court} • {dateStr}
      </div>
      <div class='muted'>
        {startStr} - {endStr}
      </div>
      <div class='mt-1'>
        <span class='badge-status {badgeClassEncoded}'>{statusEncoded}</span>
        <span class='badge-status badge-approved ms-1' style='font-weight:800;'>{emphasisEncoded}</span>
      </div>
    </div>
  </div>
</div>";
        }
        private string StatusBadge(string status)
        {
            string s = (status ?? "").Trim().ToLowerInvariant();
            if (s == "pending") return "badge-pending";
            if (s == "approved") return "badge-approved";
            if (s == "cancelled") return "badge-cancelled";
            if (s == "completed") return "badge-completed";
            return "badge-request";
        }

        // -------------------- MODAL LISTS (CATEGORIES) --------------------
        private void BindReservationLists(DataTable dt)
        {
            DataView dvRequests = new DataView(dt);
            dvRequests.RowFilter = "RequestStatus <> ''";

            DataView dvApproved = new DataView(dt);
            dvApproved.RowFilter = "ReservationStatusName = 'Approved'";

            DataView dvPending = new DataView(dt);
            dvPending.RowFilter = "ReservationStatusName = 'Pending'";

            DataView dvCancelled = new DataView(dt);
            dvCancelled.RowFilter = "ReservationStatusName = 'Cancelled'";

            DataView dvCompleted = new DataView(dt);
            dvCompleted.RowFilter = "ReservationStatusName = 'Completed'";

            rptApproved.DataSource = dvApproved;
            rptApproved.DataBind();

            rptPending.DataSource = dvPending;
            rptPending.DataBind();

            rptRequests.DataSource = dvRequests;
            rptRequests.DataBind();

            rptCancelled.DataSource = dvCancelled;
            rptCancelled.DataBind();

            rptCompleted.DataSource = dvCompleted;
            rptCompleted.DataBind();
        }
        // -------------------- Rules --------------------
        private bool CanRequestCancel(DateTime resDate, string status)
        {
            if (!(status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                  status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
                return false;

            return resDate.Date >= DateTime.Today.AddDays(1);
        }

        // -------------------- Repeater events --------------------
        protected void rptReservations_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            DataRowView row = (DataRowView)e.Item.DataItem;

            DateTime resDate = Convert.ToDateTime(row["ResDate"]);
            string status = Convert.ToString(row["ReservationStatusName"] ?? "");
            string req = Convert.ToString(row["RequestStatus"] ?? "");
            bool isPaid = row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]);
            string pay = Convert.ToString(row["PaymentStatus"] ?? "");
            bool paid = isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase);

            Button btnCancel = (Button)e.Item.FindControl("btnCancelReq");
            Label hint = (Label)e.Item.FindControl("lblRuleHint");

            bool alreadyRequested = !string.IsNullOrWhiteSpace(req);

            bool canCancel = !alreadyRequested && CanRequestCancel(resDate, status);
            

            if (btnCancel != null) btnCancel.Enabled = canCancel;

            if (hint != null)
            {
                if (alreadyRequested)
                    hint.Text = "You already have a request on this reservation. Please wait for staff/admin response.";
                else if (!canCancel && (status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
                    hint.Text = "Cancel request is allowed only at least 1 day before the reservation date.";
                else
                    hint.Text = "";
            }
        }

        protected void rptReservations_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int userId = Convert.ToInt32(Session["UserID"]);
            int reservationId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "cancel") HandleCancelRequest(userId, reservationId);

            // Rebind everything
            DataTable dt = GetReservationsForUser(userId);
            AddComputedColumns(dt);
            BindTopReservation(dt);
            BindReservationLists(dt);

            ReopenAllResModal();
        }

        private void HandleCancelRequest(int userId, int reservationId)
        {
            DateTime resDate;
            string status;
            string req;

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT ResDate, ReservationStatusName, ISNULL(RequestStatus,'') AS RequestStatus
FROM tblReservation
WHERE ReservationID=@RID AND UserID=@UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) { ShowMsg("Reservation not found.", true); return; }

                    resDate = Convert.ToDateTime(dr["ResDate"]);
                    status = Convert.ToString(dr["ReservationStatusName"] ?? "");
                    req = Convert.ToString(dr["RequestStatus"] ?? "");
                }
            }

            if (!string.IsNullOrWhiteSpace(req)) { ShowMsg("You already submitted a request for this reservation.", true); return; }
            if (!CanRequestCancel(resDate, status)) { ShowMsg("Cancel request not allowed. You can only cancel at least 1 day prior to the reservation.", true); return; }

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE tblReservation
SET RequestStatus='CancelRequest'
WHERE ReservationID=@RID AND UserID=@UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            ShowMsg("Cancel request sent. Please wait for admin approval.", false);
        }

    
        private void ShowMsg(string text, bool isError)
        {
            string css = isError ? "alert alert-danger" : "alert alert-success";
            lblMsg.Text = "<div class='" + css + "' role='alert'>" + HttpUtility.HtmlEncode(text) + "</div>";
        }

        private void ReopenAllResModal()
        {
            ScriptManager.RegisterStartupScript(
                this, this.GetType(),
                "reopenAllResModal",
                "var el=document.getElementById('allResModal'); if(el){ var m=bootstrap.Modal.getOrCreateInstance(el); m.show(); }",
                true
            );
        }

        // -------------------- SAVE PROFILE --------------------
        protected void btnSaveProfile_Click(object sender, EventArgs e)
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            string phone = (txtPhone.Text ?? "").Trim();
            string username = (txtUsername.Text ?? "").Trim();
            string newPass = (txtNewPassword.Text ?? "");
            string confirm = (txtConfirmPassword.Text ?? "");

            if (string.IsNullOrWhiteSpace(username))
            {
                lblEditMsg.Text = "<div class='alert alert-danger'>Username is required.</div>";
                return;
            }

            if (!string.IsNullOrWhiteSpace(newPass) || !string.IsNullOrWhiteSpace(confirm))
            {
                if (newPass != confirm)
                {
                    lblEditMsg.Text = "<div class='alert alert-danger'>Password confirmation does not match.</div>";
                    return;
                }
                if (newPass.Length < 4)
                {
                    lblEditMsg.Text = "<div class='alert alert-danger'>Password must be at least 4 characters.</div>";
                    return;
                }
            }

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT COUNT(*) FROM tblPlayerAccount
WHERE Username=@U AND UserID<>@ID;", con))
            {
                cmd.Parameters.AddWithValue("@U", username);
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                int exists = Convert.ToInt32(cmd.ExecuteScalar());
                if (exists > 0)
                {
                    lblEditMsg.Text = "<div class='alert alert-danger'>Username already taken.</div>";
                    return;
                }
            }

            string newImgPath = null;

            if (fuAvatar != null && fuAvatar.HasFile)
            {
                string ext = Path.GetExtension(fuAvatar.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                {
                    lblEditMsg.Text = "<div class='alert alert-danger'>Invalid image type. Use JPG/PNG/WEBP.</div>";
                    return;
                }

                string folderRel = "uploads/avatars";
                string folderAbs = Server.MapPath("~/" + folderRel);
                Directory.CreateDirectory(folderAbs);

                string fileName = "u" + userId + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ext;
                string absPath = Path.Combine(folderAbs, fileName);

                fuAvatar.SaveAs(absPath);
                newImgPath = folderRel + "/" + fileName;
            }

            string sql = @"
UPDATE tblPlayerAccount
SET PhoneNumber=@P,
    Username=@U";

            if (!string.IsNullOrWhiteSpace(newPass)) sql += ", [Password]=@PW";
            if (newImgPath != null) sql += ", ImgPath=@IMG";
            sql += " WHERE UserID=@ID;";

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@P", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@U", username);
                cmd.Parameters.AddWithValue("@ID", userId);

                if (!string.IsNullOrWhiteSpace(newPass)) cmd.Parameters.AddWithValue("@PW", newPass);
                if (newImgPath != null) cmd.Parameters.AddWithValue("@IMG", newImgPath);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblEditMsg.Text = "<div class='alert alert-success'>Profile updated successfully.</div>";
            LoadProfile(userId);
        }

        // -------------------- LOGOUT --------------------
        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();

            Response.Redirect(ResolveUrl("~/homepage/home.aspx"), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        //------------------- SHOW PAYMENTS -----------------
        private sealed class PaymentRowVM
        {
            public int PaymentID { get; set; }
            public DateTime? PaymentDate { get; set; }
            public string PaymentTypeName { get; set; }

            public int? ReservationID { get; set; }
            public int? RentalID { get; set; }
            public int? ConsumableID { get; set; }

            public string DetailsLine1 { get; set; }
            public string DetailsLine2 { get; set; }

            public decimal AmountPhp { get; set; }
        }

        private void BindPaymentsModal(int userId)
        {
            var list = new List<PaymentRowVM>();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 400
    p.PaymentID,
    p.PaymentDate,
    p.PaymentTypeName,
    p.ReservationID,
    p.RentalID,
    p.ConsumableID,
    p.Amount,

    -- reservation
    res.ResDate,
    res.StartTime AS ResStart,
    res.EndTime AS ResEnd,
    c.CourtNumber,

    -- rental
    rntl.RentalDate,
    rntl.ReturnedAt,
    rm.EquipmentType AS RentalEquipmentType,
    ISNULL(rm.EquipmentSpec,'') AS RentalEquipmentSpec,

    -- consumable
    cons.PurchaseDate,
    cons.Quantity,
    cons.UnitPrice AS ConsumableUnitPrice,
    cm.EquipmentType AS ConsumableType,
    ISNULL(cm.EquipmentSpec,'') AS ConsumableSpec

FROM tblPayment p
LEFT JOIN tblReservation res ON res.ReservationID = p.ReservationID
LEFT JOIN tblCourt c ON c.CourtID = res.CourtID

LEFT JOIN tblRental rntl ON rntl.RentalID = p.RentalID
LEFT JOIN tblEquipmentItem rei ON rei.ItemID = rntl.ItemID
LEFT JOIN tblEquipmentModel rm ON rm.ModelID = rei.ModelID

LEFT JOIN tblConsumable cons ON cons.ConsumableID = p.ConsumableID
LEFT JOIN tblEquipmentModel cm ON cm.ModelID = cons.ModelID

WHERE p.UserID = @UID
ORDER BY ISNULL(p.PaymentDate, '19000101') DESC, p.PaymentID DESC;", con))
            {
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string type = Convert.ToString(dr["PaymentTypeName"] ?? "");
                        int paymentId = Convert.ToInt32(dr["PaymentID"]);
                        int? reservationId = dr["ReservationID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["ReservationID"]);
                        int? rentalId = dr["RentalID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["RentalID"]);
                        int? consumableId = dr["ConsumableID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["ConsumableID"]);

                        decimal amountPhp = dr["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Amount"]);

                        string line1 = "";
                        string line2 = "";

                        if (reservationId.HasValue && dr["ResDate"] != DBNull.Value)
                        {
                            DateTime resDate = Convert.ToDateTime(dr["ResDate"]);
                            TimeSpan st = (TimeSpan)dr["ResStart"];
                            TimeSpan et = (TimeSpan)dr["ResEnd"];

                            string startStr = DateTime.Today.Add(st).ToString("hh:mm tt");
                            string endStr = DateTime.Today.Add(et).ToString("hh:mm tt");
                            string court = dr["CourtNumber"] == DBNull.Value ? "" : ("Court " + Convert.ToInt32(dr["CourtNumber"]));

                            line1 = $"Reservation #{reservationId.Value} • {court}";
                            line2 = $"{resDate:yyyy-MM-dd} • {startStr} - {endStr}";
                        }
                        else if (rentalId.HasValue && dr["RentalDate"] != DBNull.Value)
                        {
                            DateTime rentalDate = Convert.ToDateTime(dr["RentalDate"]);
                            string equipType = Convert.ToString(dr["RentalEquipmentType"] ?? "");
                            string spec = Convert.ToString(dr["RentalEquipmentSpec"] ?? "");
                            string itemName = string.IsNullOrWhiteSpace(spec) ? equipType : $"{equipType} ({spec})";

                            string returned = (dr["ReturnedAt"] == DBNull.Value)
                                ? "Not returned"
                                : Convert.ToDateTime(dr["ReturnedAt"]).ToString("yyyy-MM-dd HH:mm");

                            line1 = $"Rental #{rentalId.Value} • {itemName}";
                            line2 = $"{rentalDate:yyyy-MM-dd HH:mm} • Returned: {returned}";
                        }
                        else if (consumableId.HasValue && dr["PurchaseDate"] != DBNull.Value)
                        {
                            DateTime purchaseDate = Convert.ToDateTime(dr["PurchaseDate"]);
                            int qty = dr["Quantity"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Quantity"]);
                            decimal unitPrice = dr["ConsumableUnitPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["ConsumableUnitPrice"]);

                            string conType = Convert.ToString(dr["ConsumableType"] ?? "");
                            string spec = Convert.ToString(dr["ConsumableSpec"] ?? "");
                            string itemName = string.IsNullOrWhiteSpace(spec) ? conType : $"{conType} ({spec})";

                            line1 = $"Consumable #{consumableId.Value} • {itemName}";
                            line2 = $"{purchaseDate:yyyy-MM-dd HH:mm} • Qty: {qty} • Unit: ₱ {unitPrice:N2}";
                        }
                        else
                        {
                            line1 = $"Payment #{paymentId}";
                            line2 = "—";
                        }

                        list.Add(new PaymentRowVM
                        {
                            PaymentID = paymentId,
                            PaymentDate = dr["PaymentDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["PaymentDate"]),
                            PaymentTypeName = type,
                            ReservationID = reservationId,
                            RentalID = rentalId,
                            ConsumableID = consumableId,
                            DetailsLine1 = line1,
                            DetailsLine2 = line2,
                            AmountPhp = amountPhp
                        });
                    }
                }
            }

            pnlPaymentsEmpty.Visible = (list.Count == 0);
            rptPayments.DataSource = list;
            rptPayments.DataBind();
        }
        // --------------RENTALS MODAL --------------
        private sealed class RentalRowVM
        {
            public int RentalID { get; set; }
            public int? ReservationID { get; set; }
            public DateTime RentalDate { get; set; }
            public DateTime? ReturnedAt { get; set; }

            public string ItemName { get; set; }
            public decimal UnitPrice { get; set; }
            public string IsPaidDisplay { get; set; }

            public string ReservationInfo { get; set; }
            public string ReturnedAtDisplay { get; set; }
        }

        private void BindRentalsModal(int userId)
        {
            var list = new List<RentalRowVM>();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 500
    rntl.RentalID,
    rntl.ReservationID,
    rntl.RentalDate,
    rntl.ReturnedAt,
    rntl.UnitPrice,
    rntl.IsPaid,

    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,

    res.ResDate,
    res.StartTime,
    res.EndTime,
    c.CourtNumber

FROM tblRental rntl
JOIN tblEquipmentItem ei ON ei.ItemID = rntl.ItemID
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
LEFT JOIN tblReservation res ON res.ReservationID = rntl.ReservationID
LEFT JOIN tblCourt c ON c.CourtID = res.CourtID

WHERE rntl.UserID = @UID
ORDER BY rntl.RentalDate DESC, rntl.RentalID DESC;", con))
            {
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string type = Convert.ToString(dr["EquipmentType"]);
                        string spec = Convert.ToString(dr["EquipmentSpec"]);
                        string itemName = string.IsNullOrWhiteSpace(spec) ? type : $"{type} ({spec})";

                        bool isPaid = Convert.ToBoolean(dr["IsPaid"]);
                        DateTime rentalDate = Convert.ToDateTime(dr["RentalDate"]);

                        DateTime? returnedAt = dr["ReturnedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["ReturnedAt"]);
                        string returnedDisplay = returnedAt.HasValue ? returnedAt.Value.ToString("yyyy-MM-dd HH:mm") : "Not returned";

                        string reservationInfo = "Walk-in / No reservation";
                        if (dr["ReservationID"] != DBNull.Value && dr["ResDate"] != DBNull.Value)
                        {
                            int rid = Convert.ToInt32(dr["ReservationID"]);
                            DateTime resDate = Convert.ToDateTime(dr["ResDate"]);
                            TimeSpan st = (TimeSpan)dr["StartTime"];
                            TimeSpan et = (TimeSpan)dr["EndTime"];
                            string startStr = DateTime.Today.Add(st).ToString("hh:mm tt");
                            string endStr = DateTime.Today.Add(et).ToString("hh:mm tt");
                            string court = dr["CourtNumber"] == DBNull.Value ? "" : ("Court " + Convert.ToInt32(dr["CourtNumber"]));

                            reservationInfo = $"Reservation #{rid} • {court} • {resDate:yyyy-MM-dd} • {startStr}-{endStr}";
                        }

                        list.Add(new RentalRowVM
                        {
                            RentalID = Convert.ToInt32(dr["RentalID"]),
                            ReservationID = dr["ReservationID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["ReservationID"]),
                            RentalDate = rentalDate,
                            ReturnedAt = returnedAt,
                            ItemName = itemName,
                            UnitPrice = Convert.ToDecimal(dr["UnitPrice"]),
                            IsPaidDisplay = isPaid ? "Yes" : "No",
                            ReservationInfo = reservationInfo,
                            ReturnedAtDisplay = returnedDisplay
                        });
                    }
                }
            }

            pnlRentalsEmpty.Visible = (list.Count == 0);
            rptRentals.DataSource = list;
            rptRentals.DataBind();
        }

        //CONSUMABLES MODAL
        private sealed class ConsumableRowVM
        {
            public int ConsumableID { get; set; }
            public int? ReservationID { get; set; }
            public DateTime PurchaseDate { get; set; }

            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string IsPaidDisplay { get; set; }

            public string ReservationInfo { get; set; }
        }


        private void BindConsumablesModal(int userId)
        {
            var list = new List<ConsumableRowVM>();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT TOP 500
    csm.ConsumableID,
    csm.ReservationID,
    csm.PurchaseDate,
    csm.Quantity,
    csm.UnitPrice,
    csm.IsPaid,

    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,

    res.ResDate,
    res.StartTime,
    res.EndTime,
    crt.CourtNumber

FROM tblConsumable csm
JOIN tblEquipmentModel m ON m.ModelID = csm.ModelID
LEFT JOIN tblReservation res ON res.ReservationID = csm.ReservationID
LEFT JOIN tblCourt crt ON crt.CourtID = res.CourtID

WHERE csm.UserID = @UID
ORDER BY csm.PurchaseDate DESC, csm.ConsumableID DESC;", con))
            {
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string type = Convert.ToString(dr["EquipmentType"]);
                        string spec = Convert.ToString(dr["EquipmentSpec"]);
                        string itemName = string.IsNullOrWhiteSpace(spec) ? type : $"{type} ({spec})";

                        int qty = Convert.ToInt32(dr["Quantity"]);
                        decimal unitPrice = Convert.ToDecimal(dr["UnitPrice"]);


                        bool isPaid = Convert.ToBoolean(dr["IsPaid"]);

                        string reservationInfo = "Walk-in / No reservation";
                        if (dr["ReservationID"] != DBNull.Value && dr["ResDate"] != DBNull.Value)
                        {
                            int rid = Convert.ToInt32(dr["ReservationID"]);
                            DateTime resDate = Convert.ToDateTime(dr["ResDate"]);
                            TimeSpan st = (TimeSpan)dr["StartTime"];
                            TimeSpan et = (TimeSpan)dr["EndTime"];
                            string startStr = DateTime.Today.Add(st).ToString("hh:mm tt");
                            string endStr = DateTime.Today.Add(et).ToString("hh:mm tt");
                            string court = dr["CourtNumber"] == DBNull.Value ? "" : ("Court " + Convert.ToInt32(dr["CourtNumber"]));

                            reservationInfo = $"Reservation #{rid} • {court} • {resDate:yyyy-MM-dd} • {startStr}-{endStr}";
                        }

                        list.Add(new ConsumableRowVM
                        {
                            ConsumableID = Convert.ToInt32(dr["ConsumableID"]),
                            ReservationID = dr["ReservationID"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["ReservationID"]),
                            PurchaseDate = Convert.ToDateTime(dr["PurchaseDate"]),
                            ItemName = itemName,
                            Quantity = qty,
                            UnitPrice = unitPrice,
                            TotalPrice = qty * unitPrice,
                            IsPaidDisplay = isPaid ? "Yes" : "No",
                            ReservationInfo = reservationInfo
                        });
                    }
                }
            }

            pnlConsumablesEmpty.Visible = (list.Count == 0);
            rptConsumables.DataSource = list;
            rptConsumables.DataBind();
        }
    }
}