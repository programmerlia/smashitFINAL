<%@ WebHandler Language="C#" Class="Smash_IT.PaymongoWebhook" %>

using System;
using System.Collections.Generic;
using System.Configuration;
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

            // 1) Read raw body FIRST
            string raw;
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                raw = reader.ReadToEnd();
            }

            // 2) Debug dump (optional)
            try
            {
                File.WriteAllText(context.Server.MapPath("~/App_Data/paymongo_webhook.json"), raw);
            }
            catch { /* ignore */ }

            // 3) Verify signature (optional during testing)
string sigHeader = context.Request.Headers["Paymongo-Signature"] ?? "";
try
{
    File.WriteAllText(context.Server.MapPath("~/App_Data/paymongo_webhook_debug.txt"),
        "SigHeader: " + sigHeader + "\n\nRaw:\n" + raw);
}
catch { }
string whSecret = ConfigurationManager.AppSettings["PaymongoWebhookSecret"];

// Skip signature verification if no secret configured (DEV ONLY)
if (!string.IsNullOrWhiteSpace(whSecret))
{
    if (!IsValidSignature(sigHeader, whSecret, raw))
    {
        context.Response.StatusCode = 400;
        context.Response.Write("Invalid signature");
        return;
    }
}


            // 4) Parse JSON (NON-generic)
            var js = new JavaScriptSerializer();
            var evt = js.DeserializeObject(raw) as Dictionary<string, object>;

            if (evt == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid JSON");
                return;
            }

            // evt["data"] -> dict
            var data = evt.ContainsKey("data") ? evt["data"] as Dictionary<string, object> : null;
            var attributes = (data != null && data.ContainsKey("attributes"))
                ? data["attributes"] as Dictionary<string, object>
                : null;

            string eventType = null;
if (attributes != null && attributes.ContainsKey("type") && attributes["type"] != null)
{
    eventType = attributes["type"].ToString();
}

            if (string.IsNullOrWhiteSpace(eventType))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Missing event type");
                return;
            }

            // ✅ Handle paid event
            if (eventType == "checkout_session.payment.paid")
            {
                string checkoutSessionId = null;

                // usually attributes["data"]["id"] (but may vary)
                var innerData = (attributes != null && attributes.ContainsKey("data"))
                    ? attributes["data"] as Dictionary<string, object>
                    : null;

                if (innerData != null && innerData.ContainsKey("id"))
                {
                    if (innerData["id"] != null) checkoutSessionId = innerData["id"].ToString();
                }

                if (!string.IsNullOrWhiteSpace(checkoutSessionId))
                {
                    MarkReservationPaidByCheckoutSession(checkoutSessionId);
                }
                else
                {
                    // dump for debugging payload shape
                    try
                    {
                        File.WriteAllText(
                            context.Server.MapPath("~/App_Data/paymongo_webhook_debug.txt"),
                            "Could not find checkout session id.\n" +
                            "attributes keys: " + (attributes == null ? "(null)" : string.Join(",", attributes.Keys)) + "\n\n" +
                            raw
                        );
                    }
                    catch { /* ignore */ }
                }
            }

            context.Response.StatusCode = 200;
            context.Response.Write("ok");
        }

        public bool IsReusable { get { return false; } }

        private static void MarkReservationPaidByCheckoutSession(string checkoutSessionId)
        {
            using (SqlConnection con = new SqlConnection(
                ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(@"
UPDATE tblReservation
SET IsPaid = 1,
    PaymentStatus = 'Paid'
WHERE PaymongoCheckoutSessionID = @CSID", con))
            {
                cmd.Parameters.AddWithValue("@CSID", checkoutSessionId);
                con.Open();
                cmd.ExecuteNonQuery();
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

            if (t == null) return false;

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