<%@ WebHandler Language="C#" Class="Smash_IT.PaymongoWebhook" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Smash_IT
{
    // PayMongo Webhook Handler (revamped)
    // Goals:
    // - Safe parsing (won't crash on missing keys)
    // - Correctly find event type + checkout session id across common payload shapes
    // - Idempotent: never double-insert payments
    // - Computes rentals total even if tblRental.UnitPrice is NOT filled (joins to model default price)
    // - Updates tblReservation + tblRental consistently
    public class PaymongoWebhook : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 405;
                context.Response.Write("Method Not Allowed");
                return;
            }

            string raw;
            using (var reader = new StreamReader(context.Request.InputStream))
                raw = reader.ReadToEnd();

            // Optional debug dump (safe)
            TryDump(context, "~/App_Data/paymongo_webhook_last.json", raw);

            // Signature verification (optional if secret exists)
            string whSecret = ConfigurationManager.AppSettings["PaymongoWebhookSecret"];
            string sigHeader = context.Request.Headers["Paymongo-Signature"] ?? "";

            if (!string.IsNullOrWhiteSpace(whSecret))
            {
                if (!IsValidSignature(sigHeader, whSecret, raw))
                {
                    context.Response.StatusCode = 400;
                    context.Response.Write("Invalid signature");
                    return;
                }
            }

            // Parse JSON
            Dictionary<string, object> evt;
            try
            {
                evt = new JavaScriptSerializer().DeserializeObject(raw) as Dictionary<string, object>;
            }
            catch
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid JSON");
                return;
            }

            if (evt == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid JSON");
                return;
            }

            // Extract event type in a tolerant way (PayMongo payloads can vary)
            // Try: data.attributes.type (your original)
            // Also try: data.type / type
            string eventType =
                AsString(DeepGet(evt, "data", "attributes", "type")) ??
                AsString(DeepGet(evt, "data", "type")) ??
                AsString(DeepGet(evt, "type"));

            if (string.IsNullOrWhiteSpace(eventType))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Missing event type");
                return;
            }

            // We only care about paid checkout session
            if (string.Equals(eventType, "checkout_session.payment.paid", StringComparison.OrdinalIgnoreCase))
            {
                // Extract checkout session id
                // Common patterns:
                // - data.attributes.data.id   (what you used)
                // - data.attributes.data.attributes.checkout_session_id (rare)
                // - data.attributes.data.attributes.id (rare)
                // - data.attributes.data.attributes.checkout_session (rare)
                string checkoutSessionId =
                    AsString(DeepGet(evt, "data", "attributes", "data", "id")) ??
                    AsString(DeepGet(evt, "data", "attributes", "data", "attributes", "checkout_session_id")) ??
                    AsString(DeepGet(evt, "data", "attributes", "data", "attributes", "id"));

                if (!string.IsNullOrWhiteSpace(checkoutSessionId))
                {
                    try
                    {
                        ApplyPaidCheckoutSession(checkoutSessionId);
                    }
                    catch (Exception ex)
                    {
                        // Log server-side errors; respond 200 if you want PayMongo NOT to retry,
                        // or respond 500 if you DO want retries. Usually: return 200 only when success.
                        TryDump(context, "~/App_Data/paymongo_webhook_error.txt",
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + ex.ToString());

                        context.Response.StatusCode = 500;
                        context.Response.Write("error");
                        return;
                    }
                }
            }

            context.Response.StatusCode = 200;
            context.Response.Write("ok");
        }

        public bool IsReusable { get { return false; } }

        // =========================
        // Core business logic
        // =========================
        private static void ApplyPaidCheckoutSession(string checkoutSessionId)
        {
            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (var con = new SqlConnection(cs))
            {
                con.Open();
                using (var tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    // 1) Lock + get reservation by checkout session id
                    int reservationId = 0;
                    int userId = 0;
                    int requiredAmount = 0;
                    string paymentStatus = "";

                    using (var cmd = new SqlCommand(@"
SELECT TOP 1
    ReservationID,
    UserID,
    RequiredAmount,
    ISNULL(PaymentStatus,'') AS PaymentStatus
FROM tblReservation WITH (UPDLOCK, HOLDLOCK)
WHERE PaymongoCheckoutSessionID = @CSID;", con, tx))
                    {
                        cmd.Parameters.AddWithValue("@CSID", checkoutSessionId);

                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                reservationId = ToInt(dr["ReservationID"]);
                                userId = ToInt(dr["UserID"]);
                                requiredAmount = ToInt(dr["RequiredAmount"]);
                                paymentStatus = Convert.ToString(dr["PaymentStatus"] ?? "");
                            }
                        }
                    }

                    // No match => ok (idempotent)
                    if (reservationId <= 0)
                    {
                        tx.Commit();
                        return;
                    }

                    // 2) If already paid/halfpaid => idempotent exit
                    if (paymentStatus.Equals("HalfPaid", StringComparison.OrdinalIgnoreCase) ||
                        paymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        tx.Commit();
                        return;
                    }

                    // 3) Strong idempotency: check if payment rows already exist per type
                    bool hasRentalPay = ExistsPayment(con, tx, reservationId, "Rental");
                    bool hasResPay = ExistsPayment(con, tx, reservationId, "Reservation");

                    // If both exist, just ensure status flags are correct
                    if (hasRentalPay && hasResPay)
                    {
                        MarkReservationHalfPaid(con, tx, reservationId);
                        MarkRentalsPaid(con, tx, reservationId);
                        tx.Commit();
                        return;
                    }

                    // 4) Compute rentals total centavos robustly
                    // If tblRental.UnitPrice exists and is filled: use it
                    // Else fall back to DefaultRentalPrice from model via item->model join.
                    int rentalsTotalCentavos = ComputeRentalsTotalCentavos(con, tx, reservationId);

                    // RequiredAmount = courtFull + rentalsFull
                    int courtFullCentavos = requiredAmount - rentalsTotalCentavos;
                    if (courtFullCentavos < 0) courtFullCentavos = 0;

                    int courtDepositCentavos = courtFullCentavos / 2; // 50% deposit

                    // 5) Insert missing payments only (no duplicates)
                    if (!hasRentalPay)
                        InsertPayment(con, tx, "Rental", userId, reservationId, rentalsTotalCentavos);

                    if (!hasResPay)
                        InsertPayment(con, tx, "Reservation", userId, reservationId, courtDepositCentavos);

                    // 6) Mark paid flags
                    MarkRentalsPaid(con, tx, reservationId);
                    MarkReservationHalfPaid(con, tx, reservationId);

                    tx.Commit();
                }
            }
        }

        // =========================
        // DB helpers
        // =========================
        private static bool ExistsPayment(SqlConnection con, SqlTransaction tx, int reservationId, string paymentType)
        {
            using (var cmd = new SqlCommand(@"
SELECT COUNT(*)
FROM tblPayment WITH (READCOMMITTEDLOCK)
WHERE ReservationID = @RID
  AND PaymentTypeName = @Type;", con, tx))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@Type", paymentType);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void InsertPayment(SqlConnection con, SqlTransaction tx, string type, int userId, int reservationId, int amountCentavos)
        {
            // If amount is 0, you can either skip or still record. Here: skip 0 rental payment.
            if (amountCentavos <= 0 && type.Equals("Rental", StringComparison.OrdinalIgnoreCase))
                return;

            using (var cmd = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
VALUES (@Type, @UserID, @RID, CAST(GETDATE() AS date), @Amt);", con, tx))
            {
                cmd.Parameters.AddWithValue("@Type", type);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.Parameters.AddWithValue("@Amt", amountCentavos);
                cmd.ExecuteNonQuery();
            }
        }

        private static void MarkRentalsPaid(SqlConnection con, SqlTransaction tx, int reservationId)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblRental
SET IsPaid = 1
WHERE ReservationID = @RID;", con, tx))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void MarkReservationHalfPaid(SqlConnection con, SqlTransaction tx, int reservationId)
        {
            using (var cmd = new SqlCommand(@"
UPDATE tblReservation
SET IsPaid = 1,
    PaymentStatus = 'HalfPaid'
WHERE ReservationID = @RID;", con, tx))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int ComputeRentalsTotalCentavos(SqlConnection con, SqlTransaction tx, int reservationId)
        {
            // First try: tblRental.UnitPrice if it exists and has values
            // NOTE: If UnitPrice column doesn't exist, this will throw.
            // If you're not sure, you can remove this block and rely on join-based computation.
            try
            {
                using (var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(CAST(ROUND(ISNULL(UnitPrice,0) * 100.0, 0) AS INT)), 0)
FROM tblRental
WHERE ReservationID = @RID;", con, tx))
                {
                    cmd.Parameters.AddWithValue("@RID", reservationId);
                    int viaUnitPrice = Convert.ToInt32(cmd.ExecuteScalar());

                    // If it's non-zero, trust it. If 0, we still attempt join fallback (maybe no rentals).
                    if (viaUnitPrice > 0)
                        return viaUnitPrice;
                }
            }
            catch
            {
                // ignore and fall back
            }

            // Fallback: sum DefaultRentalPrice from model per rental item
            // Assumes:
            // tblRental.ItemID -> tblEquipmentItem.ItemID -> tblEquipmentModel.ModelID -> DefaultRentalPrice
            using (var cmd2 = new SqlCommand(@"
SELECT ISNULL(SUM(CAST(ROUND(ISNULL(m.DefaultRentalPrice,0) * 100.0, 0) AS INT)), 0)
FROM tblRental r
JOIN tblEquipmentItem ei ON ei.ItemID = r.ItemID
JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
WHERE r.ReservationID = @RID;", con, tx))
            {
                cmd2.Parameters.AddWithValue("@RID", reservationId);
                return Convert.ToInt32(cmd2.ExecuteScalar());
            }
        }

        // =========================
        // Signature verification (your original logic kept)
        // =========================
        private static bool IsValidSignature(string header, string secret, string rawBody)
        {
            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(secret))
                return false;

            string t = null, te = null, li = null;

            foreach (var part in header.Split(','))
            {
                var kv = part.Split('=');
                if (kv.Length != 2) continue;

                var key = kv[0].Trim();
                var val = kv[1].Trim();

                if (key == "t") t = val;
                if (key == "te") te = val;
                if (key == "li") li = val;
            }

            if (string.IsNullOrWhiteSpace(t)) return false;

            string signed = t + "." + rawBody;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signed));
                var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                bool testMatch = !string.IsNullOrEmpty(te) && computed == te;
                bool liveMatch = !string.IsNullOrEmpty(li) && computed == li;
                return testMatch || liveMatch;
            }
        }

        // =========================
        // Small utility helpers
        // =========================
        private static object DeepGet(Dictionary<string, object> root, params string[] path)
        {
            object cur = root;
            for (int i = 0; i < path.Length; i++)
            {
                var dict = cur as Dictionary<string, object>;
                if (dict == null) return null;

                object next;
                if (!dict.TryGetValue(path[i], out next)) return null;
                cur = next;
            }
            return cur;
        }

        private static string AsString(object o)
        {
            return (o == null) ? null : Convert.ToString(o);
        }

        private static int ToInt(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            int x;
            if (int.TryParse(o.ToString(), out x)) return x;
            try { return Convert.ToInt32(o); } catch { return 0; }
        }

        private static void TryDump(HttpContext ctx, string relativePath, string content)
        {
            try
            {
                File.WriteAllText(ctx.Server.MapPath(relativePath), content ?? "");
            }
            catch { }
        }
    }
}