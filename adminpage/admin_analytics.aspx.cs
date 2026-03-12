using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_analytics1 : System.Web.UI.Page
    {
        private string connString = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                txtAnalyticsDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                RefreshDashboard();
            }
        }

        protected void DateChanged(object sender, EventArgs e) => RefreshDashboard();
        protected void RefreshButton_Click(object sender, EventArgs e) => RefreshDashboard();

        private void RefreshDashboard()
        {
            if (!DateTime.TryParse(txtAnalyticsDate.Text, out DateTime selectedDate))
                selectedDate = DateTime.Now;

            lblDisplayDate.Text = selectedDate.ToString("MMMM dd, yyyy");
            string dateParam = selectedDate.ToString("yyyy-MM-dd");

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                LoadReservationStats(conn, dateParam);
                LoadKPIStats(conn, dateParam); // Changed from LoadQueueStats
                LoadRevenue(conn, dateParam);
                LoadRecentPayments(conn, dateParam);
                LoadActivityMix(conn, dateParam);
            }
        }

        private void LoadReservationStats(SqlConnection conn, string date)
        {
            string query = @"SELECT ReservationStatusName, COUNT(*) as Count FROM tblReservation 
                             WHERE CAST(ResDate AS DATE) = @date GROUP BY ReservationStatusName";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);

                int pending = 0, approvedCompleted = 0, cancelled = 0;

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string status = rdr["ReservationStatusName"].ToString();
                        int count = Convert.ToInt32(rdr["Count"]);

                        if (status == "Cancelled") cancelled += count;
                        else if (status == "Pending") pending += count;
                        else if (status == "Approved" || status == "Completed") approvedCompleted += count;
                    }
                }

                lblCancelled.Text = cancelled.ToString();
                lblPending.Text = pending.ToString();
                lblApproved.Text = approvedCompleted.ToString();
            }
        }

        private void LoadKPIStats(SqlConnection conn, string date)
        {
            // 1. Walk-Ins directly from the WalkIn table
            string walkInQuery = "SELECT COUNT(*) FROM tblPlayerWalkIn WHERE CAST(CreatedAt AS DATE) = @date";
            using (SqlCommand cmd = new SqlCommand(walkInQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblQueueWalkIn.Text = cmd.ExecuteScalar().ToString();
            }

            // 2. Reservations directly from the Reservation table
            string resQuery = "SELECT COUNT(*) FROM tblReservation WHERE CAST(ResDate AS DATE) = @date AND ReservationStatusName != 'Cancelled'";
            using (SqlCommand cmd = new SqlCommand(resQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblQueueRes.Text = cmd.ExecuteScalar().ToString();
            }

            // 3. Total Check-ins (Walk-Ins + Reservations)
            int walkIns = int.Parse(lblQueueWalkIn.Text);
            int reservations = int.Parse(lblQueueRes.Text);
            lblQueuePaid.Text = (walkIns + reservations).ToString();
        }

        private void LoadRevenue(SqlConnection conn, string date)
        {
            string query = "SELECT ISNULL(SUM(Amount), 0) FROM tblPayment WHERE CAST(PaymentDate AS DATE) = @date";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                decimal totalRev = Convert.ToDecimal(cmd.ExecuteScalar());
                lblRevenue.Text = string.Format("₱{0:N2}", totalRev);
            }
        }

        private void LoadRecentPayments(SqlConnection conn, string date)
        {
            string query = @"
                SELECT TOP 10 
                    p.PaymentDate, 
                    p.Amount, 
                    p.PaymentTypeName,
                    COALESCE(r.PaymentStatus, 'Completed') as PaymentStatus,
                    COALESCE((pa.Firstname + ' ' + pa.Lastname), (pw.Firstname + ' ' + pw.Lastname), 'Guest') as PayerName
                FROM tblPayment p
                LEFT JOIN tblPlayerAccount pa ON p.UserID = pa.UserID
                LEFT JOIN tblPlayerWalkIn pw ON p.WalkInID = pw.WalkInID
                LEFT JOIN tblReservation r ON p.ReservationID = r.ReservationID
                WHERE CAST(p.PaymentDate AS DATE) = @date
                ORDER BY p.PaymentID DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvRecentPayments.DataSource = dt;
                    gvRecentPayments.DataBind();
                }
            }
        }

        private void LoadActivityMix(SqlConnection conn, string date)
        {
            // 1. Sport Popularity (Based on Court Assignments)
            string sportQuery = @"SELECT c.SportName, COUNT(r.ReservationID) as Count
                        FROM tblReservation r
                        JOIN tblCourt c ON r.CourtID = c.CourtID
                        WHERE CAST(r.ResDate AS DATE) = @date AND r.ReservationStatusName IN ('Approved', 'Completed')
                        GROUP BY c.SportName";

            using (SqlCommand cmd = new SqlCommand(sportQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblBadmintonCount.Text = "0"; lblPickleballCount.Text = "0";
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string sport = rdr["SportName"].ToString().ToLower();
                        if (sport == "badminton") lblBadmintonCount.Text = rdr["Count"].ToString();
                        else if (sport == "pickleball") lblPickleballCount.Text = rdr["Count"].ToString();
                    }
                }
            }

            // 2. Exact Revenue Split (Pulling directly from raw tables to prevent missing data)

            // Court Revenue (From tblPayment)
            string courtQuery = "SELECT ISNULL(SUM(Amount), 0) FROM tblPayment WHERE CAST(PaymentDate AS DATE) = @date AND PaymentTypeName IN ('Reservation', 'WalkIn', 'Queue')";
            using (SqlCommand cmd = new SqlCommand(courtQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblRevCourts.Text = string.Format("₱{0:N2}", Convert.ToDecimal(cmd.ExecuteScalar()));
            }

            // Rental Revenue (Directly from tblRental)
            string rentalQuery = "SELECT ISNULL(SUM(UnitPrice), 0) FROM tblRental WHERE CAST(RentalDate AS DATE) = @date AND IsPaid = 1";
            using (SqlCommand cmd = new SqlCommand(rentalQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblRevRentals.Text = string.Format("₱{0:N2}", Convert.ToDecimal(cmd.ExecuteScalar()));
            }

            // Consumables Revenue (Directly from tblConsumable)
            string consQuery = "SELECT ISNULL(SUM(UnitPrice * Quantity), 0) FROM tblConsumable WHERE CAST(PurchaseDate AS DATE) = @date AND IsPaid = 1";
            using (SqlCommand cmd = new SqlCommand(consQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblRevConsumables.Text = string.Format("₱{0:N2}", Convert.ToDecimal(cmd.ExecuteScalar()));
            }

            // PAYC Revenue (From tblPayment)
            string paycQuery = "SELECT ISNULL(SUM(Amount), 0) FROM tblPayment WHERE CAST(PaymentDate AS DATE) = @date AND PaymentTypeName = 'PAYC'";
            using (SqlCommand cmd = new SqlCommand(paycQuery, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                lblRevPAYC.Text = string.Format("₱{0:N2}", Convert.ToDecimal(cmd.ExecuteScalar()));
            }
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=SmashIT_Revenue_Report.xls");
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";

            using (StringWriter sw = new StringWriter())
            {
                using (HtmlTextWriter htw = new HtmlTextWriter(sw))
                {
                    GridView gv = new GridView();
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        string query = @"SELECT CAST(PaymentDate AS DATE) as [Date], 
                                         PaymentTypeName as [Category],
                                         SUM(Amount) as [Revenue], 
                                         COUNT(PaymentID) as [Transactions]
                                         FROM tblPayment 
                                         GROUP BY CAST(PaymentDate AS DATE), PaymentTypeName 
                                         ORDER BY [Date] DESC";
                        using (SqlDataAdapter adp = new SqlDataAdapter(query, conn))
                        {
                            DataTable dt = new DataTable();
                            adp.Fill(dt);
                            gv.DataSource = dt;
                            gv.DataBind();
                        }
                    }
                    gv.RenderControl(htw);
                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();
                }
            }
        }

        public override void VerifyRenderingInServerForm(Control control) { }
    }
}