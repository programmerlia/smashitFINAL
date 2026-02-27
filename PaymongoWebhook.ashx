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
    public class PaymongoWebhook : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                context.Response.Write("Method Not Allowed");
                return;
            }

            string raw;
            using (var reader = new StreamReader(context.Request.InputStream))
                raw = reader.ReadToEnd();

            // (Optional) debug dump
            try { File.WriteAllText(context.Server.MapPath("~/App_Data/paymongo_webhook_last.json"), raw); } catch { }

            string sigHeader = context.Request.Headers["Paymongo-Signature"] ?? "";
            string whSecret = ConfigurationManager.AppSettings["PaymongoWebhookSecret"];

            if (!string.IsNullOrWhiteSpace(whSecret))
            {
                if (!IsValidSignature(sigHeader, whSecret, raw))
                {
                    context.Response.StatusCode = 400;
                    context.Response.Write("Invalid signature");
                    return;
                }
            }

            var js = new JavaScriptSerializer();
            var evt = js.DeserializeObject(raw) as Dictionary<string, object>;
            if (evt == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid JSON");
                return;
            }

            var data = evt.ContainsKey("data") ? evt["data"] as Dictionary<string, object> : null;
            var attributes = (data != null && data.ContainsKey("attributes")) ? data["attributes"] as Dictionary<string, object> : null;

            string eventType = (attributes != null && attributes.ContainsKey("type") && attributes["type"] != null)
                ? attributes["type"].ToString()
                : null;

            if (string.IsNullOrWhiteSpace(eventType))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Missing event type");
                return;
            }

            if (eventType == "checkout_session.payment.paid")
            {
                string checkoutSessionId = null;

                // PayMongo: data.attributes.data.id
                var innerData = (attributes != null && attributes.ContainsKey("data"))
                    ? attributes["data"] as Dictionary<string, object>
                    : null;

                if (innerData != null && innerData.ContainsKey("id") && innerData["id"] != null)
                    checkoutSessionId = innerData["id"].ToString();

                if (!string.IsNullOrWhiteSpace(checkoutSessionId))
                {
                    ApplyPaidCheckoutSession(checkoutSessionId);
                }
            }

            context.Response.StatusCode = 200;
            context.Response.Write("ok");
        }

        public bool IsReusable { get { return false; } }

        private static void ApplyPaidCheckoutSession(string checkoutSessionId)
        {
            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (var con = new SqlConnection(cs))
            {
                con.Open();
                using (var tx = con.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        // 1) Lock + get reservation
                        int reservationId = 0;
                        int requiredAmount = 0;
                        string paymentStatus = null;

                        using (var cmd = new SqlCommand(@"
SELECT TOP 1 ReservationID, RequiredAmount, ISNULL(PaymentStatus,'') AS PaymentStatus
FROM tblReservation WITH (UPDLOCK, HOLDLOCK)
WHERE PaymongoCheckoutSessionID = @CSID;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@CSID", checkoutSessionId);

                            using (var dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    reservationId = Convert.ToInt32(dr["ReservationID"]);
                                    requiredAmount = Convert.ToInt32(dr["RequiredAmount"]);
                                    paymentStatus = dr["PaymentStatus"].ToString();
                                }
                            }
                        }

                        if (reservationId <= 0)
                        {
                            tx.Commit();
                            return; // no matching reservation
                        }

                        // 2) Idempotency guard
                        // If already marked halfpaid/paid, skip
                        if (string.Equals(paymentStatus, "HalfPaid", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                        {
                            tx.Commit();
                            return;
                        }

                        // Also guard against duplicate payment inserts (webhook retries)
                        int existingPayments = 0;
                        using (var cmd = new SqlCommand(@"
SELECT COUNT(*)
FROM tblPayment
WHERE ReservationID = @RID
  AND PaymentTypeName IN ('Rental','Reservation');", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            existingPayments = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (existingPayments >= 2)
                        {
                            // Payments already inserted; just ensure statuses are consistent
                            using (var cmd = new SqlCommand(@"
UPDATE tblReservation
SET IsPaid = 1, PaymentStatus = 'HalfPaid'
WHERE ReservationID = @RID;", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = new SqlCommand(@"
UPDATE tblRental
SET IsPaid = 1
WHERE ReservationID = @RID;", con, tx))
                            {
                                cmd.Parameters.AddWithValue("@RID", reservationId);
                                cmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            return;
                        }

                        // 3) Compute rentals total centavos from tblRental.UnitPrice
                        // UnitPrice is DECIMAL(10,2) pesos; Amount in tblPayment is INT (centavos)
                        int rentalsTotal = 0;
                        using (var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(CAST(ROUND(UnitPrice * 100.0, 0) AS INT)), 0)
FROM tblRental
WHERE ReservationID = @RID;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            rentalsTotal = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 4) Compute court full and deposit from RequiredAmount rule:
                        // RequiredAmount = courtFull + rentalsFull
                        int courtFull = requiredAmount - rentalsTotal;
                        if (courtFull < 0) courtFull = 0; // safety

                        int courtDeposit = courtFull / 2; // 50%
                        // If you want rounding up for odd centavos:
                        // int courtDeposit = (courtFull + 1) / 2;

                        // 5) Insert TWO payment rows (Rental + Reservation deposit)
                        using (var cmd = new SqlCommand(@"
INSERT INTO tblPayment (PaymentTypeName, ReservationID, PaymentDate, Amount)
VALUES ('Rental', @RID, CAST(GETDATE() AS date), @AmtRental);

INSERT INTO tblPayment (PaymentTypeName, ReservationID, PaymentDate, Amount)
VALUES ('Reservation', @RID, CAST(GETDATE() AS date), @AmtDeposit);", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            cmd.Parameters.AddWithValue("@AmtRental", rentalsTotal);
                            cmd.Parameters.AddWithValue("@AmtDeposit", courtDeposit);
                            cmd.ExecuteNonQuery();
                        }

                        // 6) Mark rentals paid
                        using (var cmd = new SqlCommand(@"
UPDATE tblRental
SET IsPaid = 1
WHERE ReservationID = @RID;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            cmd.ExecuteNonQuery();
                        }

                        // 7) Mark reservation as HalfPaid (deposit paid)
                        using (var cmd = new SqlCommand(@"
UPDATE tblReservation
SET IsPaid = 1,
    PaymentStatus = 'HalfPaid'
WHERE ReservationID = @RID;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@RID", reservationId);
                            cmd.ExecuteNonQuery();
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
        }

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
    }
}