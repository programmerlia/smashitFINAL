using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

namespace login
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Check if both cookies exist
                if (Request.Cookies["SmashItSavedUser"] != null && Request.Cookies["SmashItSavedPass"] != null)
                {
                    txtUsername.Text = Request.Cookies["SmashItSavedUser"].Value;

                    try
                    {
                        // Decode the password from Base64
                        string encodedPass = Request.Cookies["SmashItSavedPass"].Value;
                        byte[] passBytes = Convert.FromBase64String(encodedPass);
                        string decodedPass = Encoding.UTF8.GetString(passBytes);

                        // Set the value attribute so it populates the Password box
                        txtPassword.Attributes.Add("value", decodedPass);
                        chkRememberMe.Checked = true;
                    }
                    catch
                    {
                        // If decoding fails (corrupted cookie), clear it
                    }
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

                            // Case-sensitive validation
                            if (dbUser.Equals(inputUser, StringComparison.Ordinal) &&
                                dbPass.Equals(inputPass, StringComparison.Ordinal))
                            {
                                HandleCookies(dbUser, inputPass);

                                // Set Sessions
                                Session["StaffID"] = dbID;
                                Session["Username"] = dbUser;
                                Session["FullName"] = $"{reader["Firstname"]} {reader["Lastname"]}";
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

        private void HandleCookies(string user, string pass)
        {
            if (chkRememberMe.Checked)
            {
                // Save Username
                HttpCookie userCookie = new HttpCookie("SmashItSavedUser")
                {
                    Value = user,
                    Expires = DateTime.Now.AddDays(30)
                };
                Response.Cookies.Add(userCookie);

                // Save Password (Base64 Encoded)
                string encodedPass = Convert.ToBase64String(Encoding.UTF8.GetBytes(pass));
                HttpCookie passCookie = new HttpCookie("SmashItSavedPass")
                {
                    Value = encodedPass,
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true // Enhanced security against XSS
                };
                Response.Cookies.Add(passCookie);
            }
            else
            {
                // Expire existing cookies if unchecked
                if (Request.Cookies["SmashItSavedUser"] != null)
                {
                    HttpCookie c1 = new HttpCookie("SmashItSavedUser") { Expires = DateTime.Now.AddDays(-1) };
                    Response.Cookies.Add(c1);
                }
                if (Request.Cookies["SmashItSavedPass"] != null)
                {
                    HttpCookie c2 = new HttpCookie("SmashItSavedPass") { Expires = DateTime.Now.AddDays(-1) };
                    Response.Cookies.Add(c2);
                }
            }
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