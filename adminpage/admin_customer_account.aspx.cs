using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_customer_account : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        // ViewState properties to persist sort state across postbacks
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
            LoadCustomers(); // Filters based on txtSearch.Text
        }

        protected void gvCustomers_Sorting(object sender, GridViewSortEventArgs e)
        {
            // If clicking the same column, toggle direction. Otherwise, default to ASC.
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
            SELECT UserID, FullName, Email, PhoneNumber, Username, CreatedAt 
            FROM tblPlayerAccount 
            WHERE FullName LIKE @Search OR Email LIKE @Search OR Username LIKE @Search
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

        // --- CRUD Methods ---

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
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    bool isNew = string.IsNullOrEmpty(hfUserID.Value);
                    string query = isNew
                        ? "INSERT INTO tblPlayerAccount (FullName, Email, PhoneNumber, Username, [Password]) VALUES (@Name, @Email, @Phone, @User, @Pass)"
                        : "UPDATE tblPlayerAccount SET FullName=@Name, Email=@Email, PhoneNumber=@Phone, Username=@User WHERE UserID=@ID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Name", txtFullName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", txtEmail.Text.Trim());
                    cmd.Parameters.AddWithValue("@Phone", txtPhone.Text.Trim());
                    cmd.Parameters.AddWithValue("@User", txtUsername.Text.Trim());
                    if (isNew) cmd.Parameters.AddWithValue("@Pass", txtPassword.Text.Trim());
                    else cmd.Parameters.AddWithValue("@ID", hfUserID.Value);

                    cmd.ExecuteNonQuery();
                    pnlInput.Visible = false;
                    LoadCustomers();
                    ShowMessage(isNew ? "Customer added!" : "Customer updated!");
                }
            }
            catch (Exception ex) { ShowMessage(ex.Message, true); }
        }

        protected void gvCustomers_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            // 1. Check if the CommandName is one of our CRUD actions
            // This prevents the code from running when you click Sort Headers
            if (e.CommandName == "EditCustomer" || e.CommandName == "DeleteCustomer")
            {
                int id;
                // 2. Safely try to parse the ID
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
                else
                {
                    // Optional: Handle cases where ID isn't a number
                    ShowMessage("Invalid ID format.", true);
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
                    txtFullName.Text = dr["FullName"].ToString();
                    txtEmail.Text = dr["Email"].ToString();
                    txtPhone.Text = dr["PhoneNumber"].ToString();
                    txtUsername.Text = dr["Username"].ToString();
                }
            }
        }

        private void DeleteCustomer(int id)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand("DELETE FROM tblPlayerAccount WHERE UserID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
                LoadCustomers();
                ShowMessage("Player deleted.");
            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlInput.Visible = false;
            ClearFields();
        }

        private void ClearFields()
        {
            txtFullName.Text = txtEmail.Text = txtPhone.Text = txtUsername.Text = txtPassword.Text = "";
            hfUserID.Value = "";
        }
    }
}