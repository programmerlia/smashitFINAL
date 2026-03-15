using System;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.queuemasterpage
{
    public partial class queuemaster : System.Web.UI.MasterPage
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

            HighlightActiveNav();
        }

        private void DisplayUserInfo()
        {
            lblFullName.Text = Session["FullName"]?.ToString() ?? "Receptionist";
            lblRole.Text = Session["Role"]?.ToString() ?? "Receptionist";
        }

        private void HighlightActiveNav()
        {
            string currentPage = Path.GetFileName(Request.Path).ToLower();

            if (sidebarMenu != null)
            {
                foreach (Control ctrl in sidebarMenu.Controls)
                {
                    if (ctrl is HyperLink navLink)
                    {
                        string navUrl = ResolveUrl(navLink.NavigateUrl).ToLower();

                        if (navUrl.Contains(currentPage))
                        {
                            navLink.CssClass = "active";
                        }
                        else
                        {
                            navLink.CssClass = "";
                        }
                    }
                }
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/login.aspx");
        }
    }
}