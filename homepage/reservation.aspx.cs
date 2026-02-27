// reservation.aspx.cs (FULL rewrite — MATCHES YOUR DB + C# 4)
// DB assumptions based on your schema:
// - tblCourt(CourtID, CourtNumber, SportName, IsActive)
// - tblReservation(..., ResDate, StartTime, EndTime, ReservationStatusName, RequestStatus, IsPaid, PaymentStatus, RequiredAmount, PaymongoCheckoutSessionID)
// - tblEquipmentModel(ModelID, EquipmentType, EquipmentSpec, DefaultRentalPrice)
// - tblEquipmentItem(ItemID, ModelID)
// - tblRental(RentalID, ReservationID, ItemID, ReturnedAt, UnitPrice, IsPaid) + trigger sets UnitPrice
// - tblCourtQueue(StatusName, ReservationID, CourtID, QueueDate, ...)
// - tblEvent(EventDate, IsActive), tblEventCourtPool(EventID, CourtID)

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.UI;

namespace Smash_IT.homepage
{
    public partial class reservation : Page
    {
        private string CS
        {
            get { return ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString; }
        }

        // ===== Helpers: Parse Date/Time (handles "language"/culture issues) =====
        private static bool TryParseDateFlexible(string input, out DateTime date)
        {
            date = DateTime.MinValue;
            input = (input ?? "").Trim();
            if (input.Length == 0) return false;

            string[] formats = new string[]
            {
                "yyyy-MM-dd",
                "MM/dd/yyyy",
                "M/d/yyyy",
                "dd/MM/yyyy",
                "d/M/yyyy"
            };

            // Try exact formats first
            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;

            // Fallback: current culture parse
            return DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }

        private static bool TryParseTimeFlexible(string input, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            input = (input ?? "").Trim();
            if (input.Length == 0) return false;

            // Most of your JS should send "HH:mm"
            string[] formats = new string[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };

            if (TimeSpan.TryParseExact(input, formats, CultureInfo.InvariantCulture, out time))
                return true;

            // Fallback: allow "8:00 PM" etc.
            DateTime dt;
            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            {
                time = dt.TimeOfDay;
                return true;
            }

            return false;
        }

        private static int ToCentavos(decimal pesos)
        {
            return (int)Math.Round(pesos * 100m, MidpointRounding.AwayFromZero);
        }

        private static string NormalizeSport(string sport)
        {
            sport = (sport ?? "").Trim().ToLowerInvariant();
            if (sport == "badminton" || sport == "pickleball") return sport;
            return "";
        }

        private string GetSelectedSport()
        {
            // your ddlSport likely contains badminton/pickleball
            string s = "";
            try
            {
                if (ddlSport != null && ddlSport.SelectedValue != null)
                    s = ddlSport.SelectedValue;
            }
            catch { }
            return NormalizeSport(s);
        }

        // ==================== RENTALS SUPPORT ====================

        private sealed class RentalCartLine
        {
            public string EquipmentType { get; set; }
            public string EquipmentSpec { get; set; }
            public int Quantity { get; set; }
        }

        public sealed class EquipmentAvailabilityRow
        {
            public string EquipmentType { get; set; }
            public string EquipmentSpec { get; set; }
            public decimal RentalPrice { get; set; }
            public int AvailableQty { get; set; }
        }

        // ==================== AJAX: EQUIPMENT AVAILABILITY ====================
        // Busy definition:
        // - tblRental.ReturnedAt IS NULL
        // - rental is linked to a reservation that overlaps chosen slot on same ResDate
        // - reservation is not Cancelled/Completed

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<EquipmentAvailabilityRow> GetEquipmentAvailability(string date, string startTime, int durationHours)
        {
            List<EquipmentAvailabilityRow> rows = new List<EquipmentAvailabilityRow>();
            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            DateTime d;
            TimeSpan s;

            bool hasDate = TryParseDateFlexible(date, out d);
            bool hasTime = TryParseTimeFlexible(startTime, out s);

            // If no slot info, just show "not currently rented"
            if (!hasDate || !hasTime)
            {
                string sqlSimple = @"
SELECT
  m.EquipmentType,
  ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,
  m.DefaultRentalPrice AS RentalPrice,
  SUM(CASE WHEN ar.ItemID IS NULL THEN 1 ELSE 0 END) AS AvailableQty
FROM tblEquipmentItem ei
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
LEFT JOIN tblRental ar ON ar.ItemID = ei.ItemID AND ar.ReturnedAt IS NULL
GROUP BY m.EquipmentType, ISNULL(m.EquipmentSpec,''), m.DefaultRentalPrice
ORDER BY m.EquipmentType, ISNULL(m.EquipmentSpec,'');";

                using (SqlConnection con = new SqlConnection(cs))
                using (SqlCommand cmd = new SqlCommand(sqlSimple, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            EquipmentAvailabilityRow r = new EquipmentAvailabilityRow();
                            r.EquipmentType = Convert.ToString(dr["EquipmentType"]);
                            r.EquipmentSpec = Convert.ToString(dr["EquipmentSpec"]);
                            r.RentalPrice = Convert.ToDecimal(dr["RentalPrice"]);
                            r.AvailableQty = Convert.ToInt32(dr["AvailableQty"]);
                            rows.Add(r);
                        }
                    }
                }

                return rows;
            }

            TimeSpan end = s.Add(TimeSpan.FromHours(durationHours <= 0 ? 1 : durationHours));

            string sqlSlot = @"
;WITH BusyItems AS (
  SELECT rntl.ItemID
  FROM tblRental rntl
  JOIN tblReservation res ON res.ReservationID = rntl.ReservationID
  WHERE rntl.ReturnedAt IS NULL
    AND res.ResDate = @ResDate
    AND res.ReservationStatusName NOT IN ('Cancelled','Completed')
    AND (@StartTime < res.EndTime AND @EndTime > res.StartTime)
)
SELECT
  m.EquipmentType,
  ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,
  m.DefaultRentalPrice AS RentalPrice,
  SUM(CASE WHEN b.ItemID IS NULL THEN 1 ELSE 0 END) AS AvailableQty
FROM tblEquipmentItem ei
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
LEFT JOIN BusyItems b ON b.ItemID = ei.ItemID
GROUP BY m.EquipmentType, ISNULL(m.EquipmentSpec,''), m.DefaultRentalPrice
ORDER BY m.EquipmentType, ISNULL(m.EquipmentSpec,'');";

            using (SqlConnection con2 = new SqlConnection(cs))
            using (SqlCommand cmd2 = new SqlCommand(sqlSlot, con2))
            {
                cmd2.Parameters.AddWithValue("@ResDate", d.Date);
                cmd2.Parameters.AddWithValue("@StartTime", s);
                cmd2.Parameters.AddWithValue("@EndTime", end);

                con2.Open();
                using (SqlDataReader dr = cmd2.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        EquipmentAvailabilityRow r = new EquipmentAvailabilityRow();
                        r.EquipmentType = Convert.ToString(dr["EquipmentType"]);
                        r.EquipmentSpec = Convert.ToString(dr["EquipmentSpec"]);
                        r.RentalPrice = Convert.ToDecimal(dr["RentalPrice"]);
                        r.AvailableQty = Convert.ToInt32(dr["AvailableQty"]);
                        rows.Add(r);
                    }
                }
            }

            return rows;
        }

        // ==================== RENTAL CART PARSE ====================
        // Expects JSON like: { "Racket||Yonex": 2, "Shuttlecock||": 1 }
        private List<RentalCartLine> ParseRentalCart(string rawJson)
        {
            List<RentalCartLine> lines = new List<RentalCartLine>();
            if (string.IsNullOrWhiteSpace(rawJson)) return lines;

            Dictionary<string, object> dict = null;
            try { dict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(rawJson); }
            catch { return lines; }

            foreach (KeyValuePair<string, object> kv in dict)
            {
                string key = (kv.Key ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                int qty;
                try { qty = Convert.ToInt32(kv.Value); }
                catch { continue; }

                if (qty <= 0) continue;

                string[] parts = key.Split(new string[] { "||" }, StringSplitOptions.None);
                string type = parts.Length > 0 ? (parts[0] ?? "").Trim() : "";
                string spec = parts.Length > 1 ? (parts[1] ?? "").Trim() : "";

                if (string.IsNullOrWhiteSpace(type)) continue;

                RentalCartLine line = new RentalCartLine();
                line.EquipmentType = type;
                line.EquipmentSpec = spec;
                line.Quantity = qty;
                lines.Add(line);
            }

            return lines;
        }

        // ==================== RENTAL PRICING ====================
        // Uses tblEquipmentModel.DefaultRentalPrice
        private int ComputeRentalsTotalCentavos(SqlConnection con, SqlTransaction tx, List<RentalCartLine> cartLines)
        {
            if (cartLines == null || cartLines.Count == 0) return 0;

            int total = 0;

            foreach (RentalCartLine line in cartLines)
            {
                decimal unitPricePesos;

                using (SqlCommand cmd = new SqlCommand(@"
SELECT DefaultRentalPrice
FROM tblEquipmentModel
WHERE EquipmentType = @Type
  AND ISNULL(EquipmentSpec,'') = @Spec;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmd.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");

                    object p = cmd.ExecuteScalar();
                    if (p == null || p == DBNull.Value)
                        throw new Exception("Unknown equipment model: " + line.EquipmentType + " " + (line.EquipmentSpec ?? ""));

                    unitPricePesos = Convert.ToDecimal(p);
                }

                int unitCentavos = ToCentavos(unitPricePesos);
                checked { total += unitCentavos * line.Quantity; }
            }

            return total;
        }

        // ==================== RENTAL ALLOCATION ====================
        // Inserts tblRental rows, relies on trigger trg_Rental_SetUnitPrice to set UnitPrice.
        // No equipment item status updates (your tblEquipmentItem has no Status column).
        private void CreateRentalRowsForReservation(
            SqlConnection con,
            SqlTransaction tx,
            int reservationId,
            DateTime resDate,
            TimeSpan start,
            TimeSpan end,
            List<RentalCartLine> cartLines)
        {
            if (cartLines == null || cartLines.Count == 0) return;

            foreach (RentalCartLine line in cartLines)
            {
                List<int> picked = new List<int>();

                using (SqlCommand cmdPick = new SqlCommand(@"
;WITH BusyItems AS (
  SELECT rntl.ItemID
  FROM tblRental rntl
  JOIN tblReservation res ON res.ReservationID = rntl.ReservationID
  WHERE rntl.ReturnedAt IS NULL
    AND res.ResDate = @ResDate
    AND res.ReservationStatusName NOT IN ('Cancelled','Completed')
    AND (@StartTime < res.EndTime AND @EndTime > res.StartTime)
)
SELECT TOP (@Qty) ei.ItemID
FROM tblEquipmentItem ei WITH (UPDLOCK, HOLDLOCK)
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
WHERE m.EquipmentType = @Type
  AND ISNULL(m.EquipmentSpec,'') = @Spec
  AND ei.ItemID NOT IN (SELECT ItemID FROM BusyItems)
ORDER BY ei.ItemID;", con, tx))
                {
                    cmdPick.Parameters.AddWithValue("@ResDate", resDate.Date);
                    cmdPick.Parameters.AddWithValue("@StartTime", start);
                    cmdPick.Parameters.AddWithValue("@EndTime", end);
                    cmdPick.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmdPick.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");
                    cmdPick.Parameters.AddWithValue("@Qty", line.Quantity);

                    using (SqlDataReader dr = cmdPick.ExecuteReader())
                    {
                        while (dr.Read())
                            picked.Add(Convert.ToInt32(dr["ItemID"]));
                    }
                }

                if (picked.Count < line.Quantity)
                {
                    throw new Exception(
                        "Not enough stock for " + line.EquipmentType + " " + (line.EquipmentSpec ?? "") +
                        ". Requested " + line.Quantity + ", available " + picked.Count + "."
                    );
                }

                foreach (int itemId in picked)
                {
                    using (SqlCommand cmdIns = new SqlCommand(@"
INSERT INTO tblRental (ReservationID, ItemID, IsPaid)
VALUES (@ReservationID, @ItemID, 0);", con, tx))
                    {
                        cmdIns.Parameters.AddWithValue("@ReservationID", reservationId);
                        cmdIns.Parameters.AddWithValue("@ItemID", itemId);
                        cmdIns.ExecuteNonQuery();
                    }
                }
            }
        }

        // ==================== PAGE LIFECYCLE ====================

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadCourtData();

            if (!IsPostBack)
            {
                if (Request.QueryString["reset"] == "1")
                {
                    hfSelectedCourtID.Value = "";
                    hfStartTime.Value = "";
                    hfEndTime.Value = "";
                    hfRentalCart.Value = "{}";

                    lblSelectedSlot.Text = "No slot selected.";
                    try { ddlSport.ClearSelection(); } catch { }
                    if (ddlDuration != null && ddlDuration.Items.FindByValue("1") != null)
                        ddlDuration.SelectedValue = "1";
                }

                if (Session["UserID"] != null)
                {
                    int userId = Convert.ToInt32(Session["UserID"]);

                    using (SqlConnection con = new SqlConnection(CS))
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT Firstname, Lastname, Email, PhoneNumber FROM tblPlayerAccount WHERE UserID=@ID", con))
                    {
                        cmd.Parameters.AddWithValue("@ID", userId);
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtFirstname.Text = Convert.ToString(dr["Firstname"]);
                                txtLastname.Text = Convert.ToString(dr["Lastname"]);
                                txtEmail.Text = Convert.ToString(dr["Email"]);
                                txtContact.Text = Convert.ToString(dr["PhoneNumber"]);
                                Session["Firstname"] = txtFirstname.Text;
                                Session["Lastname"] = txtLastname.Text;
                                Session["Email"] = txtEmail.Text;
                                Session["PhoneNumber"] = txtContact.Text;
                            }
                        }
                    }
                }

                Calendar1.VisibleDate = DateTime.Today;
                Calendar1.SelectedDates.Clear();
                Calendar1.SelectedDate = DateTime.MinValue;

                hfSelectedDate.Value = DateTime.Today.ToString("yyyy-MM-dd");
                hfResDate.Value = hfSelectedDate.Value;

                RenderTimeTable(DateTime.Today, GetSelectedSport());
            }
        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            DateTime selectedDate = Calendar1.SelectedDate;
            hfSelectedDate.Value = selectedDate.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            RenderTimeTable(selectedDate, GetSelectedSport());

            string query = @"
SELECT StartTime, EndTime
FROM tblReservation
WHERE ResDate = @ResDate
  AND ReservationStatusName NOT IN ('Cancelled','Completed');";

            using (SqlConnection conn = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ResDate", selectedDate.Date);
                conn.Open();

                List<string> unavailable = new List<string>();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TimeSpan start = (TimeSpan)reader["StartTime"];
                        TimeSpan end = (TimeSpan)reader["EndTime"];
                        unavailable.Add(string.Format("{0:hh\\:mm} - {1:hh\\:mm}", start, end));
                    }
                }

                lblUnavailableHours.Text = unavailable.Count > 0
                    ? string.Join(", ", unavailable.ToArray())
                    : "All hours are available";
            }

            // already rendered above, no need to render again
        }

        protected void ddlSport_SelectedIndexChanged(object sender, EventArgs e)
        {
            DateTime d;
            if (!TryParseDateFlexible(hfSelectedDate.Value, out d))
            {
                d = DateTime.Today;
                hfSelectedDate.Value = d.ToString("yyyy-MM-dd");
                hfResDate.Value = hfSelectedDate.Value;
            }

            LoadCourtData();
            RenderTimeTable(d, GetSelectedSport());
        }

        // ==================== COURT JSON LOADERS ====================

        protected void LoadCourtData()
        {
            hfCourts.Value = GetCourtsJson();
            hfQueues.Value = GetQueuesJson();
        }

        private string GetCourtsJson()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT CourtID, CourtNumber, SportName, IsActive FROM tblCourt WHERE IsActive = 1", con))
            {
                new SqlDataAdapter(cmd).Fill(dt);
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            return js.Serialize(DataTableToList(dt));
        }

        private string GetQueuesJson()
        {
            // Your DB: tblCourtQueue.StatusName
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT q.CourtID,
       q.StatusName,
       r.ResDate,
       r.StartTime,
       r.EndTime
FROM tblCourtQueue q
INNER JOIN tblReservation r ON q.ReservationID = r.ReservationID
WHERE q.StatusName NOT IN ('Cancelled','Completed');", con))
            {
                new SqlDataAdapter(cmd).Fill(dt);
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            return js.Serialize(DataTableToList(dt));
        }

        private static List<Dictionary<string, object>> DataTableToList(DataTable dt)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }
            return list;
        }

        // ==================== BOOKING SUBMIT (PayMongo checkout creation) ====================
        // RULES:
        // - Store RequiredAmount in tblReservation as: CourtFull (100%) + RentalsFull (100%)
        // - Charge in PayMongo only once: RentalsFull + CourtDeposit (50% of court)
        // - DO NOT insert tblPayment here
        // - DO NOT mark reservation as paid here
        protected void btnSubmitReservation_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // Validate inputs
            DateTime resDate;
            if (!TryParseDateFlexible((hfSelectedDate.Value ?? "").Trim(), out resDate))
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid date.");
                return;
            }

            int courtId;
            if (!int.TryParse((hfSelectedCourtID.Value ?? "").Trim(), out courtId) || courtId <= 0)
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid court.");
                return;
            }

            TimeSpan startTime;
            if (!TryParseTimeFlexible((hfStartTime.Value ?? "").Trim(), out startTime))
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid start time.");
                return;
            }

            int duration;
            if (!int.TryParse(ddlDuration.SelectedValue, out duration) || duration <= 0)
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid duration.");
                return;
            }

            TimeSpan endTime = startTime.Add(TimeSpan.FromHours(duration));
            int userId = Convert.ToInt32(Session["UserID"]);

            // Optional: ensure court exists and is active
            using (SqlConnection conCheck = new SqlConnection(CS))
            using (SqlCommand cmdCheck = new SqlCommand(
                "SELECT COUNT(*) FROM tblCourt WHERE CourtID=@CID AND IsActive=1", conCheck))
            {
                cmdCheck.Parameters.AddWithValue("@CID", courtId);
                conCheck.Open();
                int ok = Convert.ToInt32(cmdCheck.ExecuteScalar());
                if (ok <= 0)
                {
                    Response.StatusCode = 400;
                    Response.Write("Selected court is not active.");
                    return;
                }
            }

            // rentals cart (optional)
            string cartJson = (hfRentalCart.Value ?? "").Trim();
            List<RentalCartLine> cartLines = ParseRentalCart(cartJson);

            // Money (CENTAVOS)
            const int courtPricePerHourCentavos = 33000;
            int courtFull = checked(duration * courtPricePerHourCentavos);
            int courtDeposit = courtFull / 2;

            // Base URL
            string scheme = Request.Headers["X-Forwarded-Proto"];
            if (string.IsNullOrEmpty(scheme)) scheme = Request.Url.Scheme;

            string host = Request.Headers["X-Forwarded-Host"];
            if (string.IsNullOrEmpty(host)) host = Request.Url.Authority;

            string baseUrl = scheme + "://" + host;

            int reservationId = 0;
            int rentalsFull = 0;

            // Create reservation + allocate rentals (transaction)
            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // Prevent overlap on same court (basic protection)
                        using (SqlCommand cmdOverlap = new SqlCommand(@"
SELECT COUNT(*)
FROM tblReservation
WHERE CourtID=@CourtID
  AND ResDate=@ResDate
  AND ReservationStatusName IN ('Pending','Approved')
  AND (@StartTime < EndTime AND @EndTime > StartTime);", con, tx))
                        {
                            cmdOverlap.Parameters.AddWithValue("@CourtID", courtId);
                            cmdOverlap.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmdOverlap.Parameters.AddWithValue("@StartTime", startTime);
                            cmdOverlap.Parameters.AddWithValue("@EndTime", endTime);
                            int overlap = Convert.ToInt32(cmdOverlap.ExecuteScalar());
                            if (overlap > 0)
                                throw new Exception("That court/time is already reserved. Please choose another slot.");
                        }

                        // rentals 100%
                        rentalsFull = ComputeRentalsTotalCentavos(con, tx, cartLines);

                        // RequiredAmount stored = 100% court + 100% rentals
                        int requiredAmountStored = checked(courtFull + rentalsFull);

                        // Insert reservation (UNPAID)
                        using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO tblReservation
(UserID, CourtID, ResDate, StartTime, EndTime, ReservationStatusName, IsPaid, PaymentStatus, RequiredAmount)
VALUES
(@UserID, @CourtID, @ResDate, @StartTime, @EndTime, 'Pending', 0, 'Unpaid', @RequiredAmount);
SELECT SCOPE_IDENTITY();", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@CourtID", courtId);
                            cmd.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);
                            cmd.Parameters.AddWithValue("@RequiredAmount", requiredAmountStored);

                            reservationId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // allocate rentals
                        if (cartLines != null && cartLines.Count > 0)
                        {
                            CreateRentalRowsForReservation(con, tx, reservationId, resDate.Date, startTime, endTime, cartLines);
                        }

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Response.StatusCode = 500;
                        Response.Write("Reservation/Rental allocation error:<br/><pre>" + HttpUtility.HtmlEncode(ex.Message) + "</pre>");
                        return;
                    }
                }
            }

            // PayMongo charge NOW = rentals full + 50% court
            int paymongoChargeNow = checked(rentalsFull + courtDeposit);

            string successUrl = baseUrl + "/ReservationSuccess.aspx?resId=" + reservationId;

            object payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        description = "Court reservation #" + reservationId,
                        reference_number = "RES-" + reservationId,
                        success_url = successUrl,
                        send_email_receipt = true,
                        payment_method_types = new string[] { "gcash" },
                        show_line_items = false,
                        line_items = new object[]
                        {
                            new
                            {
                                name = "Reservation Payment",
                                description =
                                    "CourtID " + courtId + " • " + resDate.ToString("yyyy-MM-dd") +
                                    " • " + startTime.ToString(@"hh\:mm") + "-" + endTime.ToString(@"hh\:mm"),
                                amount = paymongoChargeNow,
                                currency = "PHP",
                                quantity = 1
                            }
                        },
                        metadata = new
                        {
                            reservation_id = reservationId.ToString(),
                            court_id = courtId.ToString(),
                            date = resDate.ToString("yyyy-MM-dd"),
                            start = startTime.ToString(@"hh\:mm"),
                            end = endTime.ToString(@"hh\:mm"),
                            court_full = courtFull.ToString(),
                            court_deposit = courtDeposit.ToString(),
                            rentals_full = rentalsFull.ToString(),
                            paymongo_charge_now = paymongoChargeNow.ToString(),
                            required_amount_stored = (courtFull + rentalsFull).ToString()
                        }
                    }
                }
            };

            string secretKey = ConfigurationManager.AppSettings["PaymongoSecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Response.StatusCode = 500;
                Response.Write("PaymongoSecretKey missing in Web.config AppSettings.");
                return;
            }

            string json = new JavaScriptSerializer().Serialize(payload);
            string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));

            string checkoutUrl = null;
            string checkoutSessionId = null;

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.paymongo.com/v1/checkout_sessions");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Headers["Authorization"] = "Basic " + basic;

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(json);
                }

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    string body = reader.ReadToEnd();
                    Dictionary<string, object> parsed = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(body);
                    Dictionary<string, object> data = (Dictionary<string, object>)parsed["data"];
                    Dictionary<string, object> attrs = (Dictionary<string, object>)data["attributes"];

                    checkoutUrl = Convert.ToString(attrs["checkout_url"]);
                    checkoutSessionId = Convert.ToString(data["id"]);
                }
            }
            catch (WebException ex)
            {
                string errBody = "";
                HttpWebResponse errResp = ex.Response as HttpWebResponse;
                if (errResp != null)
                {
                    using (StreamReader r = new StreamReader(errResp.GetResponseStream()))
                        errBody = r.ReadToEnd();
                }

                try
                {
                    File.WriteAllText(HttpContext.Current.Server.MapPath("~/App_Data/paymongo_create_checkout_error.txt"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + errBody);
                }
                catch { }

                Response.StatusCode = 500;
                Response.Write("PayMongo error creating checkout session:<br/><pre>" + HttpUtility.HtmlEncode(errBody) + "</pre>");
                return;
            }

            // Save checkout session ID on reservation
            using (SqlConnection con3 = new SqlConnection(CS))
            using (SqlCommand cmd3 = new SqlCommand(@"
UPDATE tblReservation
SET PaymongoCheckoutSessionID = @CSID
WHERE ReservationID = @RID;", con3))
            {
                cmd3.Parameters.AddWithValue("@CSID", checkoutSessionId ?? "");
                cmd3.Parameters.AddWithValue("@RID", reservationId);
                con3.Open();
                cmd3.ExecuteNonQuery();
            }

            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                Response.StatusCode = 500;
                Response.Write("PayMongo checkout_url was empty.");
                return;
            }

            Response.Redirect(checkoutUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ==================== FINALIZE PAYMENTS AFTER PAYMONGO CONFIRMS PAID ====================
        // Your tblPayment has NO PaymentStatus / ExternalPaymentID columns, so:
        // - Idempotency rule: if ANY payment exists for ReservationID, skip.
        public static void FinalizePaymentsAfterPaymongoPaid(int reservationId, int userId, int rentalsFullCentavos, int courtDepositCentavos)
        {
            if (reservationId <= 0) throw new ArgumentException("reservationId invalid");
            if (userId <= 0) throw new ArgumentException("userId invalid");

            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    using (SqlCommand chk = new SqlCommand("SELECT COUNT(*) FROM tblPayment WHERE ReservationID=@RID;", con, tx))
                    {
                        chk.Parameters.AddWithValue("@RID", reservationId);
                        int exists = Convert.ToInt32(chk.ExecuteScalar());
                        if (exists > 0)
                        {
                            tx.Commit();
                            return;
                        }
                    }

                    if (rentalsFullCentavos > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES ('Rental', @UserID, @RID, CAST(GETDATE() AS DATE), @Amt);", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            cmd.Parameters.AddWithValue("@Amt", rentalsFullCentavos);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand("UPDATE tblRental SET IsPaid=1 WHERE ReservationID=@RID;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmd2 = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES ('Reservation', @UserID, @RID, CAST(GETDATE() AS DATE), @Amt);", con, tx))
                    {
                        cmd2.Parameters.AddWithValue("@UserID", userId);
                        cmd2.Parameters.AddWithValue("@RID", reservationId);
                        cmd2.Parameters.AddWithValue("@Amt", courtDepositCentavos);
                        cmd2.ExecuteNonQuery();
                    }

                    using (SqlCommand upd = new SqlCommand(@"
UPDATE tblReservation
SET IsPaid=1, PaymentStatus='HalfPaid'
WHERE ReservationID=@RID;", con, tx))
                    {
                        upd.Parameters.AddWithValue("@RID", reservationId);
                        upd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
        }

        // ==================== TIMETABLE RENDER ====================
        // Sport rule:
        // - Sport is in tblCourt.SportName
        // - If ddlSport selected, we restrict reservable slots to courts of that sport.
        private void RenderTimeTable(DateTime date, string sport)
        {
            DataTable dt = GetGridData(date, sport);

            Dictionary<string, DataRow> map = new Dictionary<string, DataRow>();
            foreach (DataRow r in dt.Rows)
            {
                int courtId = Convert.ToInt32(r["CourtID"]);
                TimeSpan slot = (TimeSpan)r["SlotStart"];
                string key = courtId.ToString() + "|" + slot.ToString();
                map[key] = r;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<div class='table-responsive'>");
            sb.Append("<table class='timetable'>");
            sb.Append("<thead><tr>");
            sb.Append("<th class='time-col'>Time</th>");
            for (int c = 1; c <= 6; c++) sb.Append("<th>Court " + c + "</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            TimeSpan start = new TimeSpan(8, 0, 0);
            TimeSpan endLast = new TimeSpan(23, 30, 0);

            for (TimeSpan t = start; t <= endLast; t = t.Add(TimeSpan.FromMinutes(30)))
            {
                TimeSpan tEnd = t.Add(TimeSpan.FromMinutes(30));
                sb.Append("<tr>");
                sb.Append("<td class='time-col'>" + DateTime.Today.Add(t).ToString("hh:mm tt") + "</td>");

                for (int courtNum = 1; courtNum <= 6; courtNum++)
                {
                    int courtId = GetCourtIdByNumber(dt, courtNum);

                    // If court doesn't exist for this sport filter, show disabled
                    if (courtId <= 0)
                    {
                        sb.Append("<td><div class='slot blocked'>N/A</div></td>");
                        continue;
                    }

                    string k = courtId.ToString() + "|" + t.ToString();
                    bool blocked = false;
                    bool isQueue = false;

                    if (map.ContainsKey(k))
                    {
                        blocked = Convert.ToInt32(map[k]["IsReservedBlocked"]) == 1;
                        isQueue = Convert.ToInt32(map[k]["IsQueueCourt"]) == 1;
                    }

                    if (blocked)
                    {
                        sb.Append("<td><div class='slot blocked'>Blocked</div></td>");
                    }
                    else if (isQueue)
                    {
                        sb.Append("<td><div class='slot queue'>Queue</div></td>");
                    }
                    else
                    {
                        string dateStr = date.ToString("yyyy-MM-dd");
                        string startStr = ((int)t.TotalHours).ToString("D2") + ":" + t.Minutes.ToString("D2");
                        string endStr = ((int)tEnd.TotalHours).ToString("D2") + ":" + tEnd.Minutes.ToString("D2");

                        sb.Append("<td>");
                        sb.Append("<div class='slot reservable' " +
                                  "data-date='" + dateStr + "' data-court='" + courtId + "' data-courtnum='" + courtNum + "' " +
                                  "data-start='" + startStr + "' data-end='" + endStr + "'>Reserve</div>");
                        sb.Append("</td>");
                    }
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");

            phTimeTable.Controls.Clear();
            phTimeTable.Controls.Add(new LiteralControl(sb.ToString()));
        }

        // This now respects sport filter because dt is already filtered.
        // If a court number is not present in dt (because different sport), returns -1.
        private int GetCourtIdByNumber(DataTable dt, int courtNumber)
        {
            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt32(r["CourtNumber"]) == courtNumber)
                    return Convert.ToInt32(r["CourtID"]);
            }
            return -1;
        }

        private DataTable GetGridData(DateTime date, string sport)
        {
            sport = NormalizeSport(sport);

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = con;

                cmd.CommandText = @"
DECLARE @d date = @ResDate;

WITH Slots AS (
  SELECT CAST('08:00:00' AS time) AS SlotStart
  UNION ALL
  SELECT DATEADD(MINUTE, 30, SlotStart)
  FROM Slots
  WHERE SlotStart < '23:30:00'
),
Courts AS (
  SELECT CourtID, CourtNumber
  FROM tblCourt
  WHERE IsActive = 1
    AND CourtNumber BETWEEN 1 AND 6
    AND (@Sport = '' OR SportName = @Sport)
),
Res AS (
  SELECT CourtID, StartTime, EndTime
  FROM tblReservation
  WHERE ResDate = @d
    AND ReservationStatusName IN ('Pending','Approved')
),
Evt AS (
  SELECT TOP 1 EventID
  FROM tblEvent
  WHERE EventDate = @d AND IsActive = 1
),
EvtCourts AS (
  SELECT ecp.CourtID
  FROM tblEventCourtPool ecp
  JOIN Evt ON Evt.EventID = ecp.EventID
)
SELECT
  c.CourtID,
  c.CourtNumber,
  s.SlotStart,
  CASE WHEN EXISTS (
    SELECT 1 FROM Res r
    WHERE r.CourtID = c.CourtID
      AND s.SlotStart >= r.StartTime
      AND s.SlotStart <  r.EndTime
  ) THEN 1 ELSE 0 END AS IsReservedBlocked,
  CASE WHEN EXISTS (
    SELECT 1 FROM EvtCourts ec WHERE ec.CourtID = c.CourtID
  ) THEN 1 ELSE 0 END AS IsQueueCourt
FROM Courts c
CROSS JOIN Slots s
OPTION (MAXRECURSION 1000);";

                cmd.Parameters.AddWithValue("@ResDate", date.Date);
                cmd.Parameters.AddWithValue("@Sport", sport);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                return dt;
            }
        }

        protected void btnResetReservation_Click(object sender, EventArgs e)
        {
            try { ddlSport.ClearSelection(); } catch { }
            if (ddlSport != null && ddlSport.Items.Count > 0) ddlSport.SelectedIndex = 0;

            try { ddlDuration.ClearSelection(); } catch { }
            if (ddlDuration != null && ddlDuration.Items.FindByValue("1") != null)
                ddlDuration.SelectedValue = "1";
            else if (ddlDuration != null && ddlDuration.Items.Count > 0)
                ddlDuration.SelectedIndex = 0;

            txtFirstname.Text = "";
            txtLastname.Text = "";
            txtEmail.Text = "";
            txtContact.Text = "";
            lblSelectedSlot.Text = "No slot selected.";

            hfSelectedCourtID.Value = "";
            hfSelectedDate.Value = "";
            hfCourtID.Value = "";
            hfCourtNum.Value = "";
            hfResDate.Value = "";
            hfStartTime.Value = "";
            hfEndTime.Value = "";

            hfRentalCart.Value = "{}";
            hfRentalItems.Value = "";
            hfRentalStock.Value = "";

            Session.Remove("ReservationDraft");
            Session.Remove("RentalCart");

            phTimeTable.Controls.Clear();
            reservationSection.Style["display"] = "none";
            updReservation.Update();
        }
    }
}