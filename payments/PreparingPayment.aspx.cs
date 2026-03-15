using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Smash_IT.payments
{
    public partial class PreparingPayment : System.Web.UI.Page
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

            var draft = Smash_IT.homepage.reservation.GetPendingDraftFromSession(Session, token);
            if (draft == null)
            {
                draft = Smash_IT.homepage.reservation.GetPendingDraftFromCookie(Request, token);
            }

            if (draft == null)
            {
                Response.Redirect("~/homepage/reservation.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string existingToken = Convert.ToString(Session["PendingReservationToken"] ?? "");
            if (!string.Equals(existingToken, token, StringComparison.Ordinal))
            {
                Session["PendingReservationCheckoutSessionID"] = null;
                Session["PendingReservationCheckoutURL"] = null;
                Session["PendingReservationToken"] = token;
            }

            string existingCheckoutSessionId = Convert.ToString(Session["PendingReservationCheckoutSessionID"] ?? "");
            string existingCheckoutUrl = Convert.ToString(Session["PendingReservationCheckoutURL"] ?? "");

            if (!string.IsNullOrWhiteSpace(existingCheckoutSessionId) &&
                !string.IsNullOrWhiteSpace(existingCheckoutUrl))
            {
                Response.Redirect(existingCheckoutUrl, false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string scheme = Request.Headers["X-Forwarded-Proto"];
            if (string.IsNullOrEmpty(scheme)) scheme = Request.Url.Scheme;

            string host = Request.Headers["X-Forwarded-Host"];
            if (string.IsNullOrEmpty(host)) host = Request.Url.Authority;

            string baseUrl = scheme + "://" + host;

            string successUrl = baseUrl + ResolveUrl("~/payments/ReservationSuccess.aspx?token=" + token);
            string cancelUrl = baseUrl + ResolveUrl("~/homepage/reservation.aspx");

            int paymongoChargeNowCentavos =
                (int)Math.Round(draft.PaymongoChargeNowPesos * 100m, MidpointRounding.AwayFromZero);

            object payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        description = "Court reservation payment",
                        reference_number = "DRAFT-" + draft.DraftToken,
                        success_url = successUrl,
                        cancel_url = cancelUrl,
                        send_email_receipt = true,
                        payment_method_types = new string[] { "gcash" },
                        show_line_items = false,
                        line_items = new object[]
                        {
                            new
                            {
                                name = "Reservation Payment",
                                description = "CourtID " + draft.CourtID + " • " +
                                              draft.ResDate + " • " +
                                              draft.StartTime + "-" + draft.EndTime,
                                amount = paymongoChargeNowCentavos,
                                currency = "PHP",
                                quantity = 1
                            }
                        },
                        metadata = new
                        {
                            draft_token = draft.DraftToken,
                            user_id = draft.UserID.ToString(),
                            court_id = draft.CourtID.ToString(),
                            res_date = draft.ResDate,
                            start = draft.StartTime,
                            end = draft.EndTime
                        }
                    }
                }
            };

            string secretKey = ConfigurationManager.AppSettings["PaymongoSecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Response.Write("PaymongoSecretKey missing.");
                return;
            }

            string json = new JavaScriptSerializer().Serialize(payload);
            string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));

            try
            {
                string checkoutUrl = null;
                string checkoutSessionId = null;

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.paymongo.com/v1/checkout_sessions");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Headers["Authorization"] = "Basic " + basic;

                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(json);
                }

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

                    checkoutUrl = Convert.ToString(attrs["checkout_url"]);
                    checkoutSessionId = Convert.ToString(data["id"]);
                }

                Session["PendingReservationCheckoutSessionID"] = checkoutSessionId ?? "";
                Session["PendingReservationCheckoutURL"] = checkoutUrl ?? "";

                var backupPayload = new Dictionary<string, object>
{
    { "Draft", draft },
    { "CheckoutSessionId", checkoutSessionId ?? "" }
};

                string backupJson = new JavaScriptSerializer().Serialize(backupPayload);
                byte[] backupBytes = Encoding.UTF8.GetBytes(backupJson);
                byte[] protectedBackupBytes = System.Web.Security.MachineKey.Protect(backupBytes, "PendingReservationBackup");
                string protectedBackupValue = Convert.ToBase64String(protectedBackupBytes);

                HttpCookie backupCookie = new HttpCookie("PendingReservationBackup", protectedBackupValue);
                backupCookie.HttpOnly = true;
                backupCookie.Secure = false;
                backupCookie.Path = "/";
                backupCookie.Expires = DateTime.Now.AddHours(2);
                Response.Cookies.Set(backupCookie);
                if (string.IsNullOrWhiteSpace(checkoutUrl))
                    throw new Exception("PayMongo checkout_url was empty.");

                Response.Redirect(checkoutUrl, false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (WebException ex)
            {
                string errBody = "";
                HttpWebResponse errResp = ex.Response as HttpWebResponse;
                if (errResp != null)
                {
                    using (StreamReader r = new StreamReader(errResp.GetResponseStream()))
                        errBody = r.ReadToEnd();
                }

                try
                {
                    File.WriteAllText(
                        Server.MapPath("~/App_Data/paymongo_prepare_error.txt"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + errBody
                    );
                }
                catch { }

                Response.Write("Unable to create payment session.");
            }
        }
    }
}