using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_staff : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        // ViewState properties to keep track of sorting
        private string SortExpr
        {
            get { return ViewState["SortExpr"]?.ToString() ?? "StaffID"; }
            set { ViewState["SortExpr"] = value; }
        }

        private string SortDir
        {
            get { return ViewState["SortDir"]?.ToString() ?? "ASC"; }
            set { ViewState["SortDir"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadStaff();
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            LoadStaff();
        }

        protected void gvStaff_Sorting(object sender, GridViewSortEventArgs e)
        {
            SortDir = (SortExpr == e.SortExpression && SortDir == "ASC") ? "DESC" : "ASC";
            SortExpr = e.SortExpression;
            LoadStaff();
        }

        private void LoadStaff()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                // Note: Ensure your txtSearch control exists on the .aspx page
                string searchTerm = "%" + (txtSearch != null ? txtSearch.Text.Trim() : "") + "%";

                string query = $@"
                    SELECT StaffID, FullName, StaffRole, Username 
                    FROM tblStaffAccount 
                    WHERE FullName LIKE @Search OR Username LIKE @Search OR StaffRole LIKE @Search
                    ORDER BY {SortExpr} {SortDir};

                    SELECT COUNT(*) FROM tblStaffAccount;";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Search", searchTerm);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);

                gvStaff.DataSource = ds.Tables[0];
                gvStaff.DataBind();

                // If you added a lblTotalStaff stat card to match customers:
                if (lblTotalStaff != null)
                    lblTotalStaff.Text = ds.Tables[1].Rows[0][0].ToString();
            }
        }

        private void ShowMessage(string message, bool isError = false)
        {
            lblMsg.Text = message;
            lblMsg.CssClass = isError ? "status-msg msg-error" : "status-msg msg-success";
            string cleanMessage = message.Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(this, GetType(), "alert", $"alert('{cleanMessage}');", true);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string staffId = hfStaffID.Value;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Duplicate Username Check (Copied from Customer logic)
                    string checkQuery = "SELECT COUNT(*) FROM tblStaffAccount WHERE Username = @User";
                    if (!string.IsNullOrEmpty(staffId)) checkQuery += " AND StaffID != @ID";

                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@User", username);
                    if (!string.IsNullOrEmpty(staffId)) checkCmd.Parameters.AddWithValue("@ID", staffId);

                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        ShowMessage("Username already taken by another staff member.", true);
                        return;
                    }

                    bool isNew = string.IsNullOrEmpty(staffId);
                    string query = isNew
                        ? "INSERT INTO tblStaffAccount (FullName, StaffRole, Username, [Password]) VALUES (@Name, @Role, @User, @Pass)"
                        : "UPDATE tblStaffAccount SET FullName=@Name, StaffRole=@Role, Username=@User WHERE StaffID=@ID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Name", txtFullName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Role", ddlRole.SelectedValue);
                    cmd.Parameters.AddWithValue("@User", username);
                    if (isNew) cmd.Parameters.AddWithValue("@Pass", txtPassword.Text.Trim());
                    else cmd.Parameters.AddWithValue("@ID", staffId);

                    cmd.ExecuteNonQuery();
                    pnlInput.Visible = false;
                    LoadStaff();
                    ShowMessage(isNew ? "Staff added successfully!" : "Staff updated successfully!");
                }
            }
            catch (Exception ex) { ShowMessage("Error: " + ex.Message, true); }
        }

        protected void gvStaff_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "EditStaff" || e.CommandName == "DeleteStaff")
            {
                int id = Convert.ToInt32(e.CommandArgument);
                if (e.CommandName == "EditStaff")
                {
                    hfStaffID.Value = id.ToString();
                    LoadForEdit(id);
                    pnlInput.Visible = true;
                }
                else if (e.CommandName == "DeleteStaff")
                {
                    DeleteStaff(id);
                }
            }
        }

        private void LoadForEdit(int id)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tblStaffAccount WHERE StaffID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    txtFullName.Text = dr["FullName"].ToString();
                    ddlRole.SelectedValue = dr["StaffRole"].ToString();
                    txtUsername.Text = dr["Username"].ToString();
                }
            }
        }

        private void DeleteStaff(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM tblStaffAccount WHERE StaffID=@ID", conn);
                    cmd.Parameters.AddWithValue("@ID", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    LoadStaff();
                    ShowMessage("Staff member deleted.");
                }
            }
            catch { ShowMessage("Cannot delete staff: They are linked to existing transactions.", true); }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlInput.Visible = false;
            ClearFields();
        }

        protected void btnShowAdd_Click(object sender, EventArgs e)
        {
            ClearFields();
            pnlInput.Visible = true;
        }

        private void ClearFields()
        {
            txtFullName.Text = txtUsername.Text = txtPassword.Text = "";
            hfStaffID.Value = "";
        }
    }
}