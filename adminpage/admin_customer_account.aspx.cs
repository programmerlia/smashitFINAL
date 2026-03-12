using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;

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

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadCustomers();
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
                    ImgPath 
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

                            fileAvatar.SaveAs(folderPath + filename);
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
                        cmd.Parameters.AddWithValue("@Pass", txtPassword.Text.Trim());
                    else
                        cmd.Parameters.AddWithValue("@ID", hfUserID.Value);

                    if (isNew || hasNewImage)
                        cmd.Parameters.AddWithValue("@ImgPath", imgPath);

                    cmd.ExecuteNonQuery();

                    pnlInput.Visible = false;
                    ClearFields();
                    LoadCustomers();
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
            txtFirstName.Text = txtLastName.Text = txtEmail.Text = txtPhone.Text = txtUsername.Text = txtPassword.Text = "";
            hfUserID.Value = "";
            imgPreview.Visible = false;
            fileAvatar.Attributes.Clear();
        }

        // ==========================================
        // UPGRADED PLAYER STATISTICS AJAX METHD
        // ==========================================
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

        [WebMethod]
        public static UserStats GetPlayerStats(string userId)
        {
            string dbConn = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;
            UserStats stats = new UserStats();

            if (string.IsNullOrEmpty(userId)) return stats;

            using (SqlConnection conn = new SqlConnection(dbConn))
            {
                // A massive, efficient 9-step query batch that executes instantly
                string query = @"
                    -- 1. All Time Spent
                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment WHERE UserID = @UID;
                    
                    -- 2. Spent This Month
                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment 
                    WHERE UserID = @UID AND MONTH(PaymentDate) = MONTH(GETDATE()) AND YEAR(PaymentDate) = YEAR(GETDATE());
                    
                    -- 3. Spent This Week (Rolling 7 Days)
                    SELECT ISNULL(SUM(Amount), 0) FROM tblPayment 
                    WHERE UserID = @UID AND PaymentDate >= CAST(DATEADD(day, -7, GETDATE()) AS DATE);
                    
                    -- 4. Total Items Rented
                    SELECT COUNT(*) FROM tblRental WHERE UserID = @UID;
                    
                    -- 5. Total Items Bought (Consumables via Quantity)
                    SELECT ISNULL(SUM(Quantity), 0) FROM tblConsumable WHERE UserID = @UID;
                    
                    -- 6. Total Reservations
                    SELECT COUNT(*) FROM tblReservation WHERE UserID = @UID;
                    
                    -- 7. Queue Games (Queue + WalkIn Types)
                    SELECT COUNT(*) FROM tblCourtQueue WHERE UserID = @UID AND QueueTypeName IN ('Queue', 'WalkIn');
                    
                    -- 8. Events Joined
                    SELECT COUNT(*) FROM tblEventParticipant WHERE UserID = @UID;
                    
                    -- 9. PAYC Sessions
                    SELECT COUNT(*) FROM tblPlayAllYouCanRegistry r 
                    JOIN tblPlayerWalkIn w ON r.WalkInID = w.WalkInID WHERE w.UserID = @UID;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UID", userId);
                conn.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    // Safe execution to catch all 9 result sets without crashing
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