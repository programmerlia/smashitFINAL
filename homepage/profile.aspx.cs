using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;

namespace Smash_IT.homepage
{
    public partial class profile : System.Web.UI.Page
    {
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
                LoadProfile(userId);
                LoadStats(userId);
                LoadReservations(userId);

                lblMsg.Text = "";
                lblEditMsg.Text = "";
            }
        }

        // -------------------- PROFILE --------------------
        private void LoadProfile(int userId)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
SELECT FullName, Email, PhoneNumber, Username, CreatedAt, ImgPath
FROM tblPlayerAccount
WHERE UserID = @ID;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;

                    string fullName = Convert.ToString(dr["FullName"]);
                    string email = Convert.ToString(dr["Email"]);
                    string phone = Convert.ToString(dr["PhoneNumber"]);
                    string username = Convert.ToString(dr["Username"]);
                    DateTime created = dr["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["CreatedAt"]);
                    string imgPath = Convert.ToString(dr["ImgPath"]);

                    lblFullName.Text = fullName;
                    lblFullName2.Text = fullName;
                    lblEmail.Text = email;
                    lblPhone.Text = string.IsNullOrWhiteSpace(phone) ? "—" : phone;
                    lblUsername.Text = username;
                    lblCreatedAt.Text = created == DateTime.MinValue ? "—" : created.ToString("yyyy-MM-dd");

                    // Resolve image path (DB example: "images/person.png")
                    imgAvatar.ImageUrl = ResolveImg(imgPath);
                    imgAvatar.AlternateText = "Profile";

                    // Pre-fill edit inputs
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

        // -------------------- STATS / ANALYTICS --------------------
        private void LoadStats(int userId)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
SELECT
  (SELECT COUNT(*) FROM tblReservation WHERE UserID=@ID) AS TotalBookings,
  (SELECT COUNT(*) FROM tblReservation WHERE UserID=@ID AND Status='Pending') AS PendingCount,
  (SELECT ISNULL(SUM(DATEDIFF(MINUTE, StartTime, EndTime)),0)
   FROM tblReservation
   WHERE UserID=@ID AND Status IN ('Approved','Completed')) AS MinutesPlayed;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return;

                    int total = Convert.ToInt32(dr["TotalBookings"]);
                    int pending = Convert.ToInt32(dr["PendingCount"]);
                    int minutes = Convert.ToInt32(dr["MinutesPlayed"]);

                    lblTotalBookings.Text = total.ToString();
                    lblPendingCount.Text = pending.ToString();

                    // show hours (rounded)
                    double hours = minutes / 60.0;
                    lblHoursPlayed.Text = hours.ToString("0.#", CultureInfo.InvariantCulture);
                }
            }
        }

        // -------------------- RESERVATIONS --------------------
        private void LoadReservations(int userId)
        {
            DataTable dt = new DataTable();

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
SELECT TOP 50
  r.ReservationID,
  r.ResDate,
  r.StartTime,
  r.EndTime,
  r.Status,
  r.RequestStatus,
  r.IsPaid,
  r.PaymentStatus,
  c.CourtNumber,
  c.Sport
FROM tblReservation r
JOIN tblCourt c ON c.CourtID = r.CourtID
WHERE r.UserID = @ID
ORDER BY r.ResDate DESC, r.StartTime DESC;", con))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                con.Open();

                using (var da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }

            // add computed columns for UI
            dt.Columns.Add("StartStr", typeof(string));
            dt.Columns.Add("EndStr", typeof(string));
            dt.Columns.Add("StatusBadgeClass", typeof(string));
            dt.Columns.Add("PaymentStatusDisplay", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                var st = (TimeSpan)row["StartTime"];
                var et = (TimeSpan)row["EndTime"];

                row["StartStr"] = DateTime.Today.Add(st).ToString("hh:mm tt");
                row["EndStr"] = DateTime.Today.Add(et).ToString("hh:mm tt");

                string status = Convert.ToString(row["Status"] ?? "");
                row["StatusBadgeClass"] = StatusBadge(status);

                bool isPaid = row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]);
                string pay = Convert.ToString(row["PaymentStatus"] ?? "");
                row["PaymentStatusDisplay"] = (isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase))
                    ? "Paid"
                    : (string.IsNullOrWhiteSpace(pay) ? "Unpaid" : pay);
            }

            // top 1 pending display
            RenderTopPending1(dt);

            // full list
            rptReservations.DataSource = dt;
            rptReservations.DataBind();
        }

        private void RenderTopPending1(DataTable all)
        {
            pnlPendingTop1.Controls.Clear();

            DataRow pending = null;
            foreach (DataRow r in all.Rows)
            {
                string status = Convert.ToString(r["Status"]);
                if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    pending = r;
                    break;
                }
            }

            if (pending == null)
            {
                pnlPendingTop1.Controls.Add(new Literal
                {
                    Text = "<div class='muted'>No pending reservations right now.</div>"
                });
                return;
            }

            int rid = Convert.ToInt32(pending["ReservationID"]);
            string date = Convert.ToDateTime(pending["ResDate"]).ToString("yyyy-MM-dd");
            string start = Convert.ToString(pending["StartStr"]);
            string end = Convert.ToString(pending["EndStr"]);
            string court = Convert.ToString(pending["CourtNumber"]);
            string sport = Convert.ToString(pending["Sport"]);

            pnlPendingTop1.Controls.Add(new Literal
            {
                Text = $@"
<div class='res-item'>
  <div class='res-top'>
    <div>
      <div style='font-weight:800;'>Pending Reservation #{rid} • Court {court} • {sport}</div>
      <div class='muted'>{date} • {start} - {end}</div>
      <div class='mt-1'><span class='badge-status badge-pending'>Pending</span></div>
    </div>
  </div>
</div>"
            });
        }

        private string StatusBadge(string status)
        {
            switch ((status ?? "").Trim().ToLowerInvariant())
            {
                case "pending": return "badge-pending";
                case "approved": return "badge-approved";
                case "cancelled": return "badge-cancelled";
                case "completed": return "badge-completed";
                default: return "badge-request";
            }
        }

        // enforce: cancel only if ResDate >= Today+1
        private bool CanRequestCancel(DateTime resDate, string status)
        {
            if (!(status.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                  status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
                return false;

            return resDate.Date >= DateTime.Today.AddDays(1);
        }

        private bool CanRequestRefund(DateTime resDate, string status, bool isPaidOrPaidStatus)
        {
            // You can adjust this rule.
            // Here: refund request only if paid AND reservation not already completed/cancelled.
            if (!isPaidOrPaidStatus) return false;
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                return false;

            // allow refund even on same day? change if you want:
            return resDate.Date >= DateTime.Today;
        }

        protected void rptReservations_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var row = (DataRowView)e.Item.DataItem;

            int rid = Convert.ToInt32(row["ReservationID"]);
            DateTime resDate = Convert.ToDateTime(row["ResDate"]);
            string status = Convert.ToString(row["Status"] ?? "");
            string req = Convert.ToString(row["RequestStatus"] ?? "");

            bool isPaid = row["IsPaid"] != DBNull.Value && Convert.ToBoolean(row["IsPaid"]);
            string pay = Convert.ToString(row["PaymentStatus"] ?? "");
            bool paid = isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase);

            var btnCancel = (Button)e.Item.FindControl("btnCancelReq");
            var btnRefund = (Button)e.Item.FindControl("btnRefundReq");
            var hint = (Label)e.Item.FindControl("lblRuleHint");

            bool alreadyRequested = !string.IsNullOrWhiteSpace(req);

            bool canCancel = !alreadyRequested && CanRequestCancel(resDate, status);
            bool canRefund = !alreadyRequested && CanRequestRefund(resDate, status, paid);

            btnCancel.Enabled = canCancel;
            btnRefund.Enabled = canRefund;

            if (alreadyRequested)
            {
                hint.Text = "You already have a request on this reservation. Please wait for staff/admin response.";
            }
            else if (!canCancel && (status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
            {
                hint.Text = "Cancel request is allowed only at least 1 day before the reservation date.";
            }
            else
            {
                hint.Text = "";
            }
        }

        protected void rptReservations_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int userId = Convert.ToInt32(Session["UserID"]);
            int reservationId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "cancel")
            {
                HandleCancelRequest(userId, reservationId);
            }
            else if (e.CommandName == "refund")
            {
                HandleRefundRequest(userId, reservationId);
            }

            // refresh everything
            LoadProfile(userId);
            LoadStats(userId);
            LoadReservations(userId);
        }

        private void HandleCancelRequest(int userId, int reservationId)
        {
            // Re-check in DB (server-side enforcement)
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
SELECT ResDate, Status, ISNULL(RequestStatus,'') AS RequestStatus
FROM tblReservation
WHERE ReservationID=@RID AND UserID=@UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        ShowMsg("Reservation not found.", isError: true);
                        return;
                    }

                    DateTime resDate = Convert.ToDateTime(dr["ResDate"]);
                    string status = Convert.ToString(dr["Status"] ?? "");
                    string req = Convert.ToString(dr["RequestStatus"] ?? "");

                    if (!string.IsNullOrWhiteSpace(req))
                    {
                        ShowMsg("You already submitted a request for this reservation.", true);
                        return;
                    }

                    if (!CanRequestCancel(resDate, status))
                    {
                        ShowMsg("Cancel request not allowed. You can only cancel at least 1 day prior to the reservation.", true);
                        return;
                    }
                }
            }

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
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

        private void HandleRefundRequest(int userId, int reservationId)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
SELECT ResDate, Status, ISNULL(RequestStatus,'') AS RequestStatus, IsPaid, ISNULL(PaymentStatus,'') AS PaymentStatus
FROM tblReservation
WHERE ReservationID=@RID AND UserID=@UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();

                DateTime resDate;
                string status;
                string req;
                bool isPaid;
                string pay;

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        ShowMsg("Reservation not found.", true);
                        return;
                    }

                    resDate = Convert.ToDateTime(dr["ResDate"]);
                    status = Convert.ToString(dr["Status"] ?? "");
                    req = Convert.ToString(dr["RequestStatus"] ?? "");
                    isPaid = dr["IsPaid"] != DBNull.Value && Convert.ToBoolean(dr["IsPaid"]);
                    pay = Convert.ToString(dr["PaymentStatus"] ?? "");
                }

                if (!string.IsNullOrWhiteSpace(req))
                {
                    ShowMsg("You already submitted a request for this reservation.", true);
                    return;
                }

                bool paid = isPaid || pay.Equals("paid", StringComparison.OrdinalIgnoreCase);
                if (!CanRequestRefund(resDate, status, paid))
                {
                    ShowMsg("Refund request not allowed for this reservation (must be paid and not completed/cancelled).", true);
                    return;
                }
            }

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
UPDATE tblReservation
SET RequestStatus='RefundRequest'
WHERE ReservationID=@RID AND UserID=@UID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@UID", userId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            ShowMsg("Refund request sent. Please wait for admin approval.", false);
        }

        private void ShowMsg(string text, bool isError)
        {
            string css = isError ? "alert alert-danger" : "alert alert-success";
            lblMsg.Text = $"<div class='{css}' role='alert'>{HttpUtility.HtmlEncode(text)}</div>";
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

            // username uniqueness check
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
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

            // handle avatar upload (optional)
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

                string fileName = $"u{userId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                string absPath = Path.Combine(folderAbs, fileName);

                fuAvatar.SaveAs(absPath);

                // store like: "uploads/avatars/u1_20260225123456.png"
                newImgPath = $"{folderRel}/{fileName}";
            }

            // update DB
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
UPDATE tblPlayerAccount
SET PhoneNumber=@P,
    Username=@U
    " + (!string.IsNullOrWhiteSpace(newPass) ? ", [Password]=@PW" : "") + @"
    " + (newImgPath != null ? ", ImgPath=@IMG" : "") + @"
WHERE UserID=@ID;", con))
            {
                cmd.Parameters.AddWithValue("@P", (object)phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@U", username);
                cmd.Parameters.AddWithValue("@ID", userId);

                if (!string.IsNullOrWhiteSpace(newPass))
                    cmd.Parameters.AddWithValue("@PW", newPass); // NOTE: plaintext (matches your current schema)

                if (newImgPath != null)
                    cmd.Parameters.AddWithValue("@IMG", newImgPath);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            lblEditMsg.Text = "<div class='alert alert-success'>Profile updated successfully.</div>";

            // refresh labels
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
    }
}