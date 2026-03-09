using Smash_IT.homepage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Smash_IT
{
    public partial class ReservationSuccess : reservation
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string token = (Request.QueryString["token"] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                Response.Redirect("~/homepage/reservation.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string finalizedToken = Convert.ToString(Session["FinalizedReservationToken"] ?? "");
            if (string.Equals(finalizedToken, token, StringComparison.Ordinal))
            {
                return;
            }

            var draft = GetPendingDraftFromSession(Session, token);
            if (draft == null)
            {
                Response.Write("Payment draft not found or expired.");
                return;
            }

            string checkoutSessionId = Convert.ToString(Session["PendingReservationCheckoutSessionID"] ?? "");
            if (string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                Response.Write("Missing checkout session.");
                return;
            }

            try
            {
                if (!IsCheckoutPaid(checkoutSessionId))
                {
                    Response.Write("Payment is not yet confirmed.");
                    return;
                }

                int reservationId = FinalizeReservationAfterSuccessfulPayment(draft, checkoutSessionId);

                Session["FinalizedReservationToken"] = token;
                Session["FinalizedReservationID"] = reservationId;

                Session.Remove("PendingReservationDraft");
                Session.Remove("PendingReservationCheckoutSessionID");

                ViewState["ReservationID"] = reservationId;
            }
            catch (Exception ex)
            {
                Response.Write("Unable to finalize reservation: " + Server.HtmlEncode(ex.Message));
            }
        }

        private bool IsCheckoutPaid(string checkoutSessionId)
        {
            string secretKey = ConfigurationManager.AppSettings["PaymongoSecretKey"];
            string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.paymongo.com/v1/checkout_sessions/" + checkoutSessionId);
            req.Method = "GET";
            req.ContentType = "application/json";
            req.Headers["Authorization"] = "Basic " + basic;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
            {
                string body = reader.ReadToEnd();
                Dictionary<string, object> parsed =
                    (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(body);
                Dictionary<string, object> data =
                    (Dictionary<string, object>)parsed["data"];
                Dictionary<string, object> attrs =
                    (Dictionary<string, object>)data["attributes"];

                string paymentStatus = Convert.ToString(attrs["payment_status"] ?? "");
                string status = Convert.ToString(attrs["status"] ?? "");

                return paymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase)
                    || status.Equals("paid", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}