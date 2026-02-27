<%@ WebHandler Language="C#" Class="Smash_IT.CreateCheckout" %>

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

namespace Smash_IT
{
    public class CreateCheckout : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/html";

            int reservationId;

if (!int.TryParse((context.Request["reservationId"] ?? "").Trim(), out reservationId))
{
    context.Response.StatusCode = 400;
    context.Response.Write("Invalid reservationId.");
    return;
}
          

            // Build proper base URL (supports reverse proxies)
            string scheme = context.Request.Headers["X-Forwarded-Proto"];
            if (string.IsNullOrEmpty(scheme)) scheme = context.Request.Url.Scheme;

            string host = context.Request.Headers["X-Forwarded-Host"];
            if (string.IsNullOrEmpty(host)) host = context.Request.Url.Authority;

            string baseUrl = scheme + "://" + host;

            // ✅ Your real pages here
            string successUrl = baseUrl + "/ReservationSuccess.aspx?resId=" + reservationId;
            string cancelUrl  = baseUrl + "/homepage/reservation.aspx?cancel=1&resId=" + reservationId;

            try
            {
                // Load totals from DB and compute payNow amount safely
                var calc = LoadAmountsForReservation(reservationId);

                // payNow = rentalsFull + courtDeposit
                int payNow = checked(calc.RentalsTotalCentavos + calc.CourtDepositCentavos);

                string description = "Reservation #" + reservationId;

                var pm = CreatePaymongoCheckout(payNow, description, successUrl, cancelUrl, reservationId, calc);

                // Save checkout session id
                SaveCheckoutSessionId(reservationId, pm.CheckoutSessionId);

                // Redirect user
                context.Response.Redirect(pm.CheckoutUrl, endResponse: true);
            }
            catch (WebException wex)
{
    string errBody = "";

    HttpWebResponse errResp = wex.Response as HttpWebResponse;
    if (errResp != null)
    {
        Stream respStream = errResp.GetResponseStream();
        if (respStream != null)
        {
            using (StreamReader reader = new StreamReader(respStream))
            {
                errBody = reader.ReadToEnd();
            }
        }
    }

    context.Response.StatusCode = 500;
    context.Response.Write("PayMongo error: <pre>" + HttpUtility.HtmlEncode(errBody) + "</pre>");
}
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("Error: " + HttpUtility.HtmlEncode(ex.Message));
            }
        }

        public bool IsReusable { get { return false; } }

        private sealed class ReservationAmounts
        {
            public int RequiredAmountCentavos { get; set; }   // courtFull + rentalsFull
            public int RentalsTotalCentavos { get; set; }     // 100% rentals
            public int CourtFullCentavos { get; set; }        // 100% court
            public int CourtDepositCentavos { get; set; }     // 50% court
        }

        private static ReservationAmounts LoadAmountsForReservation(int reservationId)
        {
            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            int requiredAmount = 0;
            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand(@"
SELECT RequiredAmount
FROM tblReservation
WHERE ReservationID = @RID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                con.Open();
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value)
                    throw new Exception("Reservation not found.");

                requiredAmount = Convert.ToInt32(v);
            }

            // rentals total from tblRental.UnitPrice
            int rentalsTotal = 0;
            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand(@"
SELECT ISNULL(SUM(CAST(ROUND(UnitPrice * 100.0, 0) AS INT)), 0)
FROM tblRental
WHERE ReservationID = @RID;", con))
            {
                cmd.Parameters.AddWithValue("@RID", reservationId);
                con.Open();
                rentalsTotal = Convert.ToInt32(cmd.ExecuteScalar());
            }

            int courtFull = requiredAmount - rentalsTotal;
            if (courtFull < 0) courtFull = 0;

            // deposit 50%
            int courtDeposit = courtFull / 2;
            // if you want rounding up: (courtFull + 1) / 2

            return new ReservationAmounts
            {
                RequiredAmountCentavos = requiredAmount,
                RentalsTotalCentavos = rentalsTotal,
                CourtFullCentavos = courtFull,
                CourtDepositCentavos = courtDeposit
            };
        }

        private sealed class PaymongoCheckoutResult
        {
            public string CheckoutUrl { get; set; }
            public string CheckoutSessionId { get; set; }
        }

        private static PaymongoCheckoutResult CreatePaymongoCheckout(
            int amountCentavos,
            string description,
            string successUrl,
            string cancelUrl,
            int reservationId,
            ReservationAmounts calc)
        {
            string secretKey = ConfigurationManager.AppSettings["PaymongoSecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new Exception("PaymongoSecretKey not found in web.config");

            var payloadObj = new
            {
                data = new
                {
                    attributes = new
                    {
                        cancel_url = cancelUrl,
                        success_url = successUrl,
                        description = description,

                        // Hide line items UI if you want
                        show_line_items = false,

                        line_items = new object[]
                        {
                            new {
                                name = "Pay Now (Rentals + Court Deposit)",
                                description = description,
                                amount = amountCentavos,
                                currency = "PHP",
                                quantity = 1
                            }
                        },

                        payment_method_types = new string[] { "gcash" },

                        // ✅ Helpful for webhook debugging (doesn't replace DB lookup)
                        metadata = new {
                            reservation_id = reservationId.ToString(),
                            required_amount = calc.RequiredAmountCentavos.ToString(),
                            rentals_amount = calc.RentalsTotalCentavos.ToString(),
                            court_full = calc.CourtFullCentavos.ToString(),
                            court_deposit = calc.CourtDepositCentavos.ToString()
                        }
                    }
                }
            };

            var js = new JavaScriptSerializer();
            string payload = js.Serialize(payloadObj);

            var req = (HttpWebRequest)WebRequest.Create("https://api.paymongo.com/v1/checkout_sessions");
            req.Method = "POST";
            req.ContentType = "application/json";

            string basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));
            req.Headers["Authorization"] = "Basic " + basicAuth;

            using (var stream = new StreamWriter(req.GetRequestStream()))
                stream.Write(payload);

            string responseText;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream()))
                responseText = reader.ReadToEnd();

            var respObj = js.DeserializeObject(responseText) as Dictionary<string, object>;
            var data = respObj["data"] as Dictionary<string, object>;
            var attrs = data["attributes"] as Dictionary<string, object>;

            return new PaymongoCheckoutResult
            {
                CheckoutUrl = attrs["checkout_url"].ToString(),
                CheckoutSessionId = data["id"].ToString()
            };
        }

        private static void SaveCheckoutSessionId(int reservationId, string checkoutSessionId)
        {
            string cs = ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

            using (var con = new SqlConnection(cs))
            using (var cmd = new SqlCommand(@"
UPDATE tblReservation
SET PaymongoCheckoutSessionID = @CSID
WHERE ReservationID = @RID;", con))
            {
                cmd.Parameters.AddWithValue("@CSID", checkoutSessionId);
                cmd.Parameters.AddWithValue("@RID", reservationId);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}