using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Linq;
using Smash_IT.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;

namespace Smash_IT.adminpage
{
    // CLASS NAME FIXED to match the ASPX Inherits directive
    public partial class admin_users : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

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
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string searchVal = (txtSearch != null) ? txtSearch.Text.Trim() : string.Empty;
                    string searchTerm = "%" + searchVal + "%";

                    string safeSortExpr = string.IsNullOrEmpty(SortExpr) ? "StaffID" : SortExpr;
                    string safeSortDir = string.IsNullOrEmpty(SortDir) ? "ASC" : SortDir;

                    string query = $@"
                    SELECT 
                        StaffID, 
                        Firstname + ' ' + Lastname AS FullName, 
                        Email, 
                        RoleName AS StaffRole, 
                        Username, 
                        ImgPath 
                    FROM tblStaffAccount 
                    WHERE Firstname LIKE @Search 
                       OR Lastname LIKE @Search 
                       OR Username LIKE @Search 
                       OR RoleName LIKE @Search 
                       OR Email LIKE @Search
                    ORDER BY {safeSortExpr} {safeSortDir};

                    SELECT COUNT(*) FROM tblStaffAccount;";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Search", searchTerm);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (gvStaff != null)
                    {
                        gvStaff.DataSource = ds.Tables[0];
                        gvStaff.DataBind();
                    }

                    if (lblTotalStaff != null && ds.Tables.Count > 1)
                    {
                        lblTotalStaff.Text = ds.Tables[1].Rows[0][0].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Data Load Error: " + ex.Message, true);
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
            string email = txtEmail.Text.Trim();
            bool isNew = string.IsNullOrEmpty(staffId);

            if (string.IsNullOrEmpty(txtFirstName.Text.Trim()) || string.IsNullOrEmpty(txtLastName.Text.Trim()) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                ShowMessage("First Name, Last Name, Email, and Username are required fields.", true);
                return;
            }

            if (isNew && string.IsNullOrEmpty(txtPassword.Text.Trim()))
            {
                ShowMessage("Password is required for new staff accounts.", true);
                return;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    ShowMessage("Please enter a valid email address.", true);
                    return;
                }
            }
            catch
            {
                ShowMessage("Please enter a valid email address.", true);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM tblStaffAccount WHERE Username = @User";
                    if (!isNew) checkQuery += " AND StaffID != @ID";

                    SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@User", username);
                    if (!isNew) checkCmd.Parameters.AddWithValue("@ID", staffId);

                    if ((int)checkCmd.ExecuteScalar() > 0)
                    {
                        ShowMessage("Username already taken by another staff member.", true);
                        return;
                    }

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
                        query = "INSERT INTO tblStaffAccount (Firstname, Lastname, Email, RoleName, Username, [Password], ImgPath) VALUES (@F, @L, @Email, @Role, @User, @Pass, @ImgPath)";
                    }
                    else
                    {
                        string updatePass = !string.IsNullOrEmpty(txtPassword.Text.Trim()) ? ", [Password]=@Pass" : "";
                        string updateImg = hasNewImage ? ", ImgPath=@ImgPath" : "";

                        query = $"UPDATE tblStaffAccount SET Firstname=@F, Lastname=@L, Email=@Email, RoleName=@Role, Username=@User {updatePass} {updateImg} WHERE StaffID=@ID";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@F", txtFirstName.Text.Trim());
                    cmd.Parameters.AddWithValue("@L", txtLastName.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Role", ddlRole.SelectedValue);
                    cmd.Parameters.AddWithValue("@User", username);

                    if (isNew || (!isNew && !string.IsNullOrEmpty(txtPassword.Text.Trim())))
                    {
                        cmd.Parameters.AddWithValue("@Pass", PasswordHasher.Hash(txtPassword.Text.Trim()));
                    }

                    if (!isNew)
                    {
                        cmd.Parameters.AddWithValue("@ID", staffId);
                    }

                    if (isNew || hasNewImage)
                    {
                        cmd.Parameters.AddWithValue("@ImgPath", imgPath);
                    }

                    cmd.ExecuteNonQuery();

                    pnlInput.Visible = false;
                    ClearFields();
                    LoadStaff();
                    ShowMessage(isNew ? "Staff added successfully!" : "Staff updated successfully!");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                    ShowMessage("That Email or Username is already taken.", true);
                else
                    ShowMessage("Database Error: " + sqlEx.Message, true);
            }
            catch (Exception ex)
            {
                ShowMessage("System Error: " + ex.Message, true);
            }
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
                    lblMsg.Text = "Editing Staff ID: " + id;
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
                    txtFirstName.Text = dr["Firstname"].ToString();
                    txtLastName.Text = dr["Lastname"].ToString();
                    txtEmail.Text = dr["Email"].ToString();
                    txtUsername.Text = dr["Username"].ToString();

                    string roleFromDb = dr["RoleName"].ToString().ToLower();
                    ListItem roleItem = ddlRole.Items.FindByValue(roleFromDb);

                    if (roleItem != null)
                    {
                        ddlRole.ClearSelection();
                        roleItem.Selected = true;
                    }
                    else
                    {
                        ddlRole.SelectedIndex = 0;
                    }

                    string path = dr["ImgPath"].ToString();
                    imgPreview.ImageUrl = "~/" + path;
                    imgPreview.Visible = true;
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
                    ShowMessage("Staff member deleted successfully.");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 547)
                    ShowMessage("Cannot delete staff: They are linked to existing records (e.g., courts, events, announcements).", true);
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

        protected void btnShowAdd_Click(object sender, EventArgs e)
        {
            ClearFields();
            pnlInput.Visible = true;
        }

        private void ClearFields()
        {
            txtFirstName.Text = txtLastName.Text = txtEmail.Text = txtUsername.Text = txtPassword.Text = "";
            hfStaffID.Value = "";
            imgPreview.Visible = false;
            fileAvatar.Attributes.Clear();
            ddlRole.SelectedIndex = 0;
        }

      
    }
}
