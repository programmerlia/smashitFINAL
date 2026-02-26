using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_court : System.Web.UI.Page
    {
        private string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindCourts();
            }
        }

        private void BindCourts()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {

                string query = @"
            SELECT 
                c.CourtID, c.Sports, c.Mode, c.Status AS CourtStatus,
                COALESCE(p.FullName, 'AVAILABLE') AS OccupantName,
                CONVERT(VARCHAR(5), r.StartTime, 108) AS DisplayStart,
                CONVERT(VARCHAR(5), r.EndTime, 108) AS DisplayEnd,
                
                -- Next Player Info
                (SELECT TOP 1 np.FullName 
                 FROM tblCourtQueue nq 
                 JOIN tblReservation nr ON nq.ReservationID = nr.ReservationID
                 JOIN tblPlayerAccount np ON nr.UserID = np.UserID
                 WHERE nq.CourtID = c.CourtID AND nq.Status = 'Waiting' 
                 ORDER BY nq.QueueNumber ASC) AS NextPlayerName,

                (SELECT TOP 1 CONVERT(VARCHAR(5), nr.StartTime, 108)
                 FROM tblCourtQueue nq 
                 JOIN tblReservation nr ON nq.ReservationID = nr.ReservationID
                 WHERE nq.CourtID = c.CourtID AND nq.Status = 'Waiting' 
                 ORDER BY nq.QueueNumber ASC) AS NextPlayerTime
            FROM tblCourt c
            LEFT JOIN tblCourtQueue q ON c.CourtID = q.CourtID AND q.Status = 'Playing'
            LEFT JOIN tblReservation r ON q.ReservationID = r.ReservationID
            LEFT JOIN tblPlayerAccount p ON r.UserID = p.UserID
            ORDER BY c.CourtID ASC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                rptCourts.DataSource = dt;
                rptCourts.DataBind();
            }
        }

        protected void TimerRefresh_Tick(object sender, EventArgs e)
        {
            BindCourts();
        }

        protected void rptCourts_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int courtID = Convert.ToInt32(e.CommandArgument);
            string sqlQuery = "";

            if (e.CommandName == "ToggleStatus")
            {

                sqlQuery = @"UPDATE tblCourt SET Status = 
                     CASE Status 
                        WHEN 'Available' THEN 'Playing' 
                        WHEN 'Playing' THEN 'Closed' 
                        ELSE 'Available' END 
                     WHERE CourtID = @ID";
            }
            else if (e.CommandName == "SwitchSport")
            {

                sqlQuery = @"UPDATE tblCourt SET Sports = 
                     CASE Sports 
                        WHEN 'Badminton' THEN 'Pickleball' 
                        ELSE 'Badminton' END 
                     WHERE CourtID = @ID AND Mode = 'Switchable'";
            }

            if (!string.IsNullOrEmpty(sqlQuery))
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                        cmd.Parameters.AddWithValue("@ID", courtID);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    BindCourts();
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}