using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Smash_IT.adminpage
{
    public partial class admin_transactions : System.Web.UI.Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        // Pass to JavaScript for the Line Chart
        public string ChartLabels = "[]";
        public string ChartData = "[]";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindData();
            }
        }

        protected void Filter_Changed(object sender, EventArgs e)
        {
            BindData();
        }

        protected void SetFilter_Click(object sender, EventArgs e)
        {
            string filter = ((Button)sender).CommandArgument;
            ddlTimeFilter.SelectedValue = filter;
            BindData();
        }

        private void BindData()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string timeFilter = ddlTimeFilter.SelectedValue;
                string dateClause = "";

                if (timeFilter == "Day")
                    dateClause = "AND CAST(pay.PaymentDate AS DATE) = CAST(GETDATE() AS DATE)";
                else if (timeFilter == "Week")
                    dateClause = "AND pay.PaymentDate >= DATEADD(day, -7, GETDATE())";
                else if (timeFilter == "Month")
                    dateClause = "AND pay.PaymentDate >= DATEADD(month, -1, GETDATE())";

                // Query matches your schema exactly (No PaymentMethod column)
                string query = $@"
                    SELECT 
                        pay.PaymentID, 
                        pay.PaymentDate, 
                        pay.Amount, 
                        pay.PaymentTypeName, 
                        COALESCE(u.Firstname + ' ' + u.Lastname, w.Firstname + ' ' + w.Lastname, 'Walk-In Player') as CustomerName,
                        CASE 
                            WHEN pay.ReservationID IS NOT NULL THEN 'Court Reservation'
                            WHEN pay.RentalID IS NOT NULL THEN 'Equipment Rental'
                            WHEN pay.ConsumableID IS NOT NULL THEN 'Item Purchase'
                            WHEN pay.PaymentTypeName = 'PAYC' THEN 'Play-All-You-Can'
                            WHEN pay.PaymentTypeName = 'VOID' THEN 'REVERSED'
                            ELSE 'General Service'
                        END as ItemDetails
                    FROM tblPayment pay
                    LEFT JOIN tblPlayerAccount u ON pay.UserID = u.UserID
                    LEFT JOIN tblPlayerWalkIn w ON pay.WalkInID = w.WalkInID
                    WHERE 1=1 {dateClause}
                    ORDER BY pay.PaymentDate DESC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                gvTransactions.DataSource = dt;
                gvTransactions.DataBind();

                // 1. Dashboard Aggregates
                decimal validTotal = dt.AsEnumerable()
                    .Where(r => r.Field<decimal>("Amount") > 0)
                    .Sum(r => r.Field<decimal>("Amount"));

                decimal voidTotal = dt.AsEnumerable()
                    .Where(r => r.Field<decimal>("Amount") < 0)
                    .Sum(r => r.Field<decimal>("Amount"));

                lblTotalRevenue.InnerText = $"₱{validTotal + voidTotal:N2}";
                lblTransCount.InnerText = dt.AsEnumerable().Count(r => r.Field<decimal>("Amount") > 0).ToString();
                lblVoidTotal.InnerText = $"₱{Math.Abs(voidTotal):N2}";

                // 2. Chart Logic (Revenue Trend)
                if (dt.Rows.Count > 0)
                {
                    var chartGroup = dt.AsEnumerable()
                        .GroupBy(r => r.Field<DateTime?>("PaymentDate")?.Date ?? DateTime.Today)
                        .Select(g => new { D = g.Key.ToString("MM/dd"), T = g.Sum(x => x.Field<decimal>("Amount")) })
                        .OrderBy(x => x.D).ToList();

                    ChartLabels = "['" + string.Join("','", chartGroup.Select(x => x.D)) + "']";
                    ChartData = "[" + string.Join(",", chartGroup.Select(x => x.T)) + "]";
                }
            }
        }

        protected void gvTransactions_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "VoidTrans")
            {
                int originalID = Convert.ToInt32(e.CommandArgument);
                VoidTransaction(originalID);
                BindData();
            }
            else if (e.CommandName == "ViewDetails")
            {
                int originalID = Convert.ToInt32(e.CommandArgument);
                LoadTransactionDetails(originalID);
            }
        }

        private void LoadTransactionDetails(int paymentId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                // Comprehensive query joining User Accounts, Walk-ins, Reservations, Rentals, and Consumables
                string query = @"
            SELECT 
                pay.PaymentID, pay.PaymentDate, pay.Amount, pay.PaymentTypeName,
                
                -- User & Walk-in Details
                u.Firstname as RegFName, u.Lastname as RegLName, u.Email, u.PhoneNumber,
                w.Firstname as WalkFName, w.Lastname as WalkLName,
                
                -- Reservation specifics
                res.ResDate, res.StartTime, res.EndTime, res.SportName as ResSport, res.PlayerNumber, res.ReservationStatusName,
                court.CourtNumber,
                
                -- Rental specifics
                rent.RentalDate, rent.ReturnedAt, rent.UnitPrice as RentPrice,
                emRental.EquipmentType as RentedItem, emRental.EquipmentSpec as RentedSpec, ei.ItemID as RentedItemNum,
                
                -- Consumable specifics
                cons.PurchaseDate, cons.Quantity as BoughtQty, cons.UnitPrice as BoughtPrice,
                emConsumable.EquipmentType as BoughtItem, emConsumable.EquipmentSpec as BoughtSpec
                
            FROM tblPayment pay
            LEFT JOIN tblPlayerAccount u ON pay.UserID = u.UserID
            LEFT JOIN tblPlayerWalkIn w ON pay.WalkInID = w.WalkInID
            
            LEFT JOIN tblReservation res ON pay.ReservationID = res.ReservationID
            LEFT JOIN tblCourt court ON res.CourtID = court.CourtID
            
            LEFT JOIN tblRental rent ON pay.RentalID = rent.RentalID
            LEFT JOIN tblEquipmentItem ei ON rent.ItemID = ei.ItemID
            LEFT JOIN tblEquipmentModel emRental ON ei.ModelID = emRental.ModelID
            
            LEFT JOIN tblConsumable cons ON pay.ConsumableID = cons.ConsumableID
            LEFT JOIN tblEquipmentModel emConsumable ON cons.ModelID = emConsumable.ModelID
            
            WHERE pay.PaymentID = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", paymentId);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    StringBuilder sb = new StringBuilder();

                    // --- 1. GENERAL TRANSACTION INFO ---
                    sb.Append("<div class='modal-section-title'>Transaction Overview</div>");
                    sb.Append($"<div class='detail-row'><span class='detail-label'>Reference #</span><span class='detail-value'>{dr["PaymentID"]}</span></div>");

                    // Handle Date
                    string payDate = dr["PaymentDate"] != DBNull.Value ? Convert.ToDateTime(dr["PaymentDate"]).ToString("MMM dd, yyyy") : "N/A";
                    sb.Append($"<div class='detail-row'><span class='detail-label'>Date Recorded</span><span class='detail-value'>{payDate}</span></div>");
                    sb.Append($"<div class='detail-row'><span class='detail-label'>Category</span><span class='detail-value'>{dr["PaymentTypeName"]}</span></div>");


                    // --- 2. CUSTOMER INFO ---
                    sb.Append("<div class='modal-section-title'>Customer Details</div>");
                    if (dr["RegFName"] != DBNull.Value)
                    {
                        // It's a registered user
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Name</span><span class='detail-value'>{dr["RegFName"]} {dr["RegLName"]}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Account Type</span><span class='detail-value' style='color:#10b981;'>Registered Member</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Email</span><span class='detail-value'>{dr["Email"]}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Phone</span><span class='detail-value'>{(dr["PhoneNumber"] != DBNull.Value ? dr["PhoneNumber"] : "N/A")}</span></div>");
                    }
                    else if (dr["WalkFName"] != DBNull.Value)
                    {
                        // It's a walk-in
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Name</span><span class='detail-value'>{dr["WalkFName"]} {dr["WalkLName"]}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Account Type</span><span class='detail-value' style='color:#f59e0b;'>Walk-In Player</span></div>");
                    }
                    else
                    {
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Name</span><span class='detail-value'>Unknown / System</span></div>");
                    }


                    // --- 3. DYNAMIC SPECIFICS ---
                    // A. RESERVATIONS
                    if (dr["CourtNumber"] != DBNull.Value)
                    {
                        sb.Append("<div class='modal-section-title'>Reservation Specifics</div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Court</span><span class='detail-value'>Court {dr["CourtNumber"]} ({dr["ResSport"]})</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Play Date</span><span class='detail-value'>{Convert.ToDateTime(dr["ResDate"]).ToString("MMM dd, yyyy")}</span></div>");

                        // Formatting SQL TIME span to string
                        string startT = DateTime.Today.Add((TimeSpan)dr["StartTime"]).ToString("hh:mm tt");
                        string endT = DateTime.Today.Add((TimeSpan)dr["EndTime"]).ToString("hh:mm tt");

                        sb.Append($"<div class='detail-row'><span class='detail-label'>Time Slot</span><span class='detail-value'>{startT} - {endT}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Total Players</span><span class='detail-value'>{dr["PlayerNumber"]}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Status</span><span class='detail-value'>{dr["ReservationStatusName"]}</span></div>");
                    }

                    // B. RENTALS
                    if (dr["RentedItem"] != DBNull.Value)
                    {
                        sb.Append("<div class='modal-section-title'>Rental Specifics</div>");
                        string spec = dr["RentedSpec"] != DBNull.Value ? $" - {dr["RentedSpec"]}" : "";
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Item</span><span class='detail-value'>{dr["RentedItem"]}{spec} (ID: #{dr["RentedItemNum"]})</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Unit Price</span><span class='detail-value'>₱{Convert.ToDecimal(dr["RentPrice"]):N2}</span></div>");

                        string rentalStatus = dr["ReturnedAt"] != DBNull.Value ?
                            $"Returned at {Convert.ToDateTime(dr["ReturnedAt"]).ToString("hh:mm tt")}" :
                            "<span style='color:#e11d48;'>In Possession (Not Returned)</span>";

                        sb.Append($"<div class='detail-row'><span class='detail-label'>Status</span><span class='detail-value'>{rentalStatus}</span></div>");
                    }

                    // C. CONSUMABLES / SHOP
                    if (dr["BoughtItem"] != DBNull.Value)
                    {
                        sb.Append("<div class='modal-section-title'>Shop Purchase Specifics</div>");
                        string spec = dr["BoughtSpec"] != DBNull.Value ? $" - {dr["BoughtSpec"]}" : "";
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Item</span><span class='detail-value'>{dr["BoughtItem"]}{spec}</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Quantity</span><span class='detail-value'>{dr["BoughtQty"]}x</span></div>");
                        sb.Append($"<div class='detail-row'><span class='detail-label'>Unit Price</span><span class='detail-value'>₱{Convert.ToDecimal(dr["BoughtPrice"]):N2}</span></div>");
                    }


                    // --- 4. TOTAL FOOTER ---
                    decimal amount = Convert.ToDecimal(dr["Amount"]);
                    string amountColor = amount < 0 ? "#e11d48" : "#10b981";
                    sb.Append($"<div class='detail-row' style='margin-top:25px; border-top:2px dashed #cbd5e1; padding-top:15px; border-bottom:none;'><span class='detail-label' style='font-size:1rem; color:#1e3a8a;'>Total Amount</span><span class='detail-value' style='font-size:1.5rem; color:{amountColor};'>₱{amount:N2}</span></div>");

                    litModalContent.Text = sb.ToString();
                }
            }

            ScriptManager.RegisterStartupScript(this, GetType(), "LaunchModal", "showModal();", true);
        }

        private void VoidTransaction(int id)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                // Mirroring the amount as negative for the audit trail
                string sql = @"
                    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, Amount, PaymentDate)
                    SELECT 'VOID', UserID, WalkInID, (Amount * -1), GETDATE() 
                    FROM tblPayment WHERE PaymentID = @id";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        protected string GetTypeClass(string type)
        {
            switch (type)
            {
                case "Reservation": return "type-pill pill-res";
                case "PAYC": return "type-pill pill-payc";
                case "Rental": return "type-pill pill-rental";
                case "Consumable": return "type-pill pill-rental";
                default: return "type-pill pill-event";
            }
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=SmashIt_AuditLog.csv");
            Response.ContentType = "text/csv";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Transaction ID,Date,Customer,Type,Amount");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT pay.PaymentID, pay.PaymentDate, 
                                COALESCE(u.Firstname, 'Walk-In') as Cust, pay.PaymentTypeName, 
                                pay.Amount 
                                FROM tblPayment pay LEFT JOIN tblPlayerAccount u ON pay.UserID = u.UserID";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    sb.AppendLine($"{dr[0]},{dr[1]},{dr[2]},{dr[3]},{dr[4]}");
                }
            }
            Response.Output.Write(sb.ToString());
            Response.Flush();
            Response.End();
        }
    }
}