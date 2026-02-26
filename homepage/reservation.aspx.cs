// reservation.aspx.cs (FULL rewrite to match the new Step flow)
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
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
        // -------------------- RENTALS SUPPORT --------------------

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

        private static int ToCentavos(decimal pesos)
        {
            return (int)Math.Round(pesos * 100m, MidpointRounding.AwayFromZero);
        }

        private static readonly string[] ReservationConnectionStringCandidates =
        {
            "smashit",
            "SmashIT",
            "SmashITDb",
            "DefaultConnection",
            "soapergandahannali"
        };

        private static string GetReservationConnectionString()
        {
            foreach (var name in ReservationConnectionStringCandidates)
            {
                var setting = ConfigurationManager.ConnectionStrings[name];
                if (setting != null && !string.IsNullOrWhiteSpace(setting.ConnectionString))
                    return setting.ConnectionString;
            }

            throw new ConfigurationErrorsException(
                "No valid database connection string found for reservation flow. " +
                "Expected one of: " + string.Join(", ", ReservationConnectionStringCandidates));
        }


        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<EquipmentAvailabilityRow> GetEquipmentAvailability(string date, string startTime, int durationHours)
        {
            var rows = new List<EquipmentAvailabilityRow>();
            var cs = GetReservationConnectionString();

            DateTime d;
            TimeSpan s;

            // If date/startTime missing, return simple stock grouped by Type+Spec
            if (!DateTime.TryParseExact(date ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d) ||
                !TimeSpan.TryParseExact(startTime ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out s))
            {
                var sqlSimple = @"
SELECT
  EquipmentType,
  ISNULL(EquipmentSpec,'') AS EquipmentSpec,
  MAX(RentalPrice) AS RentalPrice,
  COUNT(*) AS AvailableQty
FROM tblEquipmentItem
WHERE Status = 'Available'
GROUP BY EquipmentType, ISNULL(EquipmentSpec,'')
ORDER BY EquipmentType, ISNULL(EquipmentSpec,'');";

                using (var con = new SqlConnection(cs))
                using (var cmd = new SqlCommand(sqlSimple, con))
                {
                    con.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            rows.Add(new EquipmentAvailabilityRow
                            {
                                EquipmentType = dr["EquipmentType"].ToString(),
                                EquipmentSpec = dr["EquipmentSpec"].ToString(),
                                RentalPrice = Convert.ToDecimal(dr["RentalPrice"]),
                                AvailableQty = Convert.ToInt32(dr["AvailableQty"])
                            });
                        }
                    }
                }
                return rows;
            }

            var end = s.Add(TimeSpan.FromHours(durationHours <= 0 ? 1 : durationHours));

            var sql = @"
;WITH ConflictedItems AS (
  SELECT rntl.ItemID
  FROM tblRental rntl
  INNER JOIN tblReservation res ON res.ReservationID = rntl.ReservationID
  WHERE res.ResDate = @ResDate
    AND res.Status NOT IN ('Cancelled','Completed')
    AND (@StartTime < res.EndTime AND @EndTime > res.StartTime)
    AND rntl.ReturnedAt IS NULL
)
SELECT
  ei.EquipmentType,
  ISNULL(ei.EquipmentSpec,'') AS EquipmentSpec,
  MAX(ei.RentalPrice) AS RentalPrice,
  COUNT(*) AS AvailableQty
FROM tblEquipmentItem ei
WHERE ei.Status = 'Available'
  AND ei.ItemID NOT IN (SELECT ItemID FROM ConflictedItems)
GROUP BY ei.EquipmentType, ISNULL(ei.EquipmentSpec,'')
ORDER BY ei.EquipmentType, ISNULL(ei.EquipmentSpec,'');";

            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@ResDate", d.Date);
                cmd.Parameters.AddWithValue("@StartTime", s);
                cmd.Parameters.AddWithValue("@EndTime", end);

                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        rows.Add(new EquipmentAvailabilityRow
                        {
                            EquipmentType = dr["EquipmentType"].ToString(),
                            EquipmentSpec = dr["EquipmentSpec"].ToString(),
                            RentalPrice = Convert.ToDecimal(dr["RentalPrice"]),
                            AvailableQty = Convert.ToInt32(dr["AvailableQty"])
                        });
                    }
                }
            }

            return rows;
        }

        private List<RentalCartLine> ParseRentalCart(string rawJson)
        {
            var lines = new List<RentalCartLine>();
            if (string.IsNullOrWhiteSpace(rawJson)) return lines;

            Dictionary<string, object> dict;
            try { dict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(rawJson); }
            catch { return lines; }

            foreach (var kv in dict)
            {
                var key = (kv.Key ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                int qty;
                try { qty = Convert.ToInt32(kv.Value); }
                catch { continue; }
                if (qty <= 0) continue;

                var parts = key.Split(new[] { "||" }, StringSplitOptions.None);
                var type = parts.Length > 0 ? parts[0].Trim() : "";
                var spec = parts.Length > 1 ? parts[1].Trim() : "";

                if (string.IsNullOrWhiteSpace(type)) continue;

                lines.Add(new RentalCartLine { EquipmentType = type, EquipmentSpec = spec, Quantity = qty });
            }

            return lines;
        }

        private (List<int> RentalIDs, List<(string DisplayName, decimal UnitPrice, int Qty)> Summary)
          CreateRentalRowsForReservation(
            SqlConnection con,
            SqlTransaction tx,
            int reservationId,
            DateTime resDate,
            TimeSpan start,
            TimeSpan end,
            List<RentalCartLine> cartLines)
        {
            var rentalIds = new List<int>();
            var summary = new List<(string DisplayName, decimal UnitPrice, int Qty)>();

            if (cartLines == null || cartLines.Count == 0)
                return (rentalIds, summary);

            foreach (var line in cartLines)
            {
                decimal unitPrice;
                using (var cmdPrice = new SqlCommand(@"
SELECT TOP 1 RentalPrice
FROM tblEquipmentItem
WHERE EquipmentType = @Type
  AND ISNULL(EquipmentSpec,'') = @Spec
ORDER BY ItemID;", con, tx))
                {
                    cmdPrice.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmdPrice.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");
                    var p = cmdPrice.ExecuteScalar();
                    if (p == null || p == DBNull.Value)
                        throw new Exception("Unknown equipment: " + line.EquipmentType + " " + line.EquipmentSpec);
                    unitPrice = Convert.ToDecimal(p);
                }

                var picked = new List<int>();
                using (var cmdPick = new SqlCommand(@"
;WITH ConflictedItems AS (
  SELECT rntl.ItemID
  FROM tblRental rntl
  INNER JOIN tblReservation res ON res.ReservationID = rntl.ReservationID
  WHERE res.ResDate = @ResDate
    AND res.Status NOT IN ('Cancelled','Completed')
    AND (@StartTime < res.EndTime AND @EndTime > res.StartTime)
    AND rntl.ReturnedAt IS NULL
)
SELECT TOP (@Qty) ei.ItemID
FROM tblEquipmentItem ei WITH (UPDLOCK, HOLDLOCK)
WHERE ei.EquipmentType = @Type
  AND ISNULL(ei.EquipmentSpec,'') = @Spec
  AND ei.Status = 'Available'
  AND ei.ItemID NOT IN (SELECT ItemID FROM ConflictedItems)
ORDER BY ei.ItemID;", con, tx))
                {
                    cmdPick.Parameters.AddWithValue("@ResDate", resDate.Date);
                    cmdPick.Parameters.AddWithValue("@StartTime", start);
                    cmdPick.Parameters.AddWithValue("@EndTime", end);
                    cmdPick.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmdPick.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");
                    cmdPick.Parameters.AddWithValue("@Qty", line.Quantity);

                    using (var dr = cmdPick.ExecuteReader())
                        while (dr.Read()) picked.Add(Convert.ToInt32(dr["ItemID"]));
                }

                if (picked.Count < line.Quantity)
                    throw new Exception($"Not enough stock for {line.EquipmentType} {line.EquipmentSpec}. Requested {line.Quantity}, available {picked.Count}.");

                foreach (var itemId in picked)
                {
                    int rentalId;
                    using (var cmdIns = new SqlCommand(@"
INSERT INTO tblRental (ReservationID, ItemID, IsPaid)
VALUES (@ReservationID, @ItemID, 0);
SELECT SCOPE_IDENTITY();", con, tx))
                    {
                        cmdIns.Parameters.AddWithValue("@ReservationID", reservationId);
                        cmdIns.Parameters.AddWithValue("@ItemID", itemId);
                        rentalId = Convert.ToInt32(cmdIns.ExecuteScalar());
                    }
                    rentalIds.Add(rentalId);

                    using (var cmdUpd = new SqlCommand(@"
UPDATE tblEquipmentItem
SET Status = 'Rented'
WHERE ItemID = @ItemID;", con, tx))
                    {
                        cmdUpd.Parameters.AddWithValue("@ItemID", itemId);
                        cmdUpd.ExecuteNonQuery();
                    }
                }

                var display = string.IsNullOrWhiteSpace(line.EquipmentSpec)
                  ? line.EquipmentType
                  : $"{line.EquipmentType} ({line.EquipmentSpec})";

                summary.Add((display, unitPrice, line.Quantity));
            }

            return (rentalIds, summary);
        }

        // -------------------- PAGE LIFECYCLE --------------------

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadCourtData();

            if (!IsPostBack)
            {
                if (Request.QueryString["reset"] == "1")
                {
                    // clear server-side fields so user can book again clean
                    hfSelectedCourtID.Value = "";
                    hfStartTime.Value = "";
                    hfEndTime.Value = "";
                    hfRentalCart.Value = "{}";

                    // optional: clear these too if you want totally blank date
                    // hfSelectedDate.Value = "";
                    // hfResDate.Value = "";

                    // clear UI labels if you have them
                    lblSelectedSlot.Text = "No slot selected.";

                    // also clear dropdowns if needed
                    ddlSport.ClearSelection();
                    ddlDuration.SelectedValue = "1";
                }


                if (Session["UserID"] != null)
                {
                    int userId = Convert.ToInt32(Session["UserID"]);
                    using (SqlConnection con = new SqlConnection(GetReservationConnectionString()))
                    using (SqlCommand cmd = new SqlCommand("SELECT FullName, Email, PhoneNumber FROM tblPlayerAccount WHERE UserID=@ID", con))
                    {
                        cmd.Parameters.AddWithValue("@ID", userId);
                        con.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                txtFName.Text = dr["FullName"].ToString();
                                txtEmail.Text = dr["Email"].ToString();
                                txtContact.Text = dr["PhoneNumber"].ToString();

                                Session["FName"] = dr["FullName"].ToString();
                                Session["Email"] = dr["Email"].ToString();
                                Session["PhoneNumber"] = dr["PhoneNumber"].ToString();
                            }
                        }
                    }
                }

                // default: render timetable for today so Step 1 isn't empty
                Calendar1.VisibleDate = DateTime.Today;
                Calendar1.SelectedDates.Clear();
                Calendar1.SelectedDate = DateTime.MinValue;

                // ✅ Keep date as today (so timetable shows), even after reset
                hfSelectedDate.Value = DateTime.Today.ToString("yyyy-MM-dd");
                hfResDate.Value = hfSelectedDate.Value;

                RenderTimeTable(DateTime.Today);
            }
        }

        protected void Calendar1_SelectionChanged(object sender, EventArgs e)
        {
            var selectedDate = Calendar1.SelectedDate;

            hfSelectedDate.Value = selectedDate.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            RenderTimeTable(selectedDate);
            string query = @"
SELECT StartTime, EndTime
FROM tblReservation
WHERE ResDate = @ResDate
  AND Status NOT IN ('Cancelled','Completed')";

            using (SqlConnection conn = new SqlConnection(GetReservationConnectionString()))
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

                lblUnavailableHours.Text = unavailable.Count > 0 ? string.Join(", ", unavailable) : "All hours are available";
            }

            RenderTimeTable(selectedDate);
        }

        protected void LoadCourtData()
        {
            hfCourts.Value = GetCourtsJson();
            hfQueues.Value = GetQueuesJson();
        }

        private string GetCourtsJson()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(GetReservationConnectionString()))
            using (SqlCommand cmd = new SqlCommand("SELECT CourtID, CourtNumber, Sport, IsActive FROM tblCourt WHERE IsActive = 1", con))
            {
                new SqlDataAdapter(cmd).Fill(dt);
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            return js.Serialize(dt.AsEnumerable().Select(r =>
              dt.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c])
            ));
        }

        private string GetQueuesJson()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(GetReservationConnectionString()))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT q.CourtID, q.Status, r.ResDate, r.StartTime, r.EndTime
FROM tblCourtQueue q
INNER JOIN tblReservation r ON q.ReservationID = r.ReservationID
WHERE q.Status NOT IN ('Cancelled','Done')", con))
            {
                new SqlDataAdapter(cmd).Fill(dt);
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            return js.Serialize(dt.AsEnumerable().Select(r =>
              dt.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => r[c])
            ));
        }

        // -------------------- BOOKING SUBMIT (PayMongo) --------------------
        protected void btnSubmitReservation_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // -------- Validate inputs --------
            string rawDate = (hfSelectedDate.Value ?? "").Trim();
            if (!DateTime.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime resDate))
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid date.");
                return;
            }

            if (!int.TryParse((hfSelectedCourtID.Value ?? "").Trim(), out int courtId) || courtId <= 0)
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid court.");
                return;
            }

            string rawStart = (hfStartTime.Value ?? "").Trim();
            if (!TimeSpan.TryParseExact(rawStart, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan startTime))
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid start time.");
                return;
            }

            if (!int.TryParse(ddlDuration.SelectedValue, out int duration) || duration <= 0)
            {
                Response.StatusCode = 400;
                Response.Write("Missing/invalid duration.");
                return;
            }

            TimeSpan endTime = startTime.Add(TimeSpan.FromHours(duration));
            int userId = Convert.ToInt32(Session["UserID"]);

            // rentals cart (optional)
            var cartJson = (hfRentalCart.Value ?? "").Trim();
            var cartLines = ParseRentalCart(cartJson);

            // -------- Money (TOTAL ONLY) --------
            const int courtPricePerHourCentavos = 33000;
            int courtAmount = checked(duration * courtPricePerHourCentavos);

            int rentalsTotal = 0;

            // -------- Base URL --------
            string scheme = Request.Headers["X-Forwarded-Proto"];
            if (string.IsNullOrEmpty(scheme)) scheme = Request.Url.Scheme;

            string host = Request.Headers["X-Forwarded-Host"];
            if (string.IsNullOrEmpty(host)) host = Request.Url.Authority;

            string baseUrl = scheme + "://" + host;

            var cs = GetReservationConnectionString();

            int reservationId = 0;
            int rentalId = 0;

            // -------- Create reservation + allocate rentals (transaction) --------
            using (var con = new SqlConnection(cs))
            {
                con.Open();
                using (var tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblReservation (UserID, CourtID, ResDate, StartTime, EndTime, Status, IsPaid, PaymentStatus)
VALUES (@UserID, @CourtID, @ResDate, @StartTime, @EndTime, 'Pending', 1, 'Paid');
SELECT SCOPE_IDENTITY();", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@CourtID", courtId);
                            cmd.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmd.Parameters.AddWithValue("@StartTime", startTime);
                            cmd.Parameters.AddWithValue("@EndTime", endTime);

                            reservationId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Allocate rentals and compute rentals subtotal in centavos
                        if (cartLines.Count > 0)
                        {
                            var result = CreateRentalRowsForReservation(con, tx, reservationId, resDate.Date, startTime, endTime, cartLines);
                            rentalId = result.RentalIDs.Count > 0 ? result.RentalIDs[0] : 0;

                            foreach (var rs in result.Summary)
                            {
                                int qty = rs.Qty;
                                if (qty <= 0) throw new Exception("Invalid qty for " + rs.DisplayName);

                                // rs.UnitPrice is decimal PESOS (e.g. 100.00)
                                int unitCentavos = (int)Math.Round(rs.UnitPrice * 100m, MidpointRounding.AwayFromZero);
                                if (unitCentavos < 1) throw new Exception("Invalid unit price for " + rs.DisplayName + ": " + rs.UnitPrice);

                                checked { rentalsTotal += unitCentavos * qty; }
                            }
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

            int grandTotal = checked(courtAmount + rentalsTotal);

            string successUrl = baseUrl + "/ReservationSuccess.aspx?resId=" + reservationId;

            var payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        description = "Court reservation #" + reservationId,
                        reference_number = "RES-" + reservationId,
                        success_url = successUrl,
                        send_email_receipt = true,
                        payment_method_types = new[] { "gcash" },

                        // ✅ REQUIRED: 1 line item only (TOTAL ONLY)
                        show_line_items = false, // hides the line item on checkout UI
                        line_items = new[]
             {
                new
                {
                    name = "Reservation Total",
                    description =
                        "CourtID " + courtId + " • " + resDate.ToString("yyyy-MM-dd") +
                        " • " + startTime.ToString(@"hh\:mm") + "-" + endTime.ToString(@"hh\:mm"),
                    amount = grandTotal,   // ✅ centavos
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
                            rental_id = rentalId > 0 ? rentalId.ToString() : "",
                            court_amount = courtAmount.ToString(),
                            rentals_amount = rentalsTotal.ToString(),
                            total_amount = grandTotal.ToString()
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

            string body = null;
            string checkoutUrl = null;
            string checkoutSessionId = null;

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.paymongo.com/v1/checkout_sessions");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Headers["Authorization"] = "Basic " + basic;

                using (var sw = new StreamWriter(req.GetRequestStream()))
                    sw.Write(json);

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream()))
                    body = reader.ReadToEnd();

                var parsed = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(body);
                var data = (Dictionary<string, object>)parsed["data"];
                var attrs = (Dictionary<string, object>)data["attributes"];

                checkoutUrl = attrs["checkout_url"].ToString();
                checkoutSessionId = data["id"].ToString();
            }
            catch (WebException ex)
            {
                string errBody = "";
                var errResp = ex.Response as HttpWebResponse;
                if (errResp != null)
                {
                    using (var reader = new StreamReader(errResp.GetResponseStream()))
                        errBody = reader.ReadToEnd();
                }

                try
                {
                    File.WriteAllText(Server.MapPath("~/App_Data/paymongo_create_checkout_error.txt"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + errBody);
                }
                catch { }

                Response.StatusCode = 500;
                Response.Write("PayMongo error creating checkout session:<br/><pre>" + HttpUtility.HtmlEncode(errBody) + "</pre>");
                return;
            }

            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE tblReservation
SET PaymongoCheckoutSessionID = @CSID
WHERE ReservationID = @RID", con))
            {
                cmd.Parameters.AddWithValue("@CSID", checkoutSessionId);
                cmd.Parameters.AddWithValue("@RID", reservationId);
                con.Open();
                cmd.ExecuteNonQuery();
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
        // -------------------- TIMETABLE RENDER --------------------
        private void RenderTimeTable(DateTime date)
        {
            var dt = GetGridData(date);

            var map = new Dictionary<string, DataRow>();
            foreach (DataRow r in dt.Rows)
            {
                int courtId = Convert.ToInt32(r["CourtID"]);
                TimeSpan slot = (TimeSpan)r["SlotStart"];
                map[$"{courtId}|{slot}"] = r;
            }

            var sb = new StringBuilder();
            sb.Append("<div class='table-responsive'>");
            sb.Append("<table class='timetable'>");
            sb.Append("<thead><tr>");
            sb.Append("<th class='time-col'>Time</th>");
            for (int c = 1; c <= 6; c++) sb.Append($"<th>Court {c}</th>");
            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            TimeSpan start = new TimeSpan(8, 0, 0);
            TimeSpan endLast = new TimeSpan(23, 30, 0);

            for (TimeSpan t = start; t <= endLast; t = t.Add(TimeSpan.FromMinutes(30)))
            {
                TimeSpan tEnd = t.Add(TimeSpan.FromMinutes(30));
                sb.Append("<tr>");
                sb.Append($"<td class='time-col'>{DateTime.Today.Add(t):hh:mm tt}</td>");

                for (int courtNum = 1; courtNum <= 6; courtNum++)
                {
                    int courtId = GetCourtIdByNumber(dt, courtNum);

                    string key = $"{courtId}|{t}";
                    bool blocked = false;
                    bool isQueue = false;

                    if (map.ContainsKey(key))
                    {
                        blocked = Convert.ToInt32(map[key]["IsReservedBlocked"]) == 1;
                        isQueue = Convert.ToInt32(map[key]["IsQueueCourt"]) == 1;
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
                        string startStr = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}";
                        string endStr = $"{(int)tEnd.TotalHours:D2}:{tEnd.Minutes:D2}";

                        sb.Append("<td>");
                        sb.Append($"<div class='slot reservable' " +
                                  $"data-date='{dateStr}' data-court='{courtId}' data-courtnum='{courtNum}' " +
                                  $"data-start='{startStr}' data-end='{endStr}'>Reserve</div>");
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
            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt32(r["CourtNumber"]) == courtNumber)
                    return Convert.ToInt32(r["CourtID"]);
            }
            return courtNumber;
        }

        private DataTable GetGridData(DateTime date)
        {
            string cs = GetReservationConnectionString();

            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand())
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
  WHERE IsActive = 1 AND CourtNumber BETWEEN 1 AND 6
),
Res AS (
  SELECT CourtID, StartTime, EndTime
  FROM tblReservation
  WHERE ResDate = @d
    AND Status IN ('Pending','Approved')
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

                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                return dt;
            }
        }

        protected void ddlSport_SelectedIndexChanged(object sender, EventArgs e)
        {
            // keep the currently selected date
            DateTime d;
            if (!DateTime.TryParseExact(hfSelectedDate.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out d))
            {
                d = DateTime.Today;
                hfSelectedDate.Value = d.ToString("yyyy-MM-dd");
                hfResDate.Value = hfSelectedDate.Value;
            }

            RenderTimeTable(d); // OR RenderTimeTable(d, ddlSport.SelectedValue) if you implement filtering
            LoadCourtData();    // if courts json depends on sport
        }

        protected void btnResetReservation_Click(object sender, EventArgs e)
        {
            // dropdowns
            ddlSport.ClearSelection();
            ddlSport.SelectedIndex = 0;

            ddlDuration.ClearSelection();
            if (ddlDuration.Items.FindByValue("1") != null)
                ddlDuration.SelectedValue = "1";
            else
                ddlDuration.SelectedIndex = 0;

            // textboxes
            txtFName.Text = "";
            txtEmail.Text = "";
            txtContact.Text = "";

            // label
            lblSelectedSlot.Text = "No slot selected.";

            // hidden fields
            hfSelectedCourtID.Value = "";
            hfSelectedDate.Value = "";
            hfCourtID.Value = "";
            hfCourtNum.Value = "";
            hfResDate.Value = "";
            hfStartTime.Value = "";
            hfEndTime.Value = "";

            hfRentalCart.Value = "";
            hfRentalItems.Value = "";
            hfRentalStock.Value = "";

            // session cleanup (optional)
            Session.Remove("ReservationDraft");
            Session.Remove("RentalCart");

            // clear timetable
            phTimeTable.Controls.Clear();

            // hide reservation section (server-side)
            reservationSection.Style["display"] = "none";

            // updatepanel refresh (only needed if the button is inside the updatepanel)
            updReservation.Update();
        }

    }
}