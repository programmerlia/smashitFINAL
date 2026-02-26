using System;
using System.Data;

namespace Smash_IT.adminpage
{
    public partial class admin_dashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindCourtMonitor();
            }
        }

        private void BindCourtMonitor()
        {
            // Placeholder data based on your uploaded images (image_2605dc.png)
            // In a real scenario, you'd fetch this from your 'tblCourts' table
            DataTable dt = new DataTable();
            dt.Columns.Add("CourtName");
            dt.Columns.Add("Sport");
            dt.Columns.Add("StatusText");
            dt.Columns.Add("StatusClass"); // For CSS styling
            dt.Columns.Add("StatusIcon");
            dt.Columns.Add("CurrentPlayer");

            dt.Rows.Add("Court 1", "Badminton", "AVAILABLE", "status-available", "fas fa-user-check", "Hidilyn Diaz (Up Next)");
            dt.Rows.Add("Court 2", "Badminton", "PLAYING", "status-playing", "fas fa-running", "Mike Tyson");
            dt.Rows.Add("Court 5", "Badminton", "PLAYING", "status-playing", "fas fa-running", "Serena Williams");
            dt.Rows.Add("Court 6", "Pickleball", "AVAILABLE", "status-pickleball", "fas fa-check", "No bookings");

            rptCourts.DataSource = dt;
            rptCourts.DataBind();
        }
    }
}