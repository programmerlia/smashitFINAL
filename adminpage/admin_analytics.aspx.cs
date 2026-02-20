using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Smash_IT.adminpage
{
    public partial class admin_analytics1 : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadDashboardStats();
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                try
                {
                    conn.Open();
                    string today = DateTime.Now.ToString("yyyy-MM-dd");

                    // 1. Reservations Summary
                    LoadReservationCounts(conn, today);

                    // 2. Queue Status
                    LoadQueueStatus(conn);

                    // 3. Walk-Ins
                    LoadWalkInCounts(conn, today);

                    // 4. Revenue
                    LoadRevenue(conn, today);

                    // 5. Court Utilization
                    LoadCourtUtilization(conn);

                    // 6. Top Players (Fixes "FullName" Error)
                    LoadTopPlayers(conn);

                    // 7. Recent Payments (Fixes "FullName" Error)
                    LoadRecentPayments(conn);

                    // 8. Staff Count
                    SqlCommand cmdStaff = new SqlCommand("SELECT COUNT(*) FROM tblStaffAccount", conn);
                    lblStaffCount.Text = cmdStaff.ExecuteScalar().ToString();
                }
                catch (Exception ex)
                {
                    Response.Write("<script>alert('Error: " + ex.Message.Replace("'", "") + "');</script>");
                }
            }
        }

        private void LoadReservationCounts(SqlConnection conn, string today)
        {
            string query = "SELECT Status, COUNT(*) as Total FROM tblReservation WHERE CAST(ResDate AS DATE) = @today GROUP BY Status";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@today", today);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    lblPending.Text = "0"; lblPaid.Text = "0"; lblHappening.Text = "0"; lblCancelled.Text = "0";
                    while (rdr.Read())
                    {
                        string status = rdr["Status"].ToString();
                        if (status == "Pending") lblPending.Text = rdr["Total"].ToString();
                        else if (status == "Paid") lblPaid.Text = rdr["Total"].ToString();
                        else if (status == "Happening") lblHappening.Text = rdr["Total"].ToString();
                        else if (status == "Cancelled") lblCancelled.Text = rdr["Total"].ToString();
                    }
                }
            }
        }

        private void LoadQueueStatus(SqlConnection conn)
        {
            string query = "SELECT Status, COUNT(*) as Total FROM tblCourtQueue GROUP BY Status";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                lblWaiting.Text = "0"; lblPlaying.Text = "0"; lblDone.Text = "0";
                while (rdr.Read())
                {
                    string status = rdr["Status"].ToString();
                    if (status == "Waiting") lblWaiting.Text = rdr["Total"].ToString();
                    else if (status == "Playing") lblPlaying.Text = rdr["Total"].ToString();
                    else if (status == "Done") lblDone.Text = rdr["Total"].ToString();
                }
            }
        }

        private void LoadWalkInCounts(SqlConnection conn, string today)
        {
            string query = "SELECT COUNT(*) FROM tblPlayerWalkIn WHERE CAST(CreatedAt AS DATE) = @today";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@today", today);
                lblWalkInToday.Text = cmd.ExecuteScalar().ToString();
            }
        }

        private void LoadRevenue(SqlConnection conn, string today)
        {
            string query = "SELECT SUM(Amount) FROM tblPayment WHERE CAST(PaymentDate AS DATE) = @today AND PaymentStatus = 'Completed'";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@today", today);
                object result = cmd.ExecuteScalar();
                lblRevenue.Text = (result != DBNull.Value && result != null) ? string.Format("₱{0:N2}", result) : "₱0.00";
            }
        }

        private void LoadCourtUtilization(SqlConnection conn)
        {
            string query = "SELECT Status, COUNT(*) as Total FROM tblCourt GROUP BY Status";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                int total = 0; lblOccupied.Text = "0"; lblAvailable.Text = "0";
                while (rdr.Read())
                {
                    string status = rdr["Status"].ToString();
                    int count = Convert.ToInt32(rdr["Total"]);
                    total += count;
                    if (status == "Playing") lblOccupied.Text = count.ToString();
                    else if (status == "Available") lblAvailable.Text = count.ToString();
                }
                lblTotalCourts.Text = total.ToString();
            }
        }

        private void LoadTopPlayers(SqlConnection conn)
        {
            // Crucial: Alias must be "FullName" to match the Repeater Eval
            string query = @"SELECT TOP 3 p.FullName, COUNT(r.ReservationID) as BookingCount 
                             FROM tblPlayerAccount p 
                             JOIN tblReservation r ON p.UserID = r.UserID 
                             GROUP BY p.FullName ORDER BY BookingCount DESC";
            using (SqlDataAdapter adp = new SqlDataAdapter(query, conn))
            {
                DataTable dt = new DataTable();
                adp.Fill(dt);
                rptTopPlayers.DataSource = dt;
                rptTopPlayers.DataBind();
            }
        }

        private void LoadRecentPayments(SqlConnection conn)
        {
            // Crucial: Alias must be "FullName" to match GridView BoundField
            string query = @"SELECT TOP 5 py.Amount, py.PaymentStatus, py.PaymentDate, 
                             COALESCE(pa.FullName, py.WalkInName, 'Guest') as FullName
                             FROM tblPayment py
                             LEFT JOIN tblPlayerAccount pa ON py.UserID = pa.UserID
                             LEFT JOIN tblPlayerWalkIn pw ON py.WalkInID = pw.WalkInID
                             ORDER BY py.PaymentDate DESC";
            using (SqlDataAdapter adp = new SqlDataAdapter(query, conn))
            {
                DataTable dt = new DataTable();
                adp.Fill(dt);
                gvRecentPayments.DataSource = dt;
                gvRecentPayments.DataBind();
            }
        }
    }
}