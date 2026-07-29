using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.UI;
using Smash_IT.Security;

namespace Smash_IT.Controls
{
    public partial class AuthModal : System.Web.UI.UserControl
    {
        private string CS => ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e) { }

        // ✅ Helper: load user details into Session (used by login + signup)
        private void SetUserSessionFromDb(int userId)
        {
            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"
SELECT UserID, Username, Firstname, Lastname, Email, PhoneNumber
FROM tblPlayerAccount
WHERE UserID = @UserID;", con))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return;

                        Session["UserID"] = Convert.ToInt32(dr["UserID"]);
                        Session["Username"] = Convert.ToString(dr["Username"]);

                        // ✅ THESE are what your reservation.aspx reads
                        Session["Firstname"] = Convert.ToString(dr["Firstname"]);
                        Session["Lastname"] = Convert.ToString(dr["Lastname"]);
                        Session["Email"] = Convert.ToString(dr["Email"]);
                        Session["PhoneNumber"] = Convert.ToString(dr["PhoneNumber"]);
                    }
                }
            }
        }

        // ================= LOGIN =================
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = (txtLoginUsername.Text ?? "").Trim();
            string password = (txtLoginPassword.Text ?? "").Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblLoginMsg.Text = "Please enter username and password.";
                return;
            }

            try
            {
                int userId = 0;

                using (SqlConnection con = new SqlConnection(CS))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(@"
SELECT UserID, [Password]
FROM tblPlayerAccount
WHERE Username=@Username;", con))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                lblLoginMsg.Text = "Invalid username or password.";
                                txtLoginUsername.Text = "";
                                txtLoginPassword.Text = "";
                                return;
                            }
                            userId = Convert.ToInt32(reader["UserID"]);
                            string storedPassword = Convert.ToString(reader["Password"]);
                            bool valid = PasswordHasher.Verify(password, storedPassword) || (PasswordHasher.IsLegacyPlainText(storedPassword) && storedPassword == password);
                            if (!valid)
                            {
                                lblLoginMsg.Text = "Invalid username or password.";
                                txtLoginUsername.Text = "";
                                txtLoginPassword.Text = "";
                                return;
                            }
                            if (PasswordHasher.IsLegacyPlainText(storedPassword))
                            {
                                reader.Close();
                                using (SqlCommand update = new SqlCommand("UPDATE tblPlayerAccount SET [Password]=@Password WHERE UserID=@UserID", con))
                                {
                                    update.Parameters.AddWithValue("@Password", PasswordHasher.Hash(password));
                                    update.Parameters.AddWithValue("@UserID", userId);
                                    update.ExecuteNonQuery();
                                }
                            }
                        }

                    }
                }

                // ✅ sets UserID + Username + Firstname/Lastname/Email/PhoneNumber
                SetUserSessionFromDb(userId);

                txtLoginUsername.Text = "";
                txtLoginPassword.Text = "";
                lblLoginMsg.Text = "";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "LoginSuccess", @"
var modal = document.getElementById('loginSignupModal');
var bsModal = bootstrap.Modal.getInstance(modal);
if (bsModal) bsModal.hide();

setTimeout(function () {
  var returnUrl = sessionStorage.getItem('returnUrlAfterLogin');
  if (returnUrl) {
    sessionStorage.removeItem('returnUrlAfterLogin');
    window.location.href = returnUrl;
    return;
  }
  window.location.reload();
}, 200);
", true);
            }
            catch (Exception ex)
            {
                lblLoginMsg.Text = "Error: " + ex.Message;
            }
        }

        // ================= SIGNUP - SEND OTP =================
        protected void btnSignup_Click(object sender, EventArgs e)
        {
            string firstName = (txtFirstname.Text ?? "").Trim();
            string lastName = (txtLastname.Text ?? "").Trim();
            string email = (txtEmail.Text ?? "").Trim();
            string phone = (txtPhone.Text ?? "").Trim();
            string username = (txtSignupUsername.Text ?? "").Trim();
            string password = (txtSignupPassword.Text ?? "").Trim();

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone) ||
                string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblSignupMsg.Text = "Please fill in all required fields.";
                ShowSignupModal(step: 1, unlock3: false);
                return;
            }

            if (CheckUserExists(username, email))
            {
                lblSignupMsg.Text = "Username or Email already exists.";
                ShowSignupModal(step: 1, unlock3: false);
                return;
            }

            try
            {
                string otp = new Random().Next(100000, 999999).ToString();
                Session["OTP"] = otp;

                Session["SignupData"] = new
                {
                    firstName,
                    lastName,
                    email,
                    phone,
                    username,
                    password
                };

                SendOTPEmail(email, otp);
                ShowSignupModal(step: 2, unlock3: false);
            }
            catch (Exception ex)
            {
                lblSignupMsg.Text = "Error: " + ex.Message;
            }
        }

        private bool CheckUserExists(string username, string email)
        {
            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM tblPlayerAccount WHERE Username = @Username OR Email = @Email", con))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // ================= VERIFY OTP =================
        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            string enteredOTP = (txtOTP.Text ?? "").Trim();

            if (Session["OTP"] == null || enteredOTP != Session["OTP"].ToString())
            {
                lblOTPMsg.Text = "Invalid OTP. Please try again.";
                txtOTP.Text = "";
                ShowSignupModal(step: 2, unlock3: false);
                return;
            }

            Session["OTPVerified"] = true;
            lblOTPMsg.Text = "";
            ShowSignupModal(step: 3, unlock3: true);
        }

        protected void btnCreateAccount_Click(object sender, EventArgs e)
        {
            if (Session["OTPVerified"] == null || !(bool)Session["OTPVerified"])
            {
                lblTermsMsg.Text = "Please verify your OTP first.";
                return;
            }

            if (!chkTerms.Checked)
            {
                lblTermsMsg.Text = "You must accept the Terms & Conditions.";
                ShowSignupModal(step: 3, unlock3: true);
                return;
            }

            try
            {
                dynamic data = Session["SignupData"];
                if (data == null)
                {
                    lblTermsMsg.Text = "Signup session expired. Please start again.";
                    return;
                }

                string firstname = data.firstName;
                string lastname = data.lastName;
                string email = data.email;
                string phone = data.phone;
                string username = data.username;
                string password = data.password;

                int newUserId = InsertUser(firstname, lastname, email, phone, username, password);

                if (newUserId <= 0)
                {
                    lblTermsMsg.Text = "Error creating account. Please try again.";
                    return;
                }

                // ✅ set sessions for reservation autofill (either via DB helper or directly)
                SetUserSessionFromDb(newUserId);

                Session["OTP"] = null;
                Session["OTPVerified"] = null;
                Session["SignupData"] = null;

                ClearSignupFields();
                chkTerms.Checked = false;
                lblTermsMsg.Text = "";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "SignupSuccess", @"
var modal = document.getElementById('loginSignupModal');
var bsModal = bootstrap.Modal.getInstance(modal);
if (bsModal) bsModal.hide();

setTimeout(function () {
  var returnUrl = sessionStorage.getItem('returnUrlAfterLogin');
  if (returnUrl) {
    sessionStorage.removeItem('returnUrlAfterLogin');
    window.location.href = returnUrl;
    return;
  }
  window.location.reload();
}, 200);
", true);
            }
            catch (Exception ex)
            {
                lblTermsMsg.Text = "Error: " + ex.Message;
            }
        }

        private int InsertUser(string firstname, string lastname, string email, string phone, string username, string password)
        {
            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO tblPlayerAccount (Firstname, Lastname, Email, PhoneNumber, Username, Password, CreatedAt) 
VALUES (@firstname, @lastname, @Email, @Phone, @Username, @Password, @CreatedAt);
SELECT SCOPE_IDENTITY();", con))
                {
                    cmd.Parameters.AddWithValue("@firstname", firstname);
                    cmd.Parameters.AddWithValue("@lastname", lastname);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Phone", phone);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", PasswordHasher.Hash(password));
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void SendOTPEmail(string toEmail, string otp)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(toEmail);
                string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
                string smtpPortValue = ConfigurationManager.AppSettings["SmtpPort"];
                string smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                string smtpFrom = ConfigurationManager.AppSettings["SmtpFrom"];
                int smtpPort;
                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(smtpFrom) || !int.TryParse(smtpPortValue, out smtpPort)) throw new ConfigurationErrorsException("SMTP settings are missing or invalid.");
                mail.From = new MailAddress(smtpFrom);
                mail.Subject = "Smash-It: OTP Verification";
                mail.Body = $@"
<html>
<body style='font-family: Arial, sans-serif;'>
  <h2>Welcome to Smash-It!</h2>
  <p>Your OTP for account verification is:</p>
  <h1 style='color: #1a187c;'>{otp}</h1>
  <p>Please enter this code to verify your account.</p>
  <p>If you did not request this, please ignore this email.</p>
</body>
</html>";
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
                smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }

        private void ClearSignupFields()
        {
            txtFirstname.Text = "";
            txtLastname.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtSignupUsername.Text = "";
            txtSignupPassword.Text = "";
            txtOTP.Text = "";
            lblSignupMsg.Text = "";
            lblOTPMsg.Text = "";
        }

        private void ShowSignupModal(int step, bool unlock3)
        {
            hfSignupStep.Value = step.ToString();

            string script = $@"
(function(){{
  var modalEl = document.getElementById('loginSignupModal');
  if (modalEl) {{
    var m = bootstrap.Modal.getOrCreateInstance(modalEl);
    m.show();
  }}

  var signupTab = document.querySelector('a[href=""#signupTab""]');
  if (signupTab) {{
    var t = new bootstrap.Tab(signupTab);
    t.show();
  }}

  if (window.AuthSignup) {{
    {(unlock3 ? "AuthSignup.unlockStep3();" : "AuthSignup.lockStep3();")}
    AuthSignup.setStep({step});
  }}
}})();";

            ScriptManager.RegisterStartupScript(this, GetType(), "ShowSignupModal", script, true);
        }
    }
}
