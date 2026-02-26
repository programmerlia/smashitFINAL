using System;

namespace Smash_IT.receptionistpage
{
    public partial class receptionist : System.Web.UI.MasterPage
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["Username"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                lblFullName.Text = "<b>Name: </b>" + (Session["FullName"]?.ToString() ?? "Unknown");
                lblRole.Text = "<b>Role: </b>" + (Session["Role"]?.ToString() ?? "Unknown");
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();

            Response.Redirect("~/adminpage/login.aspx");
        }
    }
}
