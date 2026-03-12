using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace Smash_IT.adminpage
{
    public partial class admin : System.Web.UI.MasterPage
    {
        public string StaffDisplayName
        {
            get { return lblFullName.Text; }
            set { lblFullName.Text = value; }
        }

        public string StaffRole
        {
            get { return lblRole.Text; }
            set { lblRole.Text = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Username"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                DisplayUserInfo();
            }

            // Always run this so the highlight updates when changing pages
            HighlightActiveNav();
        }

        private void HighlightActiveNav()
        {
            string currentPage = Path.GetFileName(Request.Path).ToLower();

            // Locate the menu container using its ID
            if (sidebarMenu != null)
            {
                foreach (Control ctrl in sidebarMenu.Controls)
                {
                    if (ctrl is HyperLink navLink)
                    {
                        // Match URL to current page
                        if (navLink.NavigateUrl.ToLower().Contains(currentPage))
                        {
                            navLink.CssClass = "active";
                        }
                        else
                        {
                            // Important: Reset other links so only one is yellow
                            navLink.CssClass = "";
                        }
                    }
                }
            }
        }

        private void DisplayUserInfo()
        {
            lblFullName.Text = Session["FullName"]?.ToString() ?? "Administrator";
            lblRole.Text = Session["RoleName"]?.ToString() ?? "Staff";
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/login.aspx");
        }
    }
}