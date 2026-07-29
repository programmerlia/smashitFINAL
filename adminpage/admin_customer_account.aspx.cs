using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Linq;
using Smash_IT.Security;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_customer_account : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        private string SortExpr
        {
            get { return ViewState["SortExpr"]?.ToString() ?? "CreatedAt"; }
            set { ViewState["SortExpr"] = value; }
        }

        private string SortDir
        {
            get { return ViewState["SortDir"]?.ToString() ?? "DESC"; }
            set { ViewState["SortDir"] = value; }
        }

        public class LeaderboardRow
        {
            public int RankNo { get; set; }
            public int UserID { get; set; }
            public string FullName { get; set; }
            public string Username { get; set; }
            public string ImgPath { get; set; }
            public decimal MetricValue { get; set; }
            public string MetricValueDisplay { get; set; }
            public bool IsPlaceholder { get; set; }
        }

        public class UserStats
        {
            public decimal TotalPaid { get; set; }
            public decimal PaidThisMonth { get; set; }
            public decimal PaidThisWeek { get; set; }
            public int ItemsRented { get; set; }
            public int ItemsBought { get; set; }
            public int Reservations { get; set; }
            public int QueueGames { get; set; }
            public int EventsJoined { get; set; }
            public int PaycCount { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadCustomers();
                LoadLeaderboard();
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            LoadCustomers();
        }

        protected void gvCustomers_Sorting(object sender, GridViewSortEventArgs e)
        {
            SortDir = (SortExpr == e.SortExpression && SortDir == "ASC") ? "DESC" : "ASC";
            SortExpr = e.SortExpression;
            LoadCustomers();
        }

        protected void ddlLeaderboardType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLeaderboard();
        }

        private void LoadCustomers()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string searchTerm = "%" + txtSearch.Text.Trim() + "%";

                string query = $@"
                    SELECT 
                        UserID,
                        Firstname + ' ' + Lastname AS FullName,
                        Email,
                        PhoneNumber,
                        Username,
                        CreatedAt,
                        ISNULL(NULLIF(ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath
                    FROM tblPlayerAccount
                    WHERE Firstname LIKE @Search
                       OR Lastname LIKE @Search
                       OR Email LIKE @Search
                       OR Username LIKE @Search
                    ORDER BY {SortExpr} {SortDir};

                    SELECT COUNT(*) FROM tblPlayerAccount;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Search", searchTerm);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);

                gvCustomers.DataSource = ds.Tables[0];
                gvCustomers.DataBind();

                lblTotalPlayers.Text = ds.Tables[1].Rows[0][0].ToString();
            }
        }

        private void LoadLeaderboard()
        {
            string leaderboardType = ddlLeaderboardType != null ? ddlLeaderboardType.SelectedValue : "reservations";
            DataTable dt = GetLeaderboardData(leaderboardType);

            int currentCount = dt.Rows.Count;
            for (int i = currentCount + 1; i <= 10; i++)
            {
                DataRow row = dt.NewRow();
                row["RankNo"] = i;
                row["UserID"] = 0;
                row["FullName"] = "Player " + i;
                row["Username"] = "placeholder" + i;
                row["ImgPath"] = "uploads/avatars/person.jpg";
                row["MetricValue"] = 0m;
                row["MetricValueDisplay"] = FormatMetricValue(leaderboardType, 0m);
                row["IsPlaceholder"] = true;
                dt.Rows.Add(row);
            }

            BindPodium(dt);
            gvLeaderboard.DataSource = dt;
            gvLeaderboard.DataBind();
        }

        private DataTable GetLeaderboardData(string leaderboardType)
        {
            DataTable dt = CreateLeaderboardTable();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "";

                switch (leaderboardType)
                {
                    case "reservations":
                        query = @"
                            SELECT TOP 10
                                p.UserID,
                                (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) AS FullName,
                                ISNULL(p.Username, '') AS Username,
                                ISNULL(NULLIF(p.ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath,
                                CAST(COUNT(r.ReservationID) AS DECIMAL(18,2)) AS MetricValue
                            FROM tblPlayerAccount p
                            LEFT JOIN tblReservation r ON p.UserID = r.UserID
                            GROUP BY p.UserID, p.Firstname, p.Lastname, p.Username, p.ImgPath
                            ORDER BY COUNT(r.ReservationID) DESC, (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) ASC";
                        break;

                    case "played":
                        query = @"
                            SELECT TOP 10
                                p.UserID,
                                (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) AS FullName,
                                ISNULL(p.Username, '') AS Username,
                                ISNULL(NULLIF(p.ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath,
                                CAST((ISNULL(q.QueueCount, 0) + ISNULL(payc.PaycCount, 0)) AS DECIMAL(18,2)) AS MetricValue
                            FROM tblPlayerAccount p
                            LEFT JOIN (
                                SELECT UserID, COUNT(*) AS QueueCount
                                FROM tblCourtQueue
                                WHERE QueueTypeName IN ('Queue', 'WalkIn')
                                GROUP BY UserID
                            ) q ON p.UserID = q.UserID
                            LEFT JOIN (
                                SELECT w.UserID, COUNT(*) AS PaycCount
                                FROM tblPlayAllYouCanRegistry r
                                INNER JOIN tblPlayerWalkIn w ON r.WalkInID = w.WalkInID
                                GROUP BY w.UserID
                            ) payc ON p.UserID = payc.UserID
                            ORDER BY (ISNULL(q.QueueCount, 0) + ISNULL(payc.PaycCount, 0)) DESC, (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) ASC";
                        break;

                    case "badminton":
                        query = @"
                            SELECT TOP 10
                                p.UserID,
                                (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) AS FullName,
                                ISNULL(p.Username, '') AS Username,
                                ISNULL(NULLIF(p.ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath,
                                CAST(COUNT(r.ReservationID) AS DECIMAL(18,2)) AS MetricValue
                            FROM tblPlayerAccount p
                            LEFT JOIN tblReservation r ON p.UserID = r.UserID
                                AND (
                                    ISNULL(r.SportName, '') = 'badminton'
                                )
                            GROUP BY p.UserID, p.Firstname, p.Lastname, p.Username, p.ImgPath
                            ORDER BY COUNT(r.ReservationID) DESC, (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) ASC";
                        break;

                    case "pickleball":
                        query = @"
                            SELECT TOP 10
                                p.UserID,
                                (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) AS FullName,
                                ISNULL(p.Username, '') AS Username,
                                ISNULL(NULLIF(p.ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath,
                                CAST(COUNT(r.ReservationID) AS DECIMAL(18,2)) AS MetricValue
                            FROM tblPlayerAccount p
                            LEFT JOIN tblReservation r ON p.UserID = r.UserID
                                AND (
                                    ISNULL(r.SportName, '') = 'pickleball'
                                 
                                )
                            GROUP BY p.UserID, p.Firstname, p.Lastname, p.Username, p.ImgPath
                            ORDER BY COUNT(r.ReservationID) DESC, (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) ASC";
                        break;

                    case "paid":
                    default:
                        query = @"
                            SELECT TOP 10
                                p.UserID,
                                (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) AS FullName,
                                ISNULL(p.Username, '') AS Username,
                                ISNULL(NULLIF(p.ImgPath, ''), 'uploads/avatars/person.jpg') AS ImgPath,
                                CAST(ISNULL(SUM(py.Amount), 0) AS DECIMAL(18,2)) AS MetricValue
                            FROM tblPlayerAccount p
                            LEFT JOIN tblPayment py ON p.UserID = py.UserID
                            GROUP BY p.UserID, p.Firstname, p.Lastname, p.Username, p.ImgPath
                            ORDER BY ISNULL(SUM(py.Amount), 0) DESC, (ISNULL(p.Firstname, '') + ' ' + ISNULL(p.Lastname, '')) ASC";
                        break;
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    int rank = 1;
                    while (dr.Read())
                    {
                        DataRow row = dt.NewRow();
                        decimal metric = dr["MetricValue"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["MetricValue"]);

                        row["RankNo"] = rank;
                        row["UserID"] = dr["UserID"] == DBNull.Value ? 0 : Convert.ToInt32(dr["UserID"]);
                        row["FullName"] = (dr["FullName"] == DBNull.Value ? "" : dr["FullName"].ToString()).Trim();
                        row["Username"] = dr["Username"] == DBNull.Value ? "" : dr["Username"].ToString();
                        row["ImgPath"] = dr["ImgPath"] == DBNull.Value ? "uploads/avatars/person.jpg" : dr["ImgPath"].ToString();
                        row["MetricValue"] = metric;
                        row["MetricValueDisplay"] = FormatMetricValue(leaderboardType, metric);
                        row["IsPlaceholder"] = false;

                        if (string.IsNullOrWhiteSpace(row["FullName"].ToString()))
                            row["FullName"] = "Player " + rank;

                        dt.Rows.Add(row);
                        rank++;
                    }
                }
            }

            return dt;
        }

        private DataTable CreateLeaderboardTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("RankNo", typeof(int));
            dt.Columns.Add("UserID", typeof(int));
            dt.Columns.Add("FullName", typeof(string));
            dt.Columns.Add("Username", typeof(string));
            dt.Columns.Add("ImgPath", typeof(string));
            dt.Columns.Add("MetricValue", typeof(decimal));
            dt.Columns.Add("MetricValueDisplay", typeof(string));
            dt.Columns.Add("IsPlaceholder", typeof(bool));
            return dt;
        }

        private string FormatMetricValue(string type, decimal value)
        {
            switch (type)
            {
                case "paid":
                    return "₱" + value.ToString("#,##0.00");
                case "reservations":
                    return ((int)value).ToString("#,##0") + " reservations";
                case "played":
                    return ((int)value).ToString("#,##0") + " played";
                case "badminton":
                    return ((int)value).ToString("#,##0") + " badminton reserves";
                case "pickleball":
                    return ((int)value).ToString("#,##0") + " pickleball reserves";
                default:
                    return value.ToString("#,##0.##");
            }
        }

        private void BindPodium(DataTable dt)
        {
            BindPodiumSlot(dt, 0, imgRank1, lnkRank1, lblRank1User, lblRank1Score);
            BindPodiumSlot(dt, 1, imgRank2, lnkRank2, lblRank2User, lblRank2Score);
            BindPodiumSlot(dt, 2, imgRank3, lnkRank3, lblRank3User, lblRank3Score);
        }

        private void BindPodiumSlot(DataTable dt, int index, Image img, HtmlAnchor link, HtmlGenericControl userCtrl, HtmlGenericControl scoreCtrl)
        {
            string leaderboardType = ddlLeaderboardType != null ? ddlLeaderboardType.SelectedValue : "reservations";

            if (dt.Rows.Count <= index)
            {
                img.ImageUrl = ResolveUrl("~/uploads/avatars/person.jpg");
                link.InnerText = "Player " + (index + 1);
                link.HRef = "javascript:void(0);";
                userCtrl.InnerText = "@placeholder" + (index + 1);
                scoreCtrl.InnerText = FormatMetricValue(leaderboardType, 0m);
                return;
            }

            DataRow row = dt.Rows[index];
            int userId = Convert.ToInt32(row["UserID"]);
            string fullName = row["FullName"].ToString();
            string username = row["Username"].ToString();
            string imgPath = row["ImgPath"].ToString();
            string score = row["MetricValueDisplay"].ToString();
            bool isPlaceholder = Convert.ToBoolean(row["IsPlaceholder"]);

            img.ImageUrl = ResolveUrl("~/" + (string.IsNullOrWhiteSpace(imgPath) ? "uploads/avatars/person.jpg" : imgPath));
            link.InnerText = fullName;
            userCtrl.InnerText = string.IsNullOrWhiteSpace(username) ? "@no_username" : "@" + username.TrimStart('@');
            scoreCtrl.InnerText = score;

            if (isPlaceholder || userId <= 0)
            {
                link.HRef = "javascript:void(0);";
            }
            else
            {
                string safeName = fullName.Replace("\\", "\\\\").Replace("'", "\\'");
                link.HRef = "javascript:showDetails('" + userId + "', '" + safeName + "')";
            }
        }

        private void ShowMessage(string message, bool isError = false)
        {
            lblMsg.Text = message;
            lblMsg.CssClass = isError ? "status-msg msg-error" : "status-msg msg-success";
            string cleanMessage = message.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"alert('{cleanMessage}');", true);
        }

        protected void btnShowAdd_Click(object sender, EventArgs e)
        {
            ClearFields();
            pnlInput.Visible = true;
            lblMsg.Text = "";
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string phone = txtPhone.Text.Trim();
                if (!string.IsNullOrEmpty(phone))
                {
                    if (phone.Length != 11 || !phone.All(char.IsDigit))
                    {
                        ShowMessage("Phone number must be exactly 11 digits (e.g., 09123456789).", true);
                        return;
                    }
                }

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    bool isNew = string.IsNullOrEmpty(hfUserID.Value);
                    bool hasNewImage = fileAvatar.HasFile;
                    string imgPath = "uploads/avatars/person.jpg";

                    if (hasNewImage)
                    {
                        string ext = Path.GetExtension(fileAvatar.FileName).ToLower();
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                        if (allowedExtensions.Contains(ext))
                        {
                            string filename = Guid.NewGuid().ToString() + ext;
                            string folderPath = Server.MapPath("~/uploads/avatars/");

                            if (!Directory.Exists(folderPath))
                                Directory.CreateDirectory(folderPath);

                            fileAvatar.SaveAs(Path.Combine(folderPath, filename));
                            imgPath = "uploads/avatars/" + filename;
                        }
                        else
                        {
                            ShowMessage("Invalid image format. Only JPG, PNG, and GIF are allowed.", true);
                            return;
                        }
                    }

                    string query = "";
                    if (isNew)
                    {
                        query = "INSERT INTO tblPlayerAccount (Firstname, Lastname, Email, PhoneNumber, Username, [Password], ImgPath) VALUES (@F, @L, @Email, @Phone, @User, @Pass, @ImgPath)";
                    }
                    else
                    {
                        if (hasNewImage)
                            query = "UPDATE tblPlayerAccount SET Firstname=@F, Lastname=@L, Email=@Email, PhoneNumber=@Phone, Username=@User, ImgPath=@ImgPath WHERE UserID=@ID";
                        else
                            query = "UPDATE tblPlayerAccount SET Firstname=@F, Lastname=@L, Email=@Email, PhoneNumber=@Phone, Username=@User WHERE UserID=@ID";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@F", txtFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@L", txtLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(txtEmail.Text.Trim()) ? (object)DBNull.Value : txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@Phone", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone);
                    cmd.Parameters.AddWithValue("@User", string.IsNullOrEmpty(txtUsername.Text.Trim()) ? (object)DBNull.Value : txtUsername.Text.Trim());

                    if (isNew)
                        cmd.Parameters.AddWithValue("@Pass", PasswordHasher.Hash(txtPassword.Text.Trim()));
                    else
                        cmd.Parameters.AddWithValue("@ID", hfUserID.Value);

                    if (isNew || hasNewImage)
                        cmd.Parameters.AddWithValue("@ImgPath", imgPath);

                    cmd.ExecuteNonQuery();

                    pnlInput.Visible = false;
                    ClearFields();
                    LoadCustomers();
                    LoadLeaderboard();
                    ShowMessage(isNew ? "Customer added successfully!" : "Customer updated successfully!");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                    ShowMessage("That Email or Username is already taken by another account.", true);
                else
                    ShowMessage("Database Error: " + sqlEx.Message, true);
            }
            catch (Exception ex)
            {
                ShowMessage("System Error: " + ex.Message, true);
            }
        }

        protected void gvCustomers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditCustomer" || e.CommandName == "DeleteCustomer")
            {
                int id;
                if (int.TryParse(e.CommandArgument.ToString(), out id))
                {
                    if (e.CommandName == "EditCustomer")
                    {
                        hfUserID.Value = id.ToString();
                        LoadForEdit(id);
                        pnlInput.Visible = true;
                        lblMsg.Text = "Editing Customer ID: " + id;
                    }
                    else if (e.CommandName == "DeleteCustomer")
                    {
                        DeleteCustomer(id);
                    }
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tblPlayerAccount WHERE UserID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    txtFirstName.Text = dr["Firstname"].ToString();
                    txtLastName.Text = dr["Lastname"].ToString();
                    txtEmail.Text = dr["Email"].ToString();
                    txtPhone.Text = dr["PhoneNumber"].ToString();
                    txtUsername.Text = dr["Username"].ToString();

                    string path = dr["ImgPath"].ToString();
                    imgPreview.ImageUrl = "~/" + path;
                    imgPreview.Visible = true;
                }
            }
        }

        private void DeleteCustomer(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM tblPlayerAccount WHERE UserID=@ID", conn);
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    LoadCustomers();
                    LoadLeaderboard();
                    ShowMessage("Player account deleted successfully.");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 547)
                    ShowMessage("Cannot delete this customer. They have active reservations or queue history.", true);
                else
                    ShowMessage("Database error during deletion: " + sqlEx.Message, true);
            }
            catch (Exception ex)
            {
                ShowMessage("System error during deletion: " + ex.Message, true);
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlInput.Visible = false;
            ClearFields();
            lblMsg.Text = "";
        }

        private void ClearFields()
        {
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtUsername.Text = "";
            txtPassword.Text = "";
            hfUserID.Value = "";
            imgPreview.Visible = false;
            fileAvatar.Attributes.Clear();
        }

        [WebMethod]
        public static UserStats GetPlayerStats(string userId)
        {
            string dbConn = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;
            UserStats stats = new UserStats();

            if (string.IsNullOrEmpty(userId))
                return stats;

            using (SqlConnection conn = new SqlConnection(dbConn))
            {
                string query = @"
                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment WHERE UserID = @UID;

                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment
                    WHERE UserID = @UID AND MONTH(PaymentDate) = MONTH(GETDATE()) AND YEAR(PaymentDate) = YEAR(GETDATE());

                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment
                    WHERE UserID = @UID AND PaymentDate >= CAST(DATEADD(day, -7, GETDATE()) AS DATE);

                    SELECT COUNT(*) FROM tblRental WHERE UserID = @UID;

                    SELECT ISNULL(SUM(Quantity), 0) FROM tblConsumable WHERE UserID = @UID;

                    SELECT COUNT(*) FROM tblReservation WHERE UserID = @UID;

                    SELECT COUNT(*) FROM tblCourtQueue WHERE UserID = @UID AND QueueTypeName IN ('Queue', 'WalkIn');

                    SELECT COUNT(*) FROM tblEventParticipant WHERE UserID = @UID;

                    SELECT COUNT(*) FROM tblPlayAllYouCanRegistry r
                    JOIN tblPlayerWalkIn w ON r.WalkInID = w.WalkInID
                    WHERE w.UserID = @UID;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read() && !dr.IsDBNull(0)) stats.TotalPaid = Convert.ToDecimal(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.PaidThisMonth = Convert.ToDecimal(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.PaidThisWeek = Convert.ToDecimal(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.ItemsRented = Convert.ToInt32(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.ItemsBought = Convert.ToInt32(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.Reservations = Convert.ToInt32(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.QueueGames = Convert.ToInt32(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.EventsJoined = Convert.ToInt32(dr[0]);
                    if (dr.NextResult() && dr.Read() && !dr.IsDBNull(0)) stats.PaycCount = Convert.ToInt32(dr[0]);
                }
            }

            return stats;
        }
    }
}
