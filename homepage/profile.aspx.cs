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
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

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



        // ------------------- BUNCH OF HELPER ------------
        private sealed class PaymentGroupVM
        {
            public string GroupTitle { get; set; }
            public List<PaymentRowVM> Items { get; set; }
        }

        // ------------------- cloudinary helper -----------
        private string UploadImageToCloudinary(FileUpload fu, string currentImagePath, string folderName, string filePrefix)
        {
            if (fu == null || !fu.HasFile)
                return currentImagePath;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            string fileExtension = Path.GetExtension(fu.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new Exception("Invalid file type. Allowed: jpg, jpeg, png, gif, webp.");

            string cloudname = ConfigurationManager.AppSettings["CloudinaryCloudName"];
            string apikey = ConfigurationManager.AppSettings["CloudinaryApiKey"];
            string secretKey = ConfigurationManager.AppSettings["CloudinaryApiSecret"];

            Account account = new Account(cloudname, apikey, secretKey);
            Cloudinary cloudinary = new Cloudinary(account);

            string uniqueFileName = $"{filePrefix}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fu.FileName, fu.PostedFile.InputStream),
                Folder = folderName,
                PublicId = uniqueFileName,
                Overwrite = false,
                UseFilename = false,
                UniqueFilename = false
            };

            var uploadResult = cloudinary.Upload(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception(uploadResult.Error.Message);

            return uploadResult.SecureUrl?.ToString() ?? currentImagePath;
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

            if (dbPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                dbPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return dbPath;

            return ResolveUrl("~/" + dbPath.TrimStart('~', '/'));
        }

        // -------------------- STATS --------------------
        private void LoadStats(int userId)
        {
            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @WeekStart DATE = DATEADD(DAY, 1 - DATEPART(WEEKDAY, @Today), @Today);
DECLARE @MonthStart DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);

SELECT
    (SELECT COUNT(*)
     FROM tblReservation
     WHERE UserID = @ID) AS TotalBookings,

    (SELECT COUNT(*)
     FROM tblReservation
     WHERE UserID = @ID
       AND ReservationStatusName = 'Pending'
       AND ISNULL(RequestStatus,'') = '') AS PendingCount,

    (SELECT ISNULL(SUM(DATEDIFF(MINUTE, StartTime, EndTime)), 0)
     FROM tblReservation
     WHERE UserID = @ID
       AND ReservationStatusName IN ('Approved','Completed')) AS MinutesPlayed,

    (SELECT COUNT(*)
     FROM tblRental rt
     WHERE rt.UserID = @ID
        OR EXISTS
          (
              SELECT 1
              FROM tblReservation rr
              WHERE rr.ReservationID = rt.ReservationID
                AND rr.UserID = @ID
          )) AS TotalRentals,

    (SELECT ISNULL(SUM(cs.Quantity), 0)
     FROM tblConsumable cs
     WHERE cs.UserID = @ID
        OR EXISTS
          (
              SELECT 1
              FROM tblReservation rr
              WHERE rr.ReservationID = cs.ReservationID
                AND rr.UserID = @ID
          )) AS TotalConsumables,

    (SELECT ISNULL(SUM(CAST(p.Amount AS DECIMAL(10,2))), 0)
     FROM tblPayment p
     WHERE (p.UserID = @ID
        OR EXISTS
          (
              SELECT 1
              FROM tblReservation rr
              WHERE rr.ReservationID = p.ReservationID
                AND rr.UserID = @ID
          ))
       AND p.PaymentDate >= @WeekStart
       AND p.PaymentDate < DATEADD(DAY, 7, @WeekStart)) AS SpentThisWeek,

    (SELECT ISNULL(SUM(CAST(p.Amount AS DECIMAL(10,2))), 0)
     FROM tblPayment p
     WHERE (p.UserID = @ID
        OR EXISTS
          (
              SELECT 1
              FROM tblReservation rr
              WHERE rr.ReservationID = p.ReservationID
                AND rr.UserID = @ID
          ))
       AND p.PaymentDate >= @MonthStart
       AND p.PaymentDate < DATEADD(MONTH, 1, @MonthStart)) AS SpentThisMonth;
", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;

                    int totalBookings = Convert.ToInt32(dr["TotalBookings"]);
                    int pendingCount = Convert.ToInt32(dr["PendingCount"]);
                    int minutesPlayed = Convert.ToInt32(dr["MinutesPlayed"]);
                    int totalRentals = Convert.ToInt32(dr["TotalRentals"]);
                    int totalConsumables = Convert.ToInt32(dr["TotalConsumables"]);

                    decimal spentThisWeek = Convert.ToDecimal(dr["SpentThisWeek"]);
                    decimal spentThisMonth = Convert.ToDecimal(dr["SpentThisMonth"]);

                    lblTotalBookings.Text = totalBookings.ToString();
                    lblPendingCount.Text = pendingCount.ToString();

                    double hours = minutesPlayed / 60.0;
                    lblHoursPlayed.Text = hours.ToString("0.#", CultureInfo.InvariantCulture);

                    lblTotalRentals.Text = totalRentals.ToString();
                    lblTotalConsumables.Text = totalConsumables.ToString();

                    lblSpentThisWeek.Text = "₱ " + spentThisWeek.ToString("N2");
                    lblSpentThisMonth.Text = "₱ " + spentThisMonth.ToString("N2");
                }
            }
        }

        // -------------------- RESERVATIONS-------------------

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
    ISNULL(r.RequiredAmount,0) AS RequiredAmount,
    ISNULL(pay.TotalPaid,0) AS TotalPaid,
    c.CourtNumber,

    ISNULL(rt.RentalCount,0) AS RentalCount,
    ISNULL(rt.RentalAmount,0) AS RentalAmount,

    ISNULL(cs.ConsumableQty,0) AS ConsumableQty,
    ISNULL(cs.ConsumableAmount,0) AS ConsumableAmount

FROM tblReservation r
JOIN tblCourt c ON c.CourtID = r.CourtID

LEFT JOIN (
    SELECT ReservationID, SUM(CAST(ISNULL(Amount,0) AS DECIMAL(10,2))) AS TotalPaid
    FROM tblPayment
    WHERE ReservationID IS NOT NULL
    GROUP BY ReservationID
) pay ON pay.ReservationID = r.ReservationID

LEFT JOIN (
    SELECT ReservationID,
           COUNT(*) AS RentalCount,
           SUM(CAST(ISNULL(UnitPrice,0) AS DECIMAL(10,2))) AS RentalAmount
    FROM tblRental
    WHERE ReservationID IS NOT NULL
    GROUP BY ReservationID
) rt ON rt.ReservationID = r.ReservationID

LEFT JOIN (
    SELECT ReservationID,
           SUM(ISNULL(Quantity,0)) AS ConsumableQty,
           SUM(CAST(ISNULL(Quantity,0) * ISNULL(UnitPrice,0) AS DECIMAL(10,2))) AS ConsumableAmount
    FROM tblConsumable
    WHERE ReservationID IS NOT NULL
    GROUP BY ReservationID
) cs ON cs.ReservationID = r.ReservationID

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
            if (!dt.Columns.Contains("FriendlyDate")) dt.Columns.Add("FriendlyDate", typeof(string));
            if (!dt.Columns.Contains("TimeRangeDisplay")) dt.Columns.Add("TimeRangeDisplay", typeof(string));
            if (!dt.Columns.Contains("StatusBadgeClass")) dt.Columns.Add("StatusBadgeClass", typeof(string));
            if (!dt.Columns.Contains("PaymentStatusDisplay")) dt.Columns.Add("PaymentStatusDisplay", typeof(string));
            if (!dt.Columns.Contains("PaymentBalanceDisplay")) dt.Columns.Add("PaymentBalanceDisplay", typeof(string));
            if (!dt.Columns.Contains("PaidAmountDisplay")) dt.Columns.Add("PaidAmountDisplay", typeof(string));
            if (!dt.Columns.Contains("ExtrasDisplay")) dt.Columns.Add("ExtrasDisplay", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                TimeSpan st = (TimeSpan)row["StartTime"];
                TimeSpan et = (TimeSpan)row["EndTime"];
                DateTime resDate = Convert.ToDateTime(row["ResDate"]);

                string startStr = DateTime.Today.Add(st).ToString("hh:mm tt");
                string endStr = DateTime.Today.Add(et).ToString("hh:mm tt");

                row["StartStr"] = startStr;
                row["EndStr"] = endStr;
                row["FriendlyDate"] = resDate.ToString("dddd, MMM d, yyyy");
                row["TimeRangeDisplay"] = startStr + " - " + endStr;

                string status = Convert.ToString(row["ReservationStatusName"] ?? "");
                row["StatusBadgeClass"] = StatusBadge(status);

                bool isPaid = row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]);
                string pay = Convert.ToString(row["PaymentStatus"] ?? "");

                decimal requiredAmount = row["RequiredAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["RequiredAmount"]);
                decimal totalPaid = row["TotalPaid"] == DBNull.Value ? 0m : Convert.ToDecimal(row["TotalPaid"]);
                decimal remaining = requiredAmount - totalPaid;
                if (remaining < 0m) remaining = 0m;
           
                if (status.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
                {
                    row["PaymentStatusDisplay"] = "Refunded";
                    row["PaymentBalanceDisplay"] = totalPaid > 0m
                        ? "Refund issued: ₱ " + totalPaid.ToString("N2")
                        : "Refund completed";
                }
                else
                {
                    row["PaymentStatusDisplay"] =
                        (isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase) || remaining <= 0m)
                        ? "Paid"
                        : (string.IsNullOrWhiteSpace(pay) ? "Unpaid" : pay);

                    row["PaymentBalanceDisplay"] =
                        remaining > 0m
                        ? "Remaining: ₱ " + remaining.ToString("N2") + " upon check-in"
                        : "Fully settled";
                }

                int rentalCount = row["RentalCount"] == DBNull.Value ? 0 : Convert.ToInt32(row["RentalCount"]);
                decimal rentalAmount = row["RentalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["RentalAmount"]);

                int consumableQty = row["ConsumableQty"] == DBNull.Value ? 0 : Convert.ToInt32(row["ConsumableQty"]);
                decimal consumableAmount = row["ConsumableAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ConsumableAmount"]);

                List<string> extras = new List<string>();

                if (rentalCount > 0)
                    extras.Add("Rentals: " + rentalCount + " item(s) • ₱ " + rentalAmount.ToString("N2"));

                if (consumableQty > 0)
                    extras.Add("Consumables: " + consumableQty + " item(s) • ₱ " + consumableAmount.ToString("N2"));

                row["ExtrasDisplay"] = extras.Count > 0
                    ? string.Join("<br/>", extras)
                    : "No rentals or consumables added.";
            }
        }

        private bool CanRequestCancel(string status, string requestStatus)
        {
            string s = (status ?? "").Trim();
            string req = (requestStatus ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(req))
                return false;

            if (s.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                return false;

            if (s.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
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
            string court = HttpUtility.HtmlEncode("Court " + Convert.ToString(r["CourtNumber"]));
            string sport = HttpUtility.HtmlEncode(Convert.ToString(r["SportName"]));
            string status = Convert.ToString(r["ReservationStatusName"]);
            string statusEncoded = HttpUtility.HtmlEncode(status);
            string badgeClass = HttpUtility.HtmlEncode(StatusBadge(status));

            string friendlyDate = HttpUtility.HtmlEncode(Convert.ToString(r["FriendlyDate"]));
            string timeRange = HttpUtility.HtmlEncode(Convert.ToString(r["TimeRangeDisplay"]));
            string paidAmount = HttpUtility.HtmlEncode(Convert.ToString(r["PaidAmountDisplay"]));
            string balance = HttpUtility.HtmlEncode(Convert.ToString(r["PaymentBalanceDisplay"]));
            string extras = Convert.ToString(r["ExtrasDisplay"] ?? "");

            DateTime resDate = Convert.ToDateTime(r["ResDate"]);
            TimeSpan start = (TimeSpan)r["StartTime"];
            DateTime startDT = resDate.Date.Add(start);
            TimeSpan diff = startDT - DateTime.Now;

            string emphasis;
            if (diff.TotalSeconds <= 0)
                emphasis = "Ongoing / Passed";
            else if (diff.TotalDays >= 1)
                emphasis = ((int)Math.Floor(diff.TotalDays)) + " day(s) left";
            else
                emphasis = ((int)Math.Floor(diff.TotalHours)) + " hour(s) left";

            emphasis = HttpUtility.HtmlEncode(emphasis);

            return $@"
<div class='res-clean'>
    <div class='res-clean-top'>
        <div>
            <div class='res-main-title'>{court}</div>
            <div class='res-subline'>{sport}</div>

            <div class='res-meta-row'>
                <span class='pill-chip pill-date'>📅 {friendlyDate}</span>
                <span class='pill-chip pill-time'>🕒 {timeRange}</span>
                <span class='badge-status {badgeClass}'>{statusEncoded}</span>
                <span class='badge-status badge-approved'>{emphasis}</span>
            </div>
        </div>

        <div class='payment-box'>
            <div class='payment-label'>Paid so far</div>
            <div class='payment-paid'>{paidAmount}</div>
            <div class='payment-balance'>{balance}</div>
        </div>
    </div>

    <div class='extras-box'>
        <div class='extras-title'>Added items</div>
        <div class='extras-line'>{extras}</div>
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
            if (s == "refunded") return "badge-refunded";
            return "badge-request";
        }
        // -------------------- MODAL LISTS (CATEGORIES) --------------------
        private void BindReservationLists(DataTable dt)
        {
            DataView dvApproved = new DataView(dt);
            dvApproved.RowFilter = "ReservationStatusName = 'Approved' AND RequestStatus = ''";

            DataView dvPending = new DataView(dt);
            dvPending.RowFilter = "ReservationStatusName = 'Pending' AND RequestStatus = ''";

            DataView dvRequests = new DataView(dt);
            dvRequests.RowFilter = "RequestStatus = 'forRefund'";

            DataView dvCancelled = new DataView(dt);
            dvCancelled.RowFilter = "RequestStatus = 'forCancel'";

            DataView dvCompleted = new DataView(dt);
            dvCompleted.RowFilter = "ReservationStatusName = 'Completed'";

            DataView dvRefunded = new DataView(dt);
            dvRefunded.RowFilter = "ReservationStatusName = 'Refunded'";

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

            rptRefunded.DataSource = dvRefunded;
            rptRefunded.DataBind();

            pnlEmptyApproved.Visible = dvApproved.Count == 0;
            pnlEmptyPending.Visible = dvPending.Count == 0;
            pnlEmptyRequests.Visible = dvRequests.Count == 0;
            pnlEmptyCancelled.Visible = dvCancelled.Count == 0;
            pnlEmptyCompleted.Visible = dvCompleted.Count == 0;
            pnlEmptyRefunded.Visible = dvRefunded.Count == 0;
        }

        // -------------------- Repeater events --------------------
        protected void rptReservations_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            DataRowView row = (DataRowView)e.Item.DataItem;

            string status = Convert.ToString(row["ReservationStatusName"] ?? "");
            string req = Convert.ToString(row["RequestStatus"] ?? "");

            Button btnCancel = (Button)e.Item.FindControl("btnCancelReq");
            Label hint = (Label)e.Item.FindControl("lblRuleHint");

            bool canCancel = CanRequestCancel(status, req);

            if (btnCancel != null)
            {
                btnCancel.Enabled = canCancel;
                btnCancel.Visible = canCancel;
            }

            if (hint != null)
            {
                if (req.Equals("forCancel", StringComparison.OrdinalIgnoreCase))
                    hint.Text = "Your cancellation request is waiting for admin action.";
                else if (req.Equals("forRefund", StringComparison.OrdinalIgnoreCase))
                    hint.Text = "Your reservation is now queued for refund processing.";
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    hint.Text = "Completed reservations can no longer be cancelled.";
                else if (status.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
                    hint.Text = "This reservation has already been refunded.";
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
            string status;
            string req;

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT ReservationStatusName, ISNULL(RequestStatus,'') AS RequestStatus
FROM tblReservation
WHERE ReservationID = @RID
  AND UserID = @UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        ShowMsg("Reservation not found.", true);
                        return;
                    }

                    status = Convert.ToString(dr["ReservationStatusName"] ?? "");
                    req = Convert.ToString(dr["RequestStatus"] ?? "");
                }
            }

            if (!CanRequestCancel(status, req))
            {
                if (!string.IsNullOrWhiteSpace(req))
                    ShowMsg("This reservation already has an active request.", true);
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    ShowMsg("Completed reservations can no longer be cancelled.", true);
                else if (status.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
                    ShowMsg("This reservation has already been refunded.", true);
                else
                    ShowMsg("Unable to request cancellation for this reservation.", true);

                return;
            }

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE tblReservation
SET RequestStatus = 'forCancel'
WHERE ReservationID = @RID
  AND UserID = @UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            ShowMsg("Cancellation request submitted. Please wait for admin action.", false);
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

            string currentImgPath = null;

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand("SELECT ImgPath FROM tblPlayerAccount WHERE UserID = @ID", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();
                object result = cmd.ExecuteScalar();
                currentImgPath = result == DBNull.Value || result == null ? null : Convert.ToString(result);
            }

            string newImgPath = currentImgPath;

            if (fuAvatar != null && fuAvatar.HasFile)
            {
                try
                {
                    newImgPath = UploadImageToCloudinary(
                        fuAvatar,
                        currentImgPath,
                        "smash-it/profile-pictures",
                        "profile_" + userId
                    );
                }
                catch (Exception ex)
                {
                    lblEditMsg.Text = "<div class='alert alert-danger'>Image upload failed: " + Server.HtmlEncode(ex.Message) + "</div>";
                    return;
                }
            }

            string sql = @"
UPDATE tblPlayerAccount
SET PhoneNumber=@P,
    Username=@U,
    ImgPath=@IMG";

            if (!string.IsNullOrWhiteSpace(newPass)) sql += ", [Password]=@PW";
            sql += " WHERE UserID=@ID;";

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@P", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@U", username);
                cmd.Parameters.AddWithValue("@IMG", string.IsNullOrWhiteSpace(newImgPath) ? (object)DBNull.Value : newImgPath);
                cmd.Parameters.AddWithValue("@ID", userId);

                if (!string.IsNullOrWhiteSpace(newPass))
                    cmd.Parameters.AddWithValue("@PW", newPass);

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


        protected void rptPayments_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem)
                return;

            PaymentGroupVM group = (PaymentGroupVM)e.Item.DataItem;
            Repeater inner = (Repeater)e.Item.FindControl("rptPaymentsInner");

            if (inner != null)
            {
                inner.DataSource = group.Items;
                inner.DataBind();
            }
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
    CAST(p.Amount AS DECIMAL(10,2)) AS Amount,

    res.ResDate,
    res.StartTime AS ResStart,
    res.EndTime AS ResEnd,
    c.CourtNumber,

    rntl.RentalDate,
    rntl.ReturnedAt,
    rm.EquipmentType AS RentalEquipmentType,
    ISNULL(rm.EquipmentSpec,'') AS RentalEquipmentSpec,

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
   OR EXISTS
      (
          SELECT 1
          FROM tblReservation rr
          WHERE rr.ReservationID = p.ReservationID
            AND rr.UserID = @UID
      )
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

            DateTime today = DateTime.Today;
            DateTime weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Sunday);
            DateTime monthStart = new DateTime(today.Year, today.Month, 1);

            var grouped = new List<PaymentGroupVM>
    {
        new PaymentGroupVM
        {
            GroupTitle = "This Week",
            Items = list.Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= weekStart).ToList()
        },
        new PaymentGroupVM
        {
            GroupTitle = "This Month",
            Items = list.Where(x => x.PaymentDate.HasValue &&
                                    x.PaymentDate.Value.Date >= monthStart &&
                                    x.PaymentDate.Value.Date < weekStart).ToList()
        },
        new PaymentGroupVM
        {
            GroupTitle = "Older",
            Items = list.Where(x => !x.PaymentDate.HasValue || x.PaymentDate.Value.Date < monthStart).ToList()
        }
    };

            grouped = grouped.Where(g => g.Items.Count > 0).ToList();

            pnlPaymentsEmpty.Visible = (grouped.Count == 0);
            rptPayments.DataSource = grouped;
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
   OR EXISTS
      (
          SELECT 1
          FROM tblReservation rr
          WHERE rr.ReservationID = rntl.ReservationID
            AND rr.UserID = @UID
      )
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