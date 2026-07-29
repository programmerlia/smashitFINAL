using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using Smash_IT.Security;

namespace login
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.Cookies["SmashItSavedUser"] != null)
                {
                    txtUsername.Text = Request.Cookies["SmashItSavedUser"].Value;
                    chkRememberMe.Checked = true;
                }
            }
        }

        protected void btnLogin1_Click(object sender, EventArgs e)
        {
            string inputUser = (txtUsername.Text ?? string.Empty).Trim();
            string inputPass = (txtPassword.Text ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(inputUser) || string.IsNullOrEmpty(inputPass))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();
                string query = "SELECT  StaffID, Username, [Password], Firstname, Lastname, RoleName FROM tblStaffAccount WHERE Username = @User";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@User", inputUser);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string dbID = reader["StaffID"].ToString();
                            string dbUser = reader["Username"].ToString();
                            string dbPass = reader["Password"].ToString();
                            string dbRole = reader["RoleName"].ToString();
                            string firstName = reader["Firstname"].ToString();
                            string lastName = reader["Lastname"].ToString();

                            bool valid = PasswordHasher.Verify(inputPass, dbPass) || (PasswordHasher.IsLegacyPlainText(dbPass) && dbPass.Equals(inputPass, StringComparison.Ordinal));
                            if (dbUser.Equals(inputUser, StringComparison.Ordinal) && valid)
                            {
                                if (PasswordHasher.IsLegacyPlainText(dbPass))
                                {
                                    reader.Close();
                                    using (SqlCommand update = new SqlCommand("UPDATE tblStaffAccount SET [Password]=@Password WHERE StaffID=@StaffID", con))
                                    {
                                        update.Parameters.AddWithValue("@Password", PasswordHasher.Hash(inputPass));
                                        update.Parameters.AddWithValue("@StaffID", dbID);
                                        update.ExecuteNonQuery();
                                    }
                                }
                                HandleCookies(dbUser);

                                // Set Sessions
                                Session["StaffID"] = dbID;
                                Session["Username"] = dbUser;
                                Session["FullName"] = $"{firstName} {lastName}";
                                Session["RoleName"] = dbRole;

                                RedirectByRole(dbRole);
                            }
                            else
                            {
                                ShowError("Invalid Credentials. Check your spelling.");
                            }
                        }
                        else
                        {
                            ShowError("Account not found.");
                        }
                    }
                }
            }
        }

        private void HandleCookies(string user)
        {
            if (chkRememberMe.Checked)
            {
                // Save Username
                HttpCookie userCookie = new HttpCookie("SmashItSavedUser")
                {
                    Value = user,
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection
                };
                Response.Cookies.Add(userCookie);
                ExpireCookie("SmashItSavedPass");
            }
            else
            {
                ExpireCookie("SmashItSavedUser");
                ExpireCookie("SmashItSavedPass");
            }
        }

        private void ExpireCookie(string name)
        {
            Response.Cookies.Add(new HttpCookie(name) { Expires = DateTime.Now.AddDays(-1), HttpOnly = true, Secure = Request.IsSecureConnection });
        }

        private void RedirectByRole(string role)
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/adminpage/admin_dashboard.aspx");
            else if (role.Equals("Receptionist", StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/receptionistpage/receptionist_dashboard.aspx");
            else if (role.Equals("queuemaster", StringComparison.OrdinalIgnoreCase))
                Response.Redirect("~/queuemasterpage/q_dashboard.aspx");
            else
                ShowError("Unauthorized access.");
        }

        private void ShowError(string msg)
        {
            lblMessage.Text = msg;
            lblMessage.Visible = true;
            string cleanMsg = HttpUtility.JavaScriptStringEncode(msg);
            ScriptManager.RegisterStartupScript(this, GetType(), "loginErr", $"alert('{cleanMsg}');", true);
        }
    }
}
