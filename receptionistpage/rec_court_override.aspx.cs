using Smash_IT.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace Smash_IT.receptionistpage
{
    public partial class rec_court_override : System.Web.UI.Page
    {
        private readonly string connString =
            ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        private readonly CourtAvailabilityHelper helper = new CourtAvailabilityHelper();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                helper.EnsureAvailabilityWindow();
                txtDate.Text = DateTime.Today.ToString("yyyy-MM-dd");
                LoadMatrix();
            }
        }

        protected void btnLoadSlots_Click(object sender, EventArgs e)
        {
            helper.EnsureAvailabilityWindow();
            LoadMatrix();
        }

        protected void btnConfirmApply_Click(object sender, EventArgs e)
        {
            int availabilityId;
            int createdByStaffId;

            if (!int.TryParse(hfAvailabilityID.Value, out availabilityId))
            {
                ShowAlert("Invalid slot selected.");
                return;
            }

            if (!int.TryParse(hfCreatedByStaffID.Value, out createdByStaffId))
            {
                ShowAlert("Invalid slot state.");
                return;
            }

            if (createdByStaffId == 1)
            {
                ShowAlert("This slot is locked and cannot be modified.");
                return;
            }

            string newMode = ddlModalMode.SelectedValue;

            int staffId = 2;
            if (Session["StaffID"] != null)
            {
                int.TryParse(Convert.ToString(Session["StaffID"]), out staffId);
                if (staffId <= 0)
                    staffId = 2;
            }

            try
            {
                UpdateSlotMode(availabilityId, newMode, staffId);
                LoadMatrix();
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "closeModalAfterApply",
                    "closeEditModal(); alert('Court slot updated successfully.');",
                    true
                );
            }
            catch (Exception ex)
            {
                string safe = (ex.Message ?? "Unable to update slot.").Replace("'", "\\'");
                ScriptManager.RegisterStartupScript(
                    this,
                    GetType(),
                    "applyErr",
                    "alert('" + safe + "');",
                    true
                );
            }
        }

        private void LoadMatrix()
        {
            DateTime targetDate;
            if (!DateTime.TryParse(txtDate.Text, out targetDate))
            {
                targetDate = DateTime.Today;
                txtDate.Text = targetDate.ToString("yyyy-MM-dd");
            }

            DataTable courts = GetActiveCourts();
            DataTable slots = GetSlotsForDate(targetDate.Date);

            litMatrixTable.Text = BuildMatrixHtml(courts, slots);
        }

        private DataTable GetActiveCourts()
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
SELECT CourtID, CourtNumber, SportName
FROM tblCourt
WHERE IsActive = 1
ORDER BY CourtNumber;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        private DataTable GetSlotsForDate(DateTime targetDate)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
SELECT
    ca.AvailabilityID,
    ca.CourtID,
    c.CourtNumber,
    c.SportName,
    ca.[Date],
    ca.StartTime,
    ca.EndTime,
    CONVERT(VARCHAR(5), ca.StartTime, 108) + ' - ' + CONVERT(VARCHAR(5), ca.EndTime, 108) AS TimeRange,
    ca.ModeName,
    ca.CreatedByStaffID,

    CASE
        WHEN r.ReservationID IS NOT NULL THEN
            COALESCE(
                pa.Firstname + ' ' + pa.Lastname,
                pw.Firstname + ' ' + pw.Lastname,
                'Reserved Player'
            )
        ELSE 'Open / No Assigned Player'
    END AS TakenByName,

    CASE
        WHEN s.SessionID IS NOT NULL THEN 'Active Session'
        WHEN r.ReservationID IS NOT NULL THEN 'Reservation'
        WHEN ca.CreatedByStaffID = 1 THEN 'System'
        ELSE 'Manual Override'
    END AS SourceLabel

FROM tblCourtAvailability ca
INNER JOIN tblCourt c
    ON c.CourtID = ca.CourtID

LEFT JOIN tblReservation r
    ON r.CourtID = ca.CourtID
   AND r.ResDate = ca.[Date]
   AND r.StartTime = ca.StartTime
   AND r.EndTime = ca.EndTime
   AND r.ReservationStatusName = 'Approved'

LEFT JOIN tblPlayerAccount pa
    ON r.UserID = pa.UserID
LEFT JOIN tblPlayerWalkIn pw
    ON r.WalkInID = pw.WalkInID

LEFT JOIN tblActiveSession s
    ON s.CourtID = ca.CourtID
   AND CAST(s.StartTime AS DATE) = ca.[Date]
   AND CAST(s.StartTime AS TIME) = ca.StartTime
   AND CAST(s.ExpectedEndTime AS TIME) = ca.EndTime
   AND s.StatusName = 'Active'

WHERE ca.[Date] = @TargetDate
  AND c.IsActive = 1
ORDER BY ca.StartTime, c.CourtNumber;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@TargetDate", targetDate);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        conn.Open();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        private string BuildMatrixHtml(DataTable courts, DataTable slots)
        {
            StringBuilder sb = new StringBuilder();

            List<DataRow> courtRows = courts.AsEnumerable()
                .OrderBy(r => Convert.ToInt32(r["CourtNumber"]))
                .ToList();

            List<string> timeRanges = slots.AsEnumerable()
                .Select(r => Convert.ToString(r["TimeRange"]))
                .Distinct()
                .ToList();

            sb.Append("<div class='matrix-wrap'>");
            sb.Append("<table class='matrix-table'>");

            // Header
            sb.Append("<thead><tr>");
            sb.Append("<th class='time-col'>Time Block</th>");

            foreach (DataRow court in courtRows)
            {
                string courtTitle = "Court " + Convert.ToString(court["CourtNumber"]) +
                                    " (" + Convert.ToString(court["SportName"]) + ")";
                sb.Append("<th>");
                sb.Append(HttpUtility.HtmlEncode(courtTitle));
                sb.Append("</th>");
            }

            sb.Append("</tr></thead>");
            sb.Append("<tbody>");

            foreach (string timeRange in timeRanges)
            {
                sb.Append("<tr>");
                sb.Append("<td class='time-cell'>");
                sb.Append(HttpUtility.HtmlEncode(timeRange));
                sb.Append("</td>");

                foreach (DataRow court in courtRows)
                {
                    int courtId = Convert.ToInt32(court["CourtID"]);

                    DataRow slot = slots.AsEnumerable().FirstOrDefault(r =>
                        Convert.ToInt32(r["CourtID"]) == courtId &&
                        Convert.ToString(r["TimeRange"]) == timeRange
                    );

                    if (slot == null)
                    {
                        sb.Append("<td><div class='slot-box empty-slot'>");
                        sb.Append("<div class='slot-mode'>No Slot</div>");
                        sb.Append("<div class='slot-name'>--</div>");
                        sb.Append("</div></td>");
                        continue;
                    }

                    int availabilityId = Convert.ToInt32(slot["AvailabilityID"]);
                    string modeName = Convert.ToString(slot["ModeName"]);
                    string takenByName = Convert.ToString(slot["TakenByName"]);
                    string sourceLabel = Convert.ToString(slot["SourceLabel"]);
                    int createdByStaffId = Convert.ToInt32(slot["CreatedByStaffID"]);
                    bool isLocked = createdByStaffId == 1;

                    string slotClass = GetSlotCssClass(modeName, isLocked);
                    string safeTime = JsEncode(Convert.ToString(slot["TimeRange"]));
                    string safeCourt = JsEncode("Court " + Convert.ToString(court["CourtNumber"]) + " (" + Convert.ToString(court["SportName"]) + ")");
                    string safeMode = JsEncode(modeName);
                    string safeTaken = JsEncode(takenByName);

                    string click = "openEditModal(" +
                   availabilityId + ", '" +
                   safeTime + "', '" +
                   safeCourt + "', '" +
                   safeMode + "', '" +
                   safeTaken + "', '" +
                   createdByStaffId + "')";


                    sb.Append("<td>");
                    sb.Append("<div class='slot-box " + slotClass + "' onclick=\"" + click + "\">");

                    sb.Append("<div class='slot-mode'>");
                    sb.Append(HttpUtility.HtmlEncode(modeName));
                    sb.Append("</div>");

                    sb.Append("<div class='slot-name'>");
                    if (string.IsNullOrWhiteSpace(takenByName) || takenByName == "Open / No Assigned Player")
                        sb.Append("<span class='muted-text'>Open / No Assigned Player</span>");
                    else
                        sb.Append(HttpUtility.HtmlEncode(takenByName));
                    sb.Append("</div>");

                    sb.Append("</div>");
                    sb.Append("</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
            sb.Append("</table>");
            sb.Append("</div>");

            return sb.ToString();
        }

        private string GetSlotCssClass(string modeName, bool isLocked)
        {
            string baseClass;

            switch ((modeName ?? "").Trim())
            {
                case "Queue":
                    baseClass = "queue";
                    break;
                case "Reservation":
                    baseClass = "reservation";
                    break;
                case "Closed":
                    baseClass = "closed";
                    break;
                default:
                    baseClass = "pfa";
                    break;
            }

            if (isLocked)
                baseClass += " locked";

            return baseClass;
        }

        private void UpdateSlotMode(int availabilityId, string newMode, int staffId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                string sql = @"
UPDATE tblCourtAvailability
SET ModeName = @ModeName,
    CreatedByStaffID = @StaffID
WHERE AvailabilityID = @AvailabilityID
  AND CreatedByStaffID <> 1;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ModeName", newMode);
                    cmd.Parameters.AddWithValue("@StaffID", staffId);
                    cmd.Parameters.AddWithValue("@AvailabilityID", availabilityId);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows <= 0)
                    {
                        throw new InvalidOperationException("This slot is locked and cannot be modified.");
                    }
                }
            }
        }

        private void ShowAlert(string message)
        {
            string safe = (message ?? "").Replace("'", "\\'");
            ScriptManager.RegisterStartupScript(
                this,
                GetType(),
                Guid.NewGuid().ToString("N"),
                "alert('" + safe + "');",
                true
            );
        }

        private string JsEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ");
        }
    }
}