using System;
using System.Web.UI;

namespace Smash_IT.adminpage
{
    public partial class admin_dashboard : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Security check
            if (Session["Username"] == null)
            {
                Response.Redirect("~/login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                SetDisplayName();
            }
        }

        private void SetDisplayName()
        {
            // Set the Sidebar/Master Page Labels
            if (Session["FullName"] != null)
            {
                Master.StaffDisplayName = Session["FullName"].ToString();

                // Also set the big "Hi, Name!" welcome message on the dashboard
                litStaffName.Text = Session["FullName"].ToString();
            }

            if (Session["RoleName"] != null)
            {
                Master.StaffRole = Session["RoleName"].ToString();
            }
        }
    }
}