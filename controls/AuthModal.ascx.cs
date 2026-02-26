using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Web.UI;

namespace Smash_IT.Controls
{
    public partial class AuthModal : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }


        // ================= LOGIN =================
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtLoginUsername.Text.Trim();
            string password = txtLoginPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblLoginMsg.Text = "Please enter username and password.";
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT UserID FROM tblPlayerAccount WHERE Username=@Username AND Password=@Password", con);
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);

                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        // Login successful
                        Session["UserID"] = result;
                        Session["Username"] = username;


                        // Clear fields
                        txtLoginUsername.Text = "";
                        txtLoginPassword.Text = "";
                        lblLoginMsg.Text = "";

                        // Close modal first, then redirect
                        // Close modal and redirect using ScriptManager
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
    // fallback
    window.location.reload();
  }, 200);
", true);
                        return;
                    }
                    else
                    {
                        lblLoginMsg.Text = "Invalid username or password.";
                        txtLoginUsername.Text = "";
                        txtLoginPassword.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                lblLoginMsg.Text = "Error: " + ex.Message;
            }
        }


        // ================= SIGNUP - SEND OTP =================
        protected void btnSignup_Click(object sender, EventArgs e)
        {
            // Get input values
            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string username = txtSignupUsername.Text.Trim();
            string password = txtSignupPassword.Text.Trim();

            // Validate fields
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblSignupMsg.Text = "Please fill in all required fields.";
                return;
            }

            // Check if username or email already exists
            if (CheckUserExists(username, email))
            {
                lblSignupMsg.Text = "Username or Email already exists.";
                return;
            }

            try
            {
                // Generate OTP
                string otp = new Random().Next(100000, 999999).ToString();

                // Store in session
                Session["OTP"] = otp;

                Session["SignupData"] = new { fullName, email, phone, username, password };

                // Send OTP email
                SendOTPEmail(email, otp);

                // Show Step 2 (Verify OTP) - Keep modal open AND switch to Signup tab
                ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowOTPStep", @"
  // ensure signup tab is active (keep your tab switching code if you want)

  if (window.AuthSignup) {
    window.AuthSignup.setStep(2);
  }

  var modalEl = document.getElementById('loginSignupModal');
  var myModal = bootstrap.Modal.getOrCreateInstance(modalEl);
  myModal.show();
", true);
            }
            catch (Exception ex)
            {
                lblSignupMsg.Text = "Error: " + ex.Message;
            }
        }

        // Check if username or email already exists
        private bool CheckUserExists(string username, string email)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM tblPlayerAccount WHERE Username = @Username OR Email = @Email", con);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }


        // ================= VERIFY OTP =================

        protected void btnVerifyOTP_Click(object sender, EventArgs e)
        {
            string enteredOTP = txtOTP.Text.Trim();

            if (Session["OTP"] == null || enteredOTP != Session["OTP"].ToString())
            {
                lblOTPMsg.Text = "Invalid OTP. Please try again.";
                txtOTP.Text = "";

                ScriptManager.RegisterStartupScript(this, this.GetType(), "KeepModalOpen", @"
            var modalEl = document.getElementById('loginSignupModal');
            var myModal = new bootstrap.Modal(modalEl);
            myModal.show();
        ", true);

                return;
            }

            // OTP OK -> mark verified, go to Step 3
            Session["OTPVerified"] = true;
            lblOTPMsg.Text = "";

            ScriptManager.RegisterStartupScript(this, this.GetType(), "ShowTermsStep", @"
  // ensure signup tab is active (keep your tab switching code if you want)

  if (window.AuthSignup) {
    window.AuthSignup.unlockStep3();
    window.AuthSignup.setStep(3);
  }

  var modalEl = document.getElementById('loginSignupModal');
  var myModal = bootstrap.Modal.getOrCreateInstance(modalEl);
  myModal.show();
", true);
        }


        protected void btnCreateAccount_Click(object sender, EventArgs e)
        {
            // Must have OTP verified
            if (Session["OTPVerified"] == null || !(bool)Session["OTPVerified"])
            {
                lblTermsMsg.Text = "Please verify your OTP first.";
                return;
            }

            // Must accept terms
            if (!chkTerms.Checked)
            {
                lblTermsMsg.Text = "You must accept the Terms & Conditions.";
                return;
            }

            try
            {
                // Get signup data from session
                dynamic data = Session["SignupData"];
                if (data == null)
                {
                    lblTermsMsg.Text = "Signup session expired. Please start again.";
                    return;
                }

                string fullName = data.fullName;
                string email = data.email;
                string phone = data.phone;
                string username = data.username;
                string password = data.password;

                int newUserId = InsertUser(fullName, email, phone, username, password);

                if (newUserId > 0)
                {
                    Session["UserID"] = newUserId;
                    Session["Username"] = username;

                    // Clear sessions
                    Session["OTP"] = null;
                    Session["OTPVerified"] = null;
                    Session["SignupData"] = null;

                    // Clear fields
                    ClearSignupFields();
                    chkTerms.Checked = false;
                    lblTermsMsg.Text = "";

                    // Close modal and refresh/redirect
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

                    return;
                }

                lblTermsMsg.Text = "Error creating account. Please try again.";
            }
            catch (Exception ex)
            {
                lblTermsMsg.Text = "Error: " + ex.Message;
            }
        }

        // Insert new user into database
        private int InsertUser(string fullName, string email, string phone, string username, string password)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(@"
                INSERT INTO tblPlayerAccount (FullName, Email, PhoneNumber, Username, Password, CreatedAt) 
                VALUES (@FullName, @Email, @Phone, @Username, @Password, @CreatedAt);
                SELECT SCOPE_IDENTITY();", con);

                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }


        // ================= SEND OTP EMAIL =================
        private void SendOTPEmail(string toEmail, string otp)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.To.Add(toEmail);
                mail.From = new MailAddress("hannaliolayvar@gmail.com");
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

                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("hannaliolayvar@gmail.com", "REVOKED_SMTP_APP_PASSWORD");
                smtp.EnableSsl = true;
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Log error but don't stop the process
                System.Diagnostics.Debug.WriteLine("Email Error: " + ex.Message);
            }
        }


        // ================= HELPER METHODS =================
        private void ClearSignupFields()
        {
            txtFullName.Text = "";
            txtEmail.Text = "";
            txtPhone.Text = "";
            txtSignupUsername.Text = "";
            txtSignupPassword.Text = "";
            txtOTP.Text = "";
            lblSignupMsg.Text = "";
            lblOTPMsg.Text = "";
        }
    }


}