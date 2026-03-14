using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI;

namespace Smash_IT.controls
{
    public partial class CourtLive : System.Web.UI.UserControl
    {
        private readonly string connString =
            ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadLiveCourts();
            }
        }

        protected void tmrLive_Tick(object sender, EventArgs e)
        {
            LoadLiveCourts();
            upLive.Update();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadLiveCourts();
            upLive.Update();
        }

        private void LoadLiveCourts()
        {
            DateTime now = DateTime.Now;
            List<CourtLiveCard> cards = new List<CourtLiveCard>();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                DataTable dt = GetLiveCourtSnapshot(conn, now);

                foreach (DataRow row in dt.Rows)
                {
                    CourtLiveCard card = BuildCardFromSnapshot(row, now);
                    cards.Add(card);
                }
            }

            rptLiveCourts.DataSource = cards
                .OrderBy(x => GetCourtRenderOrder(x.CourtNumber))
                .ToList();

            rptLiveCourts.DataBind();
        }

        private DataTable GetLiveCourtSnapshot(SqlConnection conn, DateTime now)
        {
            string sql = @"
SELECT
    c.CourtID,
    c.CourtNumber,
    c.SportName,

    act.PlayerName AS ActivePlayerName,
    act.StartTime AS ActiveStartTime,
    act.ExpectedEndTime AS ActiveExpectedEndTime,
    act.SessionMode,

    curRes.PlayerName AS CurrentReservationPlayerName,
    curRes.StartTime AS CurrentReservationStartTime,
    curRes.EndTime AS CurrentReservationEndTime,

    nextRes.PlayerName AS NextReservationPlayerName,
    nextRes.StartTime AS NextReservationStartTime,
    nextRes.EndTime AS NextReservationEndTime,

    nextQ.PlayerName AS NextQueuePlayerName

FROM tblCourt c

OUTER APPLY
(
    SELECT TOP 1
        CASE
            WHEN s.QueueID IS NOT NULL THEN ISNULL(ev.Title, 'Event Queue')
            ELSE COALESCE(
                paRes.Firstname + ' ' + paRes.Lastname,
                pwRes.Firstname + ' ' + pwRes.Lastname,
                paQ.Firstname + ' ' + paQ.Lastname,
                pwQ.Firstname + ' ' + pwQ.Lastname,
                pwP.Firstname + ' ' + pwP.Lastname,
                'Player'
            )
        END AS PlayerName,
        s.StartTime,
        s.ExpectedEndTime,
        CASE
            WHEN s.ReservationID IS NOT NULL THEN 'Reservation'
            WHEN s.QueueID IS NOT NULL THEN 'Queue'
            WHEN s.PAYCID IS NOT NULL THEN 'PlayForAll'
            ELSE 'Active Session'
        END AS SessionMode
    FROM tblActiveSession s
    LEFT JOIN tblReservation r2
        ON s.ReservationID = r2.ReservationID
    LEFT JOIN tblPlayerAccount paRes
        ON r2.UserID = paRes.UserID
    LEFT JOIN tblPlayerWalkIn pwRes
        ON r2.WalkInID = pwRes.WalkInID

    LEFT JOIN tblCourtQueue q
        ON s.QueueID = q.QueueID
    LEFT JOIN tblEvent ev
        ON q.EventID = ev.EventID
    LEFT JOIN tblPlayerAccount paQ
        ON q.UserID = paQ.UserID
    LEFT JOIN tblPlayerWalkIn pwQ
        ON q.WalkInID = pwQ.WalkInID

    LEFT JOIN tblPlayAllYouCanRegistry p
        ON s.PAYCID = p.PAYCID
    LEFT JOIN tblPlayerWalkIn pwP
        ON p.WalkInID = pwP.WalkInID

    WHERE s.CourtID = c.CourtID
      AND s.StatusName = 'Active'
      AND s.StartTime <= @NowDateTime
      AND (s.ActualEndTime IS NULL OR s.ActualEndTime > @NowDateTime)
    ORDER BY s.StartTime DESC
) act

OUTER APPLY
(
    SELECT TOP 1
        COALESCE(
            pa.Firstname + ' ' + pa.Lastname,
            pw.Firstname + ' ' + pw.Lastname,
            'Reserved Player'
        ) AS PlayerName,
        r.StartTime,
        r.EndTime
    FROM tblReservation r
    LEFT JOIN tblPlayerAccount pa
        ON r.UserID = pa.UserID
    LEFT JOIN tblPlayerWalkIn pw
        ON r.WalkInID = pw.WalkInID
    WHERE r.CourtID = c.CourtID
      AND r.ResDate = @TargetDate
      AND r.StartTime <= @TargetTime
      AND r.EndTime > @TargetTime
      AND r.ReservationStatusName IN ('Pending', 'Approved')
    ORDER BY r.StartTime
) curRes

OUTER APPLY
(
    SELECT TOP 1
        COALESCE(
            pa.Firstname + ' ' + pa.Lastname,
            pw.Firstname + ' ' + pw.Lastname,
            'Reserved Player'
        ) AS PlayerName,
        r.StartTime,
        r.EndTime
    FROM tblReservation r
    LEFT JOIN tblPlayerAccount pa
        ON r.UserID = pa.UserID
    LEFT JOIN tblPlayerWalkIn pw
        ON r.WalkInID = pw.WalkInID
    WHERE r.CourtID = c.CourtID
      AND r.ResDate = @TargetDate
      AND r.StartTime > @TargetTime
      AND r.ReservationStatusName IN ('Pending', 'Approved')
    ORDER BY r.StartTime ASC
) nextRes

OUTER APPLY
(
    SELECT TOP 1
        COALESCE(
            pa.Firstname + ' ' + pa.Lastname,
            pw.Firstname + ' ' + pw.Lastname,
            'Queued Player'
        ) AS PlayerName
    FROM tblCourtQueue cq
    LEFT JOIN tblPlayerAccount pa
        ON cq.UserID = pa.UserID
    LEFT JOIN tblPlayerWalkIn pw
        ON cq.WalkInID = pw.WalkInID
    WHERE cq.CourtID = c.CourtID
      AND cq.QueueDate = @TargetDate
      AND cq.StatusName = 'Waiting'
    ORDER BY cq.QueueNumber ASC
) nextQ

WHERE c.IsActive = 1
ORDER BY c.CourtNumber;";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@NowDateTime", now);
                cmd.Parameters.AddWithValue("@TargetDate", now.Date);
                cmd.Parameters.AddWithValue("@TargetTime", now.TimeOfDay);

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }

                return dt;
            }
        }

        private CourtLiveCard BuildCardFromSnapshot(DataRow row, DateTime now)
        {
            int courtNumber = Convert.ToInt32(row["CourtNumber"]);

            CourtLiveCard card = new CourtLiveCard
            {
                CourtID = Convert.ToInt32(row["CourtID"]),
                CourtNumber = courtNumber,
                SportName = Convert.ToString(row["SportName"]),
                CurrentMode = "PlayForAll",
                StatusCssClass = "available",
                StatusColor = "#22c55e",
                StatusDisplay = "Available",
                CurrentLabel = "Currently Taken By",
                CurrentPlayerName = "Open Play",
                CurrentTimeRange = "Now",
                NextPlayerName = "None",
                NextTimeRange = "--:--",
                SourceLabel = "Live Court Status",
                Remarks = "Court is currently free.",
                LayoutCssClass = GetLayoutCssClass(courtNumber),
                OrientationCssClass = GetOrientationCssClass(courtNumber)
            };

            bool hasActiveSession = row["ActiveStartTime"] != DBNull.Value;
            bool hasCurrentReservation = row["CurrentReservationStartTime"] != DBNull.Value;
            bool hasNextReservation = row["NextReservationStartTime"] != DBNull.Value;
            bool hasNextQueue = row["NextQueuePlayerName"] != DBNull.Value &&
                                !string.IsNullOrWhiteSpace(Convert.ToString(row["NextQueuePlayerName"]));

            if (hasActiveSession)
            {
                card.StatusCssClass = "occupied";
                card.StatusColor = "#1e3a8a";
                card.StatusDisplay = "Occupied";
                card.CurrentPlayerName = Convert.ToString(row["ActivePlayerName"]);

                DateTime start = Convert.ToDateTime(row["ActiveStartTime"]);
                DateTime expectedEnd = Convert.ToDateTime(row["ActiveExpectedEndTime"]);
                card.CurrentTimeRange = start.ToString("hh:mm tt") + " - " + expectedEnd.ToString("hh:mm tt");

                string sessionMode = Convert.ToString(row["SessionMode"]);
                card.CurrentMode = string.IsNullOrWhiteSpace(sessionMode) ? "Active Session" : sessionMode;

                if (card.CurrentMode == "Queue")
                {
                    card.CurrentLabel = "Active Event";
                    card.Remarks = "Court currently assigned to an event queue.";
                }
                else if (card.CurrentMode == "Reservation")
                {
                    card.CurrentLabel = "Reserved For";
                    card.Remarks = "Reserved player is currently using this court.";
                }
                else
                {
                    card.CurrentLabel = "Currently Taken By";
                    card.Remarks = "Court currently in use.";
                }

                card.SourceLabel = "Active Session";
            }
            else if (hasCurrentReservation)
            {
                card.StatusCssClass = "available";
                card.StatusColor = "#22c55e";
                card.StatusDisplay = "Reserved";
                card.CurrentMode = "Reservation";
                card.CurrentLabel = "Reserved For";
                card.CurrentPlayerName = Convert.ToString(row["CurrentReservationPlayerName"]);

                TimeSpan start = (TimeSpan)row["CurrentReservationStartTime"];
                TimeSpan end = (TimeSpan)row["CurrentReservationEndTime"];
                card.CurrentTimeRange = FormatTimeRange(start, end);

                card.SourceLabel = "Reservation";
                card.Remarks = "Reserved player has this slot.";
            }
            else
            {
                card.StatusCssClass = "available";
                card.StatusColor = "#22c55e";
                card.StatusDisplay = "Open";
                card.CurrentMode = "PlayForAll";
                card.CurrentLabel = "Currently Taken By";
                card.CurrentPlayerName = "Open Play";
                card.CurrentTimeRange = "Now";
                card.SourceLabel = "Live Court Status";
                card.Remarks = "Court is currently free.";
            }

            if (hasNextReservation)
            {
                TimeSpan nextStart = (TimeSpan)row["NextReservationStartTime"];
                TimeSpan nextEnd = (TimeSpan)row["NextReservationEndTime"];
                card.NextPlayerName = Convert.ToString(row["NextReservationPlayerName"]);
                card.NextTimeRange = FormatTimeRange(nextStart, nextEnd);
            }
            else if (card.CurrentMode == "Queue" && hasNextQueue)
            {
                card.NextPlayerName = Convert.ToString(row["NextQueuePlayerName"]);
                card.NextTimeRange = "On Deck";
            }
            else if (card.CurrentMode == "PlayForAll")
            {
                card.NextPlayerName = "Open Play";
                card.NextTimeRange = "No fixed next player";
            }
            else
            {
                card.NextPlayerName = "None";
                card.NextTimeRange = "--:--";
            }

            return card;
        }

        private string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            return DateTime.Today.Add(start).ToString("hh:mm tt") + " - " +
                   DateTime.Today.Add(end).ToString("hh:mm tt");
        }

        private string GetLayoutCssClass(int courtNumber)
        {
            switch (courtNumber)
            {
                case 1: return "slot-court-1";
                case 2: return "slot-court-2";
                case 3: return "slot-court-3";
                case 4: return "slot-court-4";
                case 5: return "slot-court-5";
                case 6: return "slot-court-6";
                default: return "";
            }
        }

        private string GetOrientationCssClass(int courtNumber)
        {
            return courtNumber == 5 ? "horizontal" : "";
        }

        private int GetCourtRenderOrder(int courtNumber)
        {
            switch (courtNumber)
            {
                case 6: return 1;
                case 4: return 2;
                case 3: return 3;
                case 2: return 4;
                case 1: return 5;
                case 5: return 6;
                default: return 999;
            }
        }

        public class CourtLiveCard
        {
            public int CourtID { get; set; }
            public int CourtNumber { get; set; }
            public string SportName { get; set; }
            public string CurrentMode { get; set; }
            public string StatusCssClass { get; set; }
            public string StatusColor { get; set; }
            public string StatusDisplay { get; set; }
            public string CurrentLabel { get; set; }
            public string CurrentPlayerName { get; set; }
            public string CurrentTimeRange { get; set; }
            public string NextPlayerName { get; set; }
            public string NextTimeRange { get; set; }
            public string SourceLabel { get; set; }
            public string Remarks { get; set; }
            public string LayoutCssClass { get; set; }
            public string OrientationCssClass { get; set; }
        }
    }
}