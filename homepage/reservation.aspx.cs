using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.SessionState;

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

        private DateTime? LastSelectedDate
        {
            get
            {
                object o = ViewState["LastSelectedDate"];
                if (o == null) return null;
                return (DateTime)o;
            }
            set
            {
                ViewState["LastSelectedDate"] = value;
            }
        }

        private sealed class RentalCartLine
        {
            public string EquipmentType { get; set; }
            public string EquipmentSpec { get; set; }
            public int Quantity { get; set; }
        }

        private sealed class ConsumableCartLine
        {
            public string EquipmentType { get; set; }
            public string EquipmentSpec { get; set; }
            public int Quantity { get; set; }
        }

        public sealed class EquipmentAvailabilityRow
        {
            public string ItemCategory { get; set; }
            public string EquipmentType { get; set; }
            public string EquipmentSpec { get; set; }
            public decimal UnitPrice { get; set; }
            public int AvailableQty { get; set; }
        }

        public sealed class PendingReservationDraft
        {
            public string DraftToken { get; set; }
            public int UserID { get; set; }
            public int CourtID { get; set; }
            public string SportName { get; set; }
            public string ResDate { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public int Duration { get; set; }

            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Email { get; set; }
            public string Contact { get; set; }
            public int Players { get; set; }

            public string RentalCartJson { get; set; }
            public string ConsumableCartJson { get; set; }

            public decimal CourtFullPesos { get; set; }
            public decimal CourtDepositPesos { get; set; }
            public decimal RentalsFullPesos { get; set; }
            public decimal ConsumablesFullPesos { get; set; }
            public decimal PaymongoChargeNowPesos { get; set; }
            public decimal RequiredAmountStoredPesos { get; set; }
        }


        private const string PendingReservationCookieName = "PendingReservationBackup";

        private static string ProtectString(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            byte[] bytes = Encoding.UTF8.GetBytes(plain);
            byte[] protectedBytes = MachineKey.Protect(bytes, "PendingReservationBackup");
            return Convert.ToBase64String(protectedBytes);
        }

        private static string UnprotectString(string protectedBase64)
        {
            if (string.IsNullOrWhiteSpace(protectedBase64)) return "";

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(protectedBase64);
                byte[] bytes = MachineKey.Unprotect(protectedBytes, "PendingReservationBackup");
                if (bytes == null || bytes.Length == 0) return "";
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        private void SavePendingReservationBackupCookie(PendingReservationDraft draft, string checkoutSessionId)
        {
            var payload = new Dictionary<string, object>
    {
        { "Draft", draft },
        { "CheckoutSessionId", checkoutSessionId ?? "" }
    };

            string json = new JavaScriptSerializer().Serialize(payload);
            string protectedValue = ProtectString(json);

            HttpCookie cookie = new HttpCookie(PendingReservationCookieName, protectedValue);
            cookie.HttpOnly = true;
            cookie.Secure = false;
            cookie.Path = "/";
            cookie.Expires = DateTime.Now.AddHours(2);

            Response.Cookies.Set(cookie);
        }

        public static PendingReservationDraft GetPendingDraftFromCookie(HttpRequest request, string token)
        {
            if (request == null) return null;

            HttpCookie cookie = request.Cookies[PendingReservationCookieName];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value)) return null;

            string json = UnprotectString(cookie.Value);
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var payload = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                if (payload == null || !payload.ContainsKey("Draft")) return null;

                string payloadJson = new JavaScriptSerializer().Serialize(payload["Draft"]);
                PendingReservationDraft draft =
                    new JavaScriptSerializer().Deserialize<PendingReservationDraft>(payloadJson);

                if (draft == null) return null;
                if (!string.Equals(draft.DraftToken ?? "", token ?? "", StringComparison.Ordinal))
                    return null;

                return draft;
            }
            catch
            {
                return null;
            }
        }

        public static string GetPendingCheckoutSessionIdFromCookie(HttpRequest request)
        {
            if (request == null) return "";

            HttpCookie cookie = request.Cookies[PendingReservationCookieName];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value)) return "";

            string json = UnprotectString(cookie.Value);
            if (string.IsNullOrWhiteSpace(json)) return "";

            try
            {
                var payload2 = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                if (payload2 == null || !payload2.ContainsKey("CheckoutSessionId")) return "";

                return Convert.ToString(payload2["CheckoutSessionId"] ?? "");
            }
            catch
            {
                return "";
            }
        }

        protected void ClearPendingReservationBackupCookie()
        {
            HttpCookie cookie = new HttpCookie(PendingReservationCookieName, "");
            cookie.HttpOnly = true;
            cookie.Secure = false;
            cookie.Path = "/";
            cookie.Expires = DateTime.Now.AddDays(-1);

            Response.Cookies.Set(cookie);
        }


        public static PendingReservationDraft GetPendingDraftFromSession(HttpSessionState session, string token)
        {
            if (session == null) return null;

            string raw = Convert.ToString(session["PendingReservationDraft"] ?? "");
            if (string.IsNullOrWhiteSpace(raw)) return null;

            PendingReservationDraft draft;
            try
            {
                draft = new JavaScriptSerializer().Deserialize<PendingReservationDraft>(raw);
            }
            catch
            {
                return null;
            }

            if (draft == null) return null;
            if (!string.Equals(draft.DraftToken ?? "", token ?? "", StringComparison.Ordinal))
                return null;

            return draft;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            EnsureUserSessionInfo();
            LoadCourtData();

            if (!IsPostBack)
            {
                Calendar1.VisibleDate = DateTime.Today;
                Calendar1.SelectedDates.Clear();

                hfSelectedDate.Value = "";
                hfResDate.Value = "";

                try { ddlSport.ClearSelection(); } catch { }
                if (ddlSport != null && ddlSport.Items.Count > 0)
                    ddlSport.SelectedIndex = 0;

                hfSelectedSport.Value = "";

                ClearSelectedSlot();
                phTimeTable.Controls.Clear();
                lblUnavailableHours.Text = "Select a date and sport to see unavailable hours.";
                UpdateSelectionStatusLabel();
            }
        }

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

            string sql;

            if (!hasDate || !hasTime)
            {
                sql = @"
SELECT
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,
    CASE
        WHEN m.ItemCategory = 'Rental' THEN m.DefaultRentalPrice
        ELSE m.DefaultSellPrice
    END AS UnitPrice,
    CASE
        WHEN m.ItemCategory = 'Rental' THEN
            SUM(CASE
                    WHEN ei.ItemID IS NOT NULL AND r.ItemID IS NULL THEN 1
                    ELSE 0
                END)
        ELSE ISNULL(m.ConsumableQty, 0)
    END AS AvailableQty
FROM tblEquipmentModel m
LEFT JOIN tblEquipmentItem ei
    ON ei.ModelID = m.ModelID
LEFT JOIN tblRental r
    ON r.ItemID = ei.ItemID
   AND r.ReturnedAt IS NULL
GROUP BY
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,''),
    m.DefaultRentalPrice,
    m.DefaultSellPrice,
    m.ConsumableQty
ORDER BY
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'');";

                using (SqlConnection con = new SqlConnection(cs))
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            rows.Add(new EquipmentAvailabilityRow
                            {
                                ItemCategory = Convert.ToString(dr["ItemCategory"]),
                                EquipmentType = Convert.ToString(dr["EquipmentType"]),
                                EquipmentSpec = Convert.ToString(dr["EquipmentSpec"]),
                                UnitPrice = Convert.ToDecimal(dr["UnitPrice"]),
                                AvailableQty = Convert.ToInt32(dr["AvailableQty"])
                            });
                        }
                    }
                }

                return rows;
            }

            TimeSpan end = s.Add(TimeSpan.FromHours(durationHours <= 0 ? 1 : durationHours));
            sql = @"
SELECT
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'') AS EquipmentSpec,
    CASE
        WHEN m.ItemCategory = 'Rental' THEN m.DefaultRentalPrice
        ELSE m.DefaultSellPrice
    END AS UnitPrice,
    CASE
        WHEN m.ItemCategory = 'Rental' THEN
            SUM(CASE
                    WHEN ei.ItemID IS NOT NULL AND r.ItemID IS NULL THEN 1
                    ELSE 0
                END)
        ELSE ISNULL(m.ConsumableQty, 0)
    END AS AvailableQty
FROM tblEquipmentModel m
LEFT JOIN tblEquipmentItem ei
    ON ei.ModelID = m.ModelID
LEFT JOIN tblRental r
    ON r.ItemID = ei.ItemID
   AND r.ReturnedAt IS NULL
GROUP BY
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,''),
    m.DefaultRentalPrice,
    m.DefaultSellPrice,
    m.ConsumableQty
ORDER BY
    m.ItemCategory,
    m.EquipmentType,
    ISNULL(m.EquipmentSpec,'');";

            using (SqlConnection con = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                con.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        rows.Add(new EquipmentAvailabilityRow
                        {
                            ItemCategory = Convert.ToString(dr["ItemCategory"]),
                            EquipmentType = Convert.ToString(dr["EquipmentType"]),
                            EquipmentSpec = Convert.ToString(dr["EquipmentSpec"]),
                            UnitPrice = Convert.ToDecimal(dr["UnitPrice"]),
                            AvailableQty = Convert.ToInt32(dr["AvailableQty"])
                        });
                    }
                }
            }

            return rows;
        }

        private List<RentalCartLine> ParseRentalCart(string rawJson)
        {
            List<RentalCartLine> lines = new List<RentalCartLine>();
            if (string.IsNullOrWhiteSpace(rawJson)) return lines;

            Dictionary<string, object> dict = null;
            try
            {
                dict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(rawJson);
            }
            catch
            {
                return lines;
            }

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

                lines.Add(new RentalCartLine
                {
                    EquipmentType = type,
                    EquipmentSpec = spec,
                    Quantity = qty
                });
            }

            return lines;
        }

        private List<ConsumableCartLine> ParseConsumableCart(string rawJson)
        {
            List<ConsumableCartLine> lines = new List<ConsumableCartLine>();
            if (string.IsNullOrWhiteSpace(rawJson)) return lines;

            Dictionary<string, object> dict = null;
            try
            {
                dict = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(rawJson);
            }
            catch
            {
                return lines;
            }

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

                lines.Add(new ConsumableCartLine
                {
                    EquipmentType = type,
                    EquipmentSpec = spec,
                    Quantity = qty
                });
            }

            return lines;
        }

        private decimal ComputeRentalsTotalPesos(SqlConnection con, SqlTransaction tx, List<RentalCartLine> cartLines)
        {
            decimal total = 0m;
            if (cartLines == null || cartLines.Count == 0) return total;

            foreach (RentalCartLine line in cartLines)
            {
                using (SqlCommand cmd = new SqlCommand(@"
SELECT DefaultRentalPrice
FROM tblEquipmentModel
WHERE ItemCategory = 'Rental'
  AND EquipmentType = @Type
  AND ISNULL(EquipmentSpec,'') = @Spec;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmd.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");

                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                        throw new Exception("Unknown rental item: " + line.EquipmentType);

                    total += Convert.ToDecimal(val) * line.Quantity;
                }
            }

            return total;
        }

        private decimal ComputeConsumablesTotalPesos(SqlConnection con, SqlTransaction tx, List<ConsumableCartLine> cartLines)
        {
            decimal total = 0m;
            if (cartLines == null || cartLines.Count == 0) return total;

            foreach (ConsumableCartLine line in cartLines)
            {
                using (SqlCommand cmd = new SqlCommand(@"
SELECT DefaultSellPrice
FROM tblEquipmentModel
WHERE ItemCategory = 'Consumable'
  AND EquipmentType = @Type
  AND ISNULL(EquipmentSpec,'') = @Spec;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmd.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");

                    object val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value)
                        throw new Exception("Unknown consumable item: " + line.EquipmentType);

                    total += Convert.ToDecimal(val) * line.Quantity;
                }
            }

            return total;
        }

        private void CreateRentalRowsForReservation(
            SqlConnection con,
            SqlTransaction tx,
            int reservationId,
            int userId,
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
SELECT TOP (@Qty) ei.ItemID
FROM tblEquipmentItem ei WITH (UPDLOCK, HOLDLOCK)
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
WHERE m.ItemCategory = 'Rental'
  AND m.EquipmentType = @Type
  AND ISNULL(m.EquipmentSpec,'') = @Spec
  AND NOT EXISTS (
      SELECT 1
      FROM tblRental rntl
      WHERE rntl.ItemID = ei.ItemID
        AND rntl.ReturnedAt IS NULL
  )
ORDER BY ei.ItemID;", con, tx))
                {
      
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
                    throw new Exception("Not enough rental stock for " + line.EquipmentType + ".");

                foreach (int itemId in picked)
                {
                    using (SqlCommand cmdIns = new SqlCommand(@"
INSERT INTO tblRental (UserID, ReservationID, ItemID, IsPaid)
VALUES (@UserID, @ReservationID, @ItemID, 0);", con, tx))
                    {
                        cmdIns.Parameters.AddWithValue("@UserID", userId);
                        cmdIns.Parameters.AddWithValue("@ReservationID", reservationId);
                        cmdIns.Parameters.AddWithValue("@ItemID", itemId);
                        cmdIns.ExecuteNonQuery();
                    }
                }
            }
        }

        private void CreateConsumableRowsForReservation(
            SqlConnection con,
            SqlTransaction tx,
            int reservationId,
            int userId,
            List<ConsumableCartLine> cartLines)
        {
            if (cartLines == null || cartLines.Count == 0) return;

            foreach (ConsumableCartLine line in cartLines)
            {
                int modelId;
                decimal unitPrice;
                int stockQty;

                using (SqlCommand cmd = new SqlCommand(@"
SELECT ModelID, DefaultSellPrice, ConsumableQty
FROM tblEquipmentModel WITH (UPDLOCK, HOLDLOCK)
WHERE ItemCategory = 'Consumable'
  AND EquipmentType = @Type
  AND ISNULL(EquipmentSpec,'') = @Spec;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@Type", line.EquipmentType);
                    cmd.Parameters.AddWithValue("@Spec", line.EquipmentSpec ?? "");

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read())
                            throw new Exception("Consumable not found: " + line.EquipmentType);

                        modelId = Convert.ToInt32(dr["ModelID"]);
                        unitPrice = Convert.ToDecimal(dr["DefaultSellPrice"]);
                        stockQty = Convert.ToInt32(dr["ConsumableQty"]);
                    }
                }

                if (stockQty < line.Quantity)
                    throw new Exception("Not enough stock for consumable: " + line.EquipmentType);

                using (SqlCommand cmdUpd = new SqlCommand(@"
UPDATE tblEquipmentModel
SET ConsumableQty = ConsumableQty - @Qty
WHERE ModelID = @ModelID;", con, tx))
                {
                    cmdUpd.Parameters.AddWithValue("@Qty", line.Quantity);
                    cmdUpd.Parameters.AddWithValue("@ModelID", modelId);
                    cmdUpd.ExecuteNonQuery();
                }

                using (SqlCommand cmdIns = new SqlCommand(@"
INSERT INTO tblConsumable
(UserID, ReservationID, ModelID, Quantity, UnitPrice, IsPaid, PurchaseDate)
VALUES
(@UserID, @ReservationID, @ModelID, @Quantity, @UnitPrice, 0, GETDATE());", con, tx))
                {
                    cmdIns.Parameters.AddWithValue("@UserID", userId);
                    cmdIns.Parameters.AddWithValue("@ReservationID", reservationId);
                    cmdIns.Parameters.AddWithValue("@ModelID", modelId);
                    cmdIns.Parameters.AddWithValue("@Quantity", line.Quantity);
                    cmdIns.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    cmdIns.ExecuteNonQuery();
                }
            }
        }

        public int FinalizeReservationAfterSuccessfulPayment(PendingReservationDraft draft, string checkoutSessionId)
        {
            if (draft == null) throw new ArgumentNullException("draft");

            DateTime resDate;
            TimeSpan startTime;
            TimeSpan endTime;

            if (!TryParseDateFlexible(draft.ResDate, out resDate))
                throw new Exception("Draft date invalid.");

            if (!TryParseTimeFlexible(draft.StartTime, out startTime))
                throw new Exception("Draft start time invalid.");

            if (!TryParseTimeFlexible(draft.EndTime, out endTime))
                throw new Exception("Draft end time invalid.");

            List<RentalCartLine> rentalLines = ParseRentalCart(draft.RentalCartJson ?? "{}");
            List<ConsumableCartLine> consumableLines = ParseConsumableCart(draft.ConsumableCartJson ?? "{}");

            int reservationId = 0;

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
      AND a.[Date] = @ResDate
      AND a.StartTime < @EndTime
      AND a.EndTime > @StartTime
      AND a.ModeName NOT IN ('Reservation', 'PlayForAll')
)
    SELECT 1;
ELSE
    SELECT 0;", con, tx))
                        {
                            cmdMode.Parameters.AddWithValue("@CourtID", draft.CourtID);
                            cmdMode.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmdMode.Parameters.AddWithValue("@StartTime", startTime);
                            cmdMode.Parameters.AddWithValue("@EndTime", endTime);

                            int notReservable = Convert.ToInt32(cmdMode.ExecuteScalar());
                            if (notReservable == 1)
                                throw new Exception("That time is no longer reservable.");
                        }

                        using (SqlCommand cmdOverlap = new SqlCommand(@"
SELECT COUNT(*)
FROM tblReservation
WHERE CourtID = @CourtID
  AND ResDate = @ResDate
  AND ReservationStatusName IN ('Pending','Approved')
  AND (@StartTime < EndTime AND @EndTime > StartTime);", con, tx))
                        {
                            cmdOverlap.Parameters.AddWithValue("@CourtID", draft.CourtID);
                            cmdOverlap.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmdOverlap.Parameters.AddWithValue("@StartTime", startTime);
                            cmdOverlap.Parameters.AddWithValue("@EndTime", endTime);

                            int overlap = Convert.ToInt32(cmdOverlap.ExecuteScalar());
                            if (overlap > 0)
                                throw new Exception("That slot was taken while payment was in progress.");
                        }

                        using (SqlCommand cmdInsRes = new SqlCommand(@"
INSERT INTO tblReservation
(UserID, CourtID, ResDate, StartTime, EndTime, SportName, ReservationStatusName, IsPaid, PaymentStatus, RequiredAmount, PaymongoCheckoutSessionID)
VALUES
(@UserID, @CourtID, @ResDate, @StartTime, @EndTime, @SportName, 'Pending', 1, 'HalfPaid', @RequiredAmount, @CSID);
SELECT SCOPE_IDENTITY();", con, tx))
                        {
                            cmdInsRes.Parameters.AddWithValue("@UserID", draft.UserID);
                            cmdInsRes.Parameters.AddWithValue("@CourtID", draft.CourtID);
                            cmdInsRes.Parameters.AddWithValue("@ResDate", resDate.Date);
                            cmdInsRes.Parameters.AddWithValue("@StartTime", startTime);
                            cmdInsRes.Parameters.AddWithValue("@EndTime", endTime);
                            cmdInsRes.Parameters.AddWithValue("@SportName", draft.SportName ?? "");
                            cmdInsRes.Parameters.AddWithValue("@RequiredAmount", draft.RequiredAmountStoredPesos);
                            cmdInsRes.Parameters.AddWithValue("@CSID", checkoutSessionId ?? "");

                            reservationId = Convert.ToInt32(cmdInsRes.ExecuteScalar());
                        }

                        CreateRentalRowsForReservation(con, tx, reservationId, draft.UserID, resDate.Date, startTime, endTime, rentalLines);
                        CreateConsumableRowsForReservation(con, tx, reservationId, draft.UserID, consumableLines);

                        if (draft.RentalsFullPesos > 0m)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES ('Rental', @UserID, @RID, CAST(GETDATE() AS DATE), @Amt);", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@UserID", draft.UserID);
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.Parameters.AddWithValue("@Amt", draft.RentalsFullPesos);
                                cmd.ExecuteNonQuery();
                            }

                            using (SqlCommand cmd = new SqlCommand("UPDATE tblRental SET IsPaid=1 WHERE ReservationID=@RID;", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (draft.ConsumablesFullPesos > 0m)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES ('Consumable', @UserID, @RID, CAST(GETDATE() AS DATE), @Amt);", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@UserID", draft.UserID);
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.Parameters.AddWithValue("@Amt", draft.ConsumablesFullPesos);
                                cmd.ExecuteNonQuery();
                            }

                            using (SqlCommand cmd = new SqlCommand("UPDATE tblConsumable SET IsPaid=1 WHERE ReservationID=@RID;", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand cmd2 = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES ('Reservation', @UserID, @RID, CAST(GETDATE() AS DATE), @Amt);", con, tx))
                        {
                            cmd2.Parameters.AddWithValue("@UserID", draft.UserID);
                            cmd2.Parameters.AddWithValue("@RID", reservationId);
                            cmd2.Parameters.AddWithValue("@Amt", draft.CourtDepositPesos);
                            cmd2.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }

            return reservationId;
        }

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

            string selectedSport = GetSelectedSport();
            if (string.IsNullOrWhiteSpace(selectedSport))
            {
                Response.StatusCode = 400;
                Response.Write("Please select a sport.");
                return;
            }

            TimeSpan endTime = startTime.Add(TimeSpan.FromHours(duration));
            int userId = Convert.ToInt32(Session["UserID"]);

            using (SqlConnection conCheck = new SqlConnection(CS))
            using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tblCourt WHERE CourtID=@CID AND IsActive=1", conCheck))
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

            string rentalCartJson = string.IsNullOrWhiteSpace(hfRentalCart.Value) ? "{}" : hfRentalCart.Value.Trim();
            string consumableCartJson = string.IsNullOrWhiteSpace(hfConsumableCart.Value) ? "{}" : hfConsumableCart.Value.Trim();

            List<RentalCartLine> rentalLines = ParseRentalCart(rentalCartJson);
            List<ConsumableCartLine> consumableLines = ParseConsumableCart(consumableCartJson);

            decimal courtPricePerHour = 330.00m;
            decimal courtFullPesos = duration * courtPricePerHour;
            decimal courtDepositPesos = courtFullPesos * 0.50m;
            decimal rentalsFullPesos = 0m;
            decimal consumablesFullPesos = 0m;
            decimal requiredAmountStoredPesos = 0m;

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
      AND a.[Date] = @ResDate
      AND a.StartTime < @EndTime
      AND a.EndTime > @StartTime
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
WHERE CourtID = @CourtID
  AND ResDate = @ResDate
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

                        rentalsFullPesos = ComputeRentalsTotalPesos(con, tx, rentalLines);
                        consumablesFullPesos = ComputeConsumablesTotalPesos(con, tx, consumableLines);
                        requiredAmountStoredPesos = courtFullPesos + rentalsFullPesos + consumablesFullPesos;

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        Response.StatusCode = 500;
                        Response.Write("Reservation validation error:<br/><pre>" + HttpUtility.HtmlEncode(ex.Message) + "</pre>");
                        return;
                    }
                }
            }

            int players = 1;
            int.TryParse(Request.Form["numPlayers"], out players);
            if (players <= 0) players = 1;

            string draftToken = Guid.NewGuid().ToString("N");

            PendingReservationDraft draft = new PendingReservationDraft
            {
                DraftToken = draftToken,
                UserID = userId,
                CourtID = courtId,
                SportName = selectedSport,
                ResDate = resDate.ToString("yyyy-MM-dd"),
                StartTime = startTime.ToString(@"hh\:mm"),
                EndTime = endTime.ToString(@"hh\:mm"),
                Duration = duration,
                Firstname = (txtFirstname.Text ?? "").Trim(),
                Lastname = (txtLastname.Text ?? "").Trim(),
                Email = (txtEmail.Text ?? "").Trim(),
                Contact = (txtContact.Text ?? "").Trim(),
                Players = players,
                RentalCartJson = rentalCartJson,
                ConsumableCartJson = consumableCartJson,
                CourtFullPesos = courtFullPesos,
                CourtDepositPesos = courtDepositPesos,
                RentalsFullPesos = rentalsFullPesos,
                ConsumablesFullPesos = consumablesFullPesos,
                PaymongoChargeNowPesos = courtDepositPesos + rentalsFullPesos + consumablesFullPesos,
                RequiredAmountStoredPesos = requiredAmountStoredPesos
            };

            Session["PendingReservationDraft"] = new JavaScriptSerializer().Serialize(draft);
            Session["PendingReservationCheckoutSessionID"] = null;
            Session["PendingReservationCheckoutURL"] = null;
            Session["PendingReservationToken"] = draftToken;

            SavePendingReservationBackupCookie(draft, null);

            string url = ResolveUrl("~/PreparingPayment.aspx?token=" + draftToken);
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void LoadCourtData()
        {
            hfCourts.Value = GetCourtsJson();
            hfQueues.Value = GetQueuesJson();
        }

        private string GetCourtsJson()
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand("SELECT CourtID, CourtNumber, SportName, IsActive FROM tblCourt WHERE IsActive = 1", con))
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
                    dict[col.ColumnName] = row[col];

                list.Add(dict);
            }

            return list;
        }

        private void UpdateSelectionStatusLabel()
        {
            bool hasDate = !string.IsNullOrWhiteSpace(hfSelectedDate.Value);
            bool hasSport = !string.IsNullOrWhiteSpace(GetSelectedSport());
            bool hasCourt = !string.IsNullOrWhiteSpace(hfSelectedCourtID.Value);
            bool hasStart = !string.IsNullOrWhiteSpace(hfStartTime.Value);

            if (!hasDate && !hasSport)
            {
                lblSelectedSlot.Text = "No date and sport selected.";
                return;
            }

            if (!hasDate)
            {
                lblSelectedSlot.Text = "No date selected.";
                return;
            }

            if (!hasSport)
            {
                lblSelectedSlot.Text = "No sport selected.";
                return;
            }

            if (!hasCourt || !hasStart)
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
            DateTime selectedDate = Calendar1.SelectedDate.Date;

            if (LastSelectedDate.HasValue && LastSelectedDate.Value.Date == selectedDate)
            {
                Calendar1.SelectedDates.Clear();
                Calendar1.SelectedDate = DateTime.MinValue;
                LastSelectedDate = null;

                hfSelectedDate.Value = "";
                hfResDate.Value = "";

                ClearSelectedSlot();
                phTimeTable.Controls.Clear();
                lblUnavailableHours.Text = "Select a date and sport to see unavailable hours.";
                UpdateSelectionStatusLabel();
                return;
            }

            DateTime min = DateTime.Today;
            DateTime max = DateTime.Today.AddDays(14);

            if (selectedDate < min || selectedDate > max)
            {
                Calendar1.SelectedDates.Clear();
                Calendar1.SelectedDate = DateTime.MinValue;
                LastSelectedDate = null;
                return;
            }

            LastSelectedDate = selectedDate;

            hfSelectedDate.Value = selectedDate.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            ClearSelectedSlot();

            string sport = GetSelectedSport();

            if (string.IsNullOrWhiteSpace(sport))
            {
                phTimeTable.Controls.Clear();
                lblUnavailableHours.Text = "Select a date and sport to see unavailable hours.";
                UpdateSelectionStatusLabel();
                return;
            }

            RenderTimeTable(selectedDate, sport);
            UpdateUnavailableHours(selectedDate, sport);
            UpdateSelectionStatusLabel();
        }

        protected void ddlSport_SelectedIndexChanged(object sender, EventArgs e)
        {
            string sport = GetSelectedSport();
            hfSelectedSport.Value = sport;

            DateTime d;
            bool hasDate = TryParseDateFlexible(hfSelectedDate.Value, out d);

            ClearSelectedSlot();

            if (!hasDate || string.IsNullOrWhiteSpace(sport))
            {
                phTimeTable.Controls.Clear();
                lblUnavailableHours.Text = "Select a date and sport to see unavailable hours.";
                UpdateSelectionStatusLabel();
                return;
            }

            hfSelectedDate.Value = d.ToString("yyyy-MM-dd");
            hfResDate.Value = hfSelectedDate.Value;

            LoadCourtData();
            RenderTimeTable(d, sport);
            UpdateUnavailableHours(d, sport);
            UpdateSelectionStatusLabel();
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

        private void RenderTimeTable(DateTime date, string sport)
        {
            sport = NormalizeSport(sport);

            if (date == DateTime.MinValue || string.IsNullOrWhiteSpace(sport))
            {
                phTimeTable.Controls.Clear();
                return;
            }

            DataTable dt = GetGridData(date, sport);

            Dictionary<string, DataRow> map = new Dictionary<string, DataRow>();
            foreach (DataRow r in dt.Rows)
            {
                int courtId = Convert.ToInt32(r["CourtID"]);
                TimeSpan slot = (TimeSpan)r["SlotStart"];
                map[courtId.ToString() + "|" + slot.ToString()] = r;
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
                sb.Append("<tr>");
                sb.Append("<td class='time-col'>" + DateTime.Today.Add(t).ToString("hh:mm tt") + "</td>");

                for (int courtNum = 1; courtNum <= 6; courtNum++)
                {
                    int courtId = GetCourtIdByNumber(dt, courtNum);
                    DateTime slotDateTime = date.Date.Add(t);

                    bool isPastSlot = (date.Date == DateTime.Today && slotDateTime <= DateTime.Now);

                    if (isPastSlot)
                    {
                        sb.Append("<td><div class='slot past'></div></td>");
                        continue;
                    }

                    if (courtId <= 0)
                    {
                        sb.Append("<td><div class='slot past'></div></td>");
                        continue;
                    }

                    string k = courtId.ToString() + "|" + t.ToString();

                    bool blocked = true;
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

                    if (isClosed)
                    {
                        sb.Append("<td><div class='slot blocked'>Closed</div></td>");
                    }
                    else if (isPfa)
                    {
                        sb.Append("<td><div class='slot reservable available' " +
                                  "data-court='" + courtId + "' " +
                                  "data-courtnum='" + courtNum + "' " +
                                  "data-date='" + date.ToString("yyyy-MM-dd") + "' " +
                                  "data-start='" + t.ToString(@"hh\:mm") + "'>PFA</div></td>");
                    }
                    else if (isQueue)
                    {
                        sb.Append("<td><div class='slot queue'>Queue</div></td>");
                    }
                    else if (blocked)
                    {
                        sb.Append("<td><div class='slot blocked'>Booked</div></td>");
                    }
                    else if (!isReservable)
                    {
                        sb.Append("<td><div class='slot blocked'>N/A</div></td>");
                    }
                    else
                    {
                        sb.Append("<td><div class='slot reservable available' " +
                                  "data-court='" + courtId + "' " +
                                  "data-courtnum='" + courtNum + "' " +
                                  "data-date='" + date.ToString("yyyy-MM-dd") + "' " +
                                  "data-start='" + t.ToString(@"hh\:mm") + "'>Free</div></td>");
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

            if (date == DateTime.MinValue || string.IsNullOrWhiteSpace(sport))
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
            if (ddlSport != null && ddlSport.Items.Count > 0)
                ddlSport.SelectedIndex = 0;

            try { ddlDuration.ClearSelection(); } catch { }
            if (ddlDuration != null && ddlDuration.Items.FindByValue("1") != null)
                ddlDuration.SelectedValue = "1";
            else if (ddlDuration != null && ddlDuration.Items.Count > 0)
                ddlDuration.SelectedIndex = 0;

            txtFirstname.Text = "";
            txtLastname.Text = "";
            txtEmail.Text = "";
            txtContact.Text = "";

            Calendar1.VisibleDate = DateTime.Today;
            Calendar1.SelectedDates.Clear();

            hfSelectedDate.Value = "";
            hfResDate.Value = "";

            ClearSelectedSlot();

            hfRentalCart.Value = "{}";
            hfConsumableCart.Value = "{}";
            hfRentalItems.Value = "";
            hfRentalStock.Value = "";

            Session.Remove("ReservationDraft");
            Session.Remove("RentalCart");
            Session.Remove("ConsumableCart");
            Session.Remove("PendingReservationDraft");
            Session.Remove("PendingReservationCheckoutSessionID");
            Session.Remove("PendingReservationCheckoutURL");
            Session.Remove("PendingReservationToken");
            Session.Remove("FinalizedReservationToken");
            Session.Remove("FinalizedReservationID");

            phTimeTable.Controls.Clear();
            lblUnavailableHours.Text = "Select a date and sport to see unavailable hours.";
            UpdateSelectionStatusLabel();

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

            UpdateSelectionStatusLabel();
        }

        private void EnsureUserSessionInfo()
        {
            if (Session["UserID"] == null) return;

            if (Session["Firstname"] != null &&
                Session["Lastname"] != null &&
                Session["Email"] != null &&
                Session["PhoneNumber"] != null)
                return;

            int userId = Convert.ToInt32(Session["UserID"]);

            using (SqlConnection con = new SqlConnection(CS))
            using (SqlCommand cmd = new SqlCommand(@"
SELECT Firstname, Lastname, Email, PhoneNumber
FROM tblPlayerAccount
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