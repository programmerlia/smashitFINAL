using System;

namespace Smash_IT.queuemasterpage
{
    public partial class q_dashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadStaffName();
            }
        }

        private void LoadStaffName()
        {
            if (Session["FullName"] != null)
            {
                litStaffName.Text = Session["FullName"].ToString();
            }
            else
            {
                litStaffName.Text = "Queue Master";
            }
        }
    }
}