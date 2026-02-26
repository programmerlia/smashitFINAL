<%@ WebHandler Language="C#" Class="Smash_IT.CreateCheckout" %>

using System;
using System.Configuration;
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
            // If you're redirecting, use text/html
            context.Response.ContentType = "text/html";

            string amountPhpRaw = (context.Request["amount"] ?? "").Trim();
            string description  = (context.Request["desc"] ?? "Reservation Payment").Trim();

            // TODO: change these to your real pages
            string successUrl = "http://localhost:5000/success.html";
            string cancelUrl  = "http://localhost:5000/cancel.html";

            decimal amountPhp;

if (!decimal.TryParse(amountPhpRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out amountPhp))
{
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid amount.");
                return;
            }

            int amountCentavos = (int)Math.Round(amountPhp * 100m, MidpointRounding.AwayFromZero);
            if (amountCentavos < 100) // ₱1.00 minimum guard (optional)
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Amount too small.");
                return;
            }

            try
            {
                string checkoutUrl = CreatePaymongoCheckoutUrl(amountCentavos, description, successUrl, cancelUrl);
                context.Response.Redirect(checkoutUrl, endResponse: true);
            }
            catch (WebException wex)
            {
                string errBody = "";
                var errResp = wex.Response as HttpWebResponse;
                if (errResp != null)
                {
                    using (var reader = new StreamReader(errResp.GetResponseStream()))
                        errBody = reader.ReadToEnd();
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

        private static string CreatePaymongoCheckoutUrl(int amountCentavos, string description, string successUrl, string cancelUrl)
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

                        // ✅ Hide the item on PayMongo page (optional)
                        show_line_items = false,

                        // ✅ REQUIRED by checkout_sessions
                        line_items = new object[]
                        {
                            new {
                                name = "Total",
                                description = description,
                                amount = amountCentavos,
                                currency = "PHP",
                                quantity = 1
                            }
                        },

                        payment_method_types = new string[] { "gcash" }
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

            var respObj = js.DeserializeObject(responseText) as System.Collections.Generic.Dictionary<string, object>;
            var data = respObj["data"] as System.Collections.Generic.Dictionary<string, object>;
            var attrs = data["attributes"] as System.Collections.Generic.Dictionary<string, object>;

            return attrs["checkout_url"].ToString();
        }
    }
}