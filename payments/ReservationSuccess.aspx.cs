using Smash_IT.homepage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Smash_IT.payments
{
    public partial class ReservationSuccess : reservation
    {
        protected new void Page_Load(object sender, EventArgs e)
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
                Response.Write("This reservation has already been finalized.");
                return;
            }

            var draft = GetPendingDraftFromSession(Session, token);
            if (draft == null)
            {
                draft = GetPendingDraftFromCookie(Request, token);
            }
            if (draft == null)
            {
                Response.Write("Reservation draft not found. It may have expired.");
                return;
            }

            string checkoutSessionId = Convert.ToString(Session["PendingReservationCheckoutSessionID"] ?? "");
            if (string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                checkoutSessionId = GetPendingCheckoutSessionIdFromCookie(Request);
            }
         

            if (string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                Response.Write("Missing checkout session.");
                return;
            }

            try
            {
                if (!IsCheckoutPaid(checkoutSessionId))
                {
                    Response.Write("Payment not yet completed. Please complete your payment and try again.");
                    return;
                }

                int reservationId = FinalizeReservationAfterSuccessfulPayment(draft, checkoutSessionId);
                Session["FinalizedReservationToken"] = token;
                Session["FinalizedReservationID"] = reservationId;

                Session.Remove("PendingReservationDraft");
                Session.Remove("PendingReservationCheckoutSessionID");
                Session.Remove("PendingReservationCheckoutURL");
                Session.Remove("PendingReservationToken");

                ClearPendingReservationBackupCookie();

                ViewState["ReservationID"] = reservationId;
            }
            catch (Exception ex)
            {
                Response.Write("Unable to finalize reservation:<br/><pre>" + Server.HtmlEncode(ex.ToString()) + "</pre>");
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

                Dictionary<string, object> data = null;
                if (parsed.ContainsKey("data"))
                    data = parsed["data"] as Dictionary<string, object>;

                if (data == null)
                    throw new Exception("PayMongo response missing data.");

                Dictionary<string, object> attrs = null;
                if (data.ContainsKey("attributes"))
                    attrs = data["attributes"] as Dictionary<string, object>;

                if (attrs == null)
                    throw new Exception("PayMongo response missing attributes.");

                string paymentStatus = attrs.ContainsKey("payment_status")
                    ? Convert.ToString(attrs["payment_status"] ?? "")
                    : "";

                string status = attrs.ContainsKey("status")
                    ? Convert.ToString(attrs["status"] ?? "")
                    : "";

                return paymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase)
                    || status.Equals("paid", StringComparison.OrdinalIgnoreCase)
                    || status.Equals("active", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}


