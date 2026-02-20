using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace login
{
    public partial class login : System.Web.UI.Page
    {
        // Controls
        protected TextBox txtUsername;
        protected TextBox txtPassword;
        protected Label lblMessage;
        protected Button btnLogin;

        protected void Page_Load(object sender, EventArgs e)
        {
            lblMessage.Text = string.Empty;
        }

        protected void btnLogin1_Click(object sender, EventArgs e)
        {
            string username = (txtUsername.Text ?? string.Empty).Trim();
            string password = (txtPassword.Text ?? string.Empty).Trim();

            // Basic validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                string message = "Please enter both username and password.";
                lblMessage.Text = message;
                ScriptManager.RegisterStartupScript(Page, Page.GetType(), "loginAlert",
                    $"alert('{HttpUtility.JavaScriptStringEncode(message)}');", true);
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connStr))
            {
                con.Open();

                string query = @"
            SELECT FullName, StaffRole
            FROM tblStaffAccount
            WHERE Username = @Username AND Password = @Password";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string fullName = reader["FullName"].ToString();
                            string role = reader["StaffRole"].ToString();

                            // Store in session
                            Session["Username"] = username;
                            Session["FullName"] = fullName;
                            Session["Role"] = role;

                            // 🔥 Role-based redirect
                            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                Response.Redirect("~/adminpage/admin_dashboard.aspx");
                            }
                            else if (role.Equals("Receptionist", StringComparison.OrdinalIgnoreCase))
                            {
                                Response.Redirect("~/adminpage/receptionist_dashboard.aspx");
                            }
                            else
                            {
                                lblMessage.Text = "Unknown role assigned.";
                            }
                        }
                        else
                        {
                            string message = "Invalid username or password.";
                            lblMessage.Text = message;
                            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "loginAlert",
                                $"alert('{HttpUtility.JavaScriptStringEncode(message)}');", true);
                        }
                    }
                }
            }
        }
    }
}
