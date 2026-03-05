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
using System.Web.UI.WebControls;

namespace Smash_IT.homepage
{
    public partial class reservation : Page
    {
        private string CS
        {
            get { return ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString; }
        }

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

            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
            return DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }

        private static bool TryParseTimeFlexible(string input, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            input = (input ?? "").Trim();
            if (input.Length == 0) return false;

            string[] formats = new string[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };

            if (TimeSpan.TryParseExact(input, formats, CultureInfo.InvariantCulture, out time))
                return true;

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

        // ==================== EQUIPMENT AVAILABILITY ====================
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

        // ==================== PAGE ====================
        protected void Page_Load(object sender, EventArgs e)
        {
            EnsureUserSessionInfo();
            LoadCourtData();

            if (!IsPostBack)
            {
                Calendar1.VisibleDate = DateTime.Today;
                Calendar1.SelectedDate = DateTime.Today;

                hfSelectedDate.Value = DateTime.Today.ToString("yyyy-MM-dd");
                hfResDate.Value = hfSelectedDate.Value;

                ClearSelectedSlot();

                RenderTimeTable(DateTime.Today, GetSelectedSport());
                UpdateUnavailableHours(DateTime.Today, GetSelectedSport());

                UpdateSelectionStatusLabel(DateTime.Today);
            }
        }

        private void UpdateSelectionStatusLabel(DateTime selectedDate)
        {
            string sport = GetSelectedSport();

            if (string.IsNullOrEmpty(sport))
            {
                lblSelectedSlot.Text = "No sport selected.";
                return;
            }

            if (string.IsNullOrEmpty(hfSelectedCourtID.Value) || string.IsNullOrEmpty(hfStartTime.Value))
            {
                lblSelectedSlot.Text = "No slot selected.";
                return;
            }

            lblSelectedSlot.Text = "Slot selected.";
        }

        protected void Calendar1_DayRender(object sender, DayRenderEventArgs e)
        {
            DateTime min = DateTime.Today;
            DateTime max = DateTime.Today.AddDays(14);

            if (e.Day.Date < min || e.Day.Date > max)
            {
                e.Day.IsSelectable = false;
                e.Cell.ForeColor = System.Drawing.Color.LightGray;
                e.Cell.BackColor = System.Drawing.ColorTranslator.FromHtml("#f5f5f5");
                e.Cell.ToolTip = "Reservations allowed only within 2 weeks.";
            }
        }
        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            DateTime selectedDate = Calendar1.SelectedDate;

            DateTime min = DateTime.Today;
            DateTime max = DateTime.Today.AddDays(14);

            if (selectedDate.Date < min || selectedDate.Date > max)
            {
                selectedDate = DateTime.Today;
                Calendar1.SelectedDate = selectedDate;
            }

            hfSelectedDate.Value = selectedDate.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            ClearSelectedSlot();

            string sport = GetSelectedSport();
            RenderTimeTable(selectedDate, sport);
            UpdateUnavailableHours(selectedDate, sport);
            UpdateSelectionStatusLabel(selectedDate);
        }

        protected void ddlSport_SelectedIndexChanged(object sender, EventArgs e)
        {
            DateTime d;
            if (!TryParseDateFlexible(hfSelectedDate.Value, out d))
                d = Calendar1.SelectedDate == DateTime.MinValue ? DateTime.Today : Calendar1.SelectedDate;

            hfSelectedDate.Value = d.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            ClearSelectedSlot();

            string sport = GetSelectedSport();

            LoadCourtData();
            RenderTimeTable(d, sport);
            UpdateUnavailableHours(d, sport);
            UpdateSelectionStatusLabel(d);
        }

        private void UpdateUnavailableHours(DateTime selectedDate, string sport)
        {
            sport = NormalizeSport(sport);

            if (string.IsNullOrEmpty(sport))
            {
                lblUnavailableHours.Text = "Select a sport to see unavailable hours.";
                return;
            }

            string query = @"
DECLARE @d date = @ResDate;

;WITH Courts AS (
    SELECT CourtID
    FROM tblCourt
    WHERE IsActive = 1
      AND (SportName = @Sport OR CourtNumber IN (5,6))
),
NonReservableSlots AS (
    SELECT a.StartTime, a.EndTime
    FROM tblCourtAvailability a
    JOIN Courts c ON c.CourtID = a.CourtID
    WHERE a.[Date] = @d
      AND a.ModeName NOT IN ('Reservation', 'PlayForAll')
),
ReservedSlots AS (
    SELECT a.StartTime, a.EndTime
    FROM tblCourtAvailability a
    JOIN Courts c ON c.CourtID = a.CourtID
    WHERE a.[Date] = @d
      AND a.ModeName IN ('Reservation', 'PlayForAll')
      AND EXISTS (
          SELECT 1
          FROM tblReservation r
          WHERE r.CourtID = a.CourtID
            AND r.ResDate = @d
            AND r.ReservationStatusName IN ('Approved','Pending')
            AND (a.StartTime < r.EndTime AND a.EndTime > r.StartTime)
      )
),
AllBad AS (
    SELECT StartTime, EndTime FROM NonReservableSlots
    UNION
    SELECT StartTime, EndTime FROM ReservedSlots
)
SELECT StartTime, EndTime
FROM AllBad
GROUP BY StartTime, EndTime
ORDER BY StartTime;";

            using (SqlConnection conn = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ResDate", selectedDate.Date);
                cmd.Parameters.AddWithValue("@Sport", sport);
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

        // ==================== BOOKING SUBMIT (PayMongo) ====================
        protected void btnSubmitReservation_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            DateTime resDate;
           

            if (!TryParseDateFlexible((hfSelectedDate.Value ?? "").Trim(), out resDate))
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid date.");
                return;
            }

            if (resDate.Date < DateTime.Today || resDate.Date > DateTime.Today.AddDays(14))
            {
                Response.StatusCode = 400;
                Response.Write("You can only reserve within 2 weeks from today.");
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

            string cartJson = (hfRentalCart.Value ?? "").Trim();
            List<RentalCartLine> cartLines = ParseRentalCart(cartJson);

            const int courtPricePerHourCentavos = 33000;
            int courtFull = checked(duration * courtPricePerHourCentavos);
            int courtDeposit = courtFull / 2;

            string scheme = Request.Headers["X-Forwarded-Proto"];
            if (string.IsNullOrEmpty(scheme)) scheme = Request.Url.Scheme;

            string host = Request.Headers["X-Forwarded-Host"];
            if (string.IsNullOrEmpty(host)) host = Request.Url.Authority;

            string baseUrl = scheme + "://" + host;

            int reservationId = 0;
            int rentalsFull = 0;

            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {

                        using (SqlCommand cmdMode = new SqlCommand(@"
                        IF EXISTS (
                            SELECT 1
                            FROM tblCourtAvailability a
                            WHERE a.CourtID = @CourtID
                              AND a.[Date]  = @ResDate
                              AND a.StartTime < @EndTime
                              AND a.EndTime   > @StartTime
                             AND a.ModeName NOT IN ('Reservation', 'PlayForAll')
                        )
                            SELECT 1;
                        ELSE
                            SELECT 0;", con, tx))
                        {
                            cmdMode.Parameters.AddWithValue("@CourtID", courtId);
                            cmdMode.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmdMode.Parameters.AddWithValue("@StartTime", startTime);
                            cmdMode.Parameters.AddWithValue("@EndTime", endTime);

                            int notReservable = Convert.ToInt32(cmdMode.ExecuteScalar());
                            if (notReservable == 1)
                                throw new Exception("That time is not reservable (Queue / PlayForAll / Closed).");
                        }
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

                        rentalsFull = ComputeRentalsTotalCentavos(con, tx, cartLines);

                        int requiredAmountStored = checked(courtFull + rentalsFull);

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

        // ==================== TIMETABLE RENDER 


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

                    var slotDateTime = date.Date.Add(t);
                    bool isPastSlot = (date.Date < DateTime.Today)
                                      || (date.Date == DateTime.Today && slotDateTime <= DateTime.Now);

                    if (isPastSlot)
                    {
                        // blank white cell (no text)
                        sb.Append("<td><div class='slot past'></div></td>");
                        continue;
                    }


                    if (courtId <= 0)
                    {
                        sb.Append("<td><div class='slot past'></div></td>");
                        continue;
                    }

                    string k = courtId.ToString() + "|" + t.ToString();
                    bool blocked = true;       // default blocked if no row exists
                    bool isQueue = false;
                    bool isClosed = false;
                    bool isPfa = false;
                    bool isReservable = false;

                    if (map.ContainsKey(k))
                    {
                        blocked = Convert.ToInt32(map[k]["IsReservedBlocked"]) == 1;
                        isQueue = Convert.ToInt32(map[k]["IsQueueCourt"]) == 1;
                        isClosed = Convert.ToInt32(map[k]["IsClosed"]) == 1;
                        isPfa = Convert.ToInt32(map[k]["IsPlayForAll"]) == 1;
                        isReservable = Convert.ToInt32(map[k]["IsReservable"]) == 1;
                    }
                    else
                    {
                        // No availability row = treat as Closed/Blocked
                        blocked = true;
                    }

                    if (isClosed)
                    {
                        sb.Append("<td><div class='slot blocked'>Closed</div></td>");
                    }
                    else if (isPfa)
                    {
                        string dateStr = date.ToString("yyyy-MM-dd");
                        string startStr = t.ToString(@"hh\:mm"); // 24h "HH:mm"

                        sb.Append("<td>");
                        sb.Append("<div class='slot reservable available' " +
                                  "data-court='" + courtId + "' " +
                                  "data-courtnum='" + courtNum + "' " +
                                  "data-date='" + dateStr + "' " +
                                  "data-start='" + startStr + "'>");
                        sb.Append("PFA");
                        sb.Append("</div>");
                        sb.Append("</td>");
                    }
                    else if (isQueue)
                    {
                        sb.Append("<td><div class='slot queue'>Queue</div></td>");
                    }
                    else if (blocked)
                    {
                        sb.Append("<td><div class='slot blocked'>Reserved</div></td>");
                    }
                    else if (!isReservable)
                    {
                        sb.Append("<td><div class='slot blocked'>N/A</div></td>");
                    }
                    else
                    {
                        string dateStr = date.ToString("yyyy-MM-dd");
                        string startStr = t.ToString(@"hh\:mm"); // 24h "HH:mm"

                        sb.Append("<td>");
                        sb.Append("<div class='slot reservable available' " +
                                  "data-court='" + courtId + "' " +
                                  "data-courtnum='" + courtNum + "' " +
                                  "data-date='" + dateStr + "' " +
                                  "data-start='" + startStr + "'>");
                        sb.Append("Available");
                        sb.Append("</div>");
                        sb.Append("</td>");
                    }

                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");

            phTimeTable.Controls.Clear();
            phTimeTable.Controls.Add(new LiteralControl(sb.ToString()));
        }

        private int GetCourtIdByNumber(DataTable dt, int courtNumber)
        {
            if (dt == null || dt.Rows.Count == 0) return 0;

            DataRow[] rows = dt.Select("CourtNumber = " + courtNumber);
            if (rows.Length == 0) return 0;

            return Convert.ToInt32(rows[0]["CourtID"]);
        }

        private DataTable GetGridData(DateTime date, string sport)
        {
            sport = NormalizeSport(sport);

            // ✅ if no sport selected, return empty = timetable shows disabled only
            if (string.IsNullOrEmpty(sport))
                return new DataTable();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = con;
                cmd.CommandText = @"
                DECLARE @d date = @ResDate;

                SELECT
                    c.CourtID,
                    c.CourtNumber,
                    a.StartTime AS SlotStart,
                    CASE WHEN EXISTS (
                        SELECT 1
                        FROM tblReservation r
                        WHERE r.CourtID = c.CourtID
                          AND r.ResDate = @d
                          AND r.ReservationStatusName IN ('Approved','Pending')
                          AND (a.StartTime < r.EndTime AND a.EndTime > r.StartTime)
                    ) THEN 1 ELSE 0 END AS IsReservedBlocked,

                    CASE WHEN a.ModeName = 'Queue' THEN 1 ELSE 0 END AS IsQueueCourt,
                    CASE WHEN a.ModeName = 'Closed' THEN 1 ELSE 0 END AS IsClosed,
                    CASE WHEN a.ModeName = 'PlayForAll' THEN 1 ELSE 0 END AS IsPlayForAll,
                    CASE WHEN a.ModeName = 'Reservation' THEN 1 ELSE 0 END AS IsReservable
                FROM tblCourtAvailability a
                JOIN tblCourt c ON c.CourtID = a.CourtID
                WHERE a.[Date] = @d
                  AND c.IsActive = 1
                  AND c.CourtNumber BETWEEN 1 AND 6
                  AND (c.SportName = @SportName OR c.CourtNumber IN (5,6))
                ORDER BY c.CourtNumber, a.StartTime;";

                cmd.Parameters.AddWithValue("@ResDate", date.Date);
                cmd.Parameters.AddWithValue("@SportName", sport);

                DataTable dt = new DataTable();
                new SqlDataAdapter(cmd).Fill(dt);
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

            DateTime d = DateTime.Today;
            Calendar1.VisibleDate = d;
            Calendar1.SelectedDate = d;

            hfSelectedDate.Value = d.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            ClearSelectedSlot();

            hfRentalCart.Value = "{}";
            hfRentalItems.Value = "";
            hfRentalStock.Value = "";

            Session.Remove("ReservationDraft");
            Session.Remove("RentalCart");

            string sport = GetSelectedSport(); 
            RenderTimeTable(d, sport);
            UpdateUnavailableHours(d, sport);
            UpdateSelectionStatusLabel(d);

            reservationSection.Style["display"] = "none";

            updReservation.Update();
        }


        private void ClearSelectedSlot()
        {
            hfSelectedCourtID.Value = "";
            hfCourtID.Value = "";
            hfCourtNum.Value = "";
            hfStartTime.Value = "";
            hfEndTime.Value = "";

            if (string.IsNullOrEmpty(GetSelectedSport()))
                lblSelectedSlot.Text = "No sport selected.";
            else
                lblSelectedSlot.Text = "No slot selected.";
        }


        private void EnsureUserSessionInfo()
        {
            if (Session["UserID"] == null) return;

            if (Session["Firstname"] != null && Session["Lastname"] != null &&
                Session["Email"] != null && Session["PhoneNumber"] != null)
                return;

            int userId = Convert.ToInt32(Session["UserID"]);

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
        SELECT Firstname, Lastname, Email, PhoneNumber
        FROM tblUser
        WHERE UserID = @UserID;", con))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                con.Open();

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        Session["Firstname"] = Convert.ToString(dr["Firstname"]);
                        Session["Lastname"] = Convert.ToString(dr["Lastname"]);
                        Session["Email"] = Convert.ToString(dr["Email"]);
                        Session["PhoneNumber"] = Convert.ToString(dr["PhoneNumber"]);
                    }
                }
            }
        }
    }
}