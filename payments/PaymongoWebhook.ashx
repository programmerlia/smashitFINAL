<%@ WebHandler Language="C#" Class="Smash_IT.payments.PaymongoWebhook" %>

using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Smash_IT.payments
{
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

            string whSecret = ConfigurationManager.AppSettings["PaymongoWebhookSecret"];
            string sigHeader = context.Request.Headers["Paymongo-Signature"] ?? "";

            if (string.IsNullOrWhiteSpace(whSecret))
            {
                context.Response.StatusCode = 503;
                context.Response.Write("Webhook is not configured");
                return;
            }
            if (!IsValidSignature(sigHeader, whSecret, raw))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid signature");
                return;
            }

            try
            {
                File.WriteAllText(
                    context.Server.MapPath("~/App_Data/paymongo_webhook_last.json"),
                    raw ?? ""
                );
            }
            catch { }

            context.Response.StatusCode = 200;
            context.Response.Write("ok");
        }

        public bool IsReusable { get { return false; } }

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
