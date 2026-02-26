using System;
using System.IO;

namespace Smash_IT.adminpage
{
    public partial class admin : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // 1. Security Check: Redirect if not logged in
            if (Session["Username"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                SetActiveNavLink();
                DisplayUserInfo();
            }
        }

        private void SetActiveNavLink()
        {
            string page = Path.GetFileName(Request.Path).ToLower();

            // Reset classes
            navAnalytics.CssClass = "";
            navAnnouncement.CssClass = "";
            navReservation.CssClass = "";
            navUsers.CssClass = "";
            navCourts.CssClass = "";
            navCustomerAccount.CssClass = "";

            switch (page)
            {
                case "admin_analytics.aspx":
                    navAnalytics.CssClass = "active";
                    break;
                case "admin_announcement.aspx":
                    navAnnouncement.CssClass = "active";
                    break;
                case "admin_reservation.aspx":
                    navReservation.CssClass = "active";
                    break;
                case "admin_users.aspx":
                    navUsers.CssClass = "active";
                    break;
                case "admin_court.aspx":
                    navUsers.CssClass = "active";
                    break;
                case "admin_customer_account.aspx":
                    navUsers.CssClass = "active";
                    break;
            }
        }

        private void DisplayUserInfo()
        {
            lblFullName.Text = Session["FullName"]?.ToString() ?? "Staff Member";
            lblRole.Text = Session["Role"]?.ToString() ?? "Administrator";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {

            Session.Clear();
            Session.Abandon();


            Response.Redirect("~/login.aspx");
        }
    }
}