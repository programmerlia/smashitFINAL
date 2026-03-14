using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Smash_IT.Helpers
{
    public class CourtAvailabilityHelper
    {
        private readonly string CS =
            ConfigurationManager.ConnectionStrings["soapergandahannali"].ConnectionString;

        private const int SYSTEM_STAFF_ID = 2;

        private DateTime WindowStartDate => DateTime.Today;
        private DateTime WindowEndDate => DateTime.Today.AddDays(2);

        /// <summary>
        /// Ensures tblCourtAvailability always contains only:
        /// today, tomorrow, and day after tomorrow.
        ///
        /// Rules:
        /// - delete rows before today
        /// - delete rows after today+2
        /// - insert missing rows in the current 3-day window
        /// - newly inserted rows default to PlayForAll
        ///
        /// Existing rows are NOT reset.
        /// </summary>
        public void SyncAvailabilityWindow()
        {
            DateTime startDate = WindowStartDate;
            DateTime endDate = WindowEndDate;

            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();

                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        DeleteAvailabilityOutsideWindow(con, tx, startDate, endDate);
                        EnsureWindowExists(con, tx, startDate, endDate);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Only syncs if needed.
        /// Good for calling from first page load / first login / first open.
        /// </summary>
        public void EnsureAvailabilityWindow()
        {
            bool needsSync = false;

            using (SqlConnection con = new SqlConnection(CS))
            {
                con.Open();

                string sql = @"
SELECT
    SUM(CASE WHEN [Date] < @Today THEN 1 ELSE 0 END) AS OldRowCount,
    SUM(CASE WHEN [Date] > @EndDate THEN 1 ELSE 0 END) AS ExtraFutureRowCount,
    SUM(CASE WHEN [Date] = @EndDate THEN 1 ELSE 0 END) AS EndDateRowCount
FROM tblCourtAvailability;";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today);
                    cmd.Parameters.AddWithValue("@EndDate", DateTime.Today.AddDays(2));

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            int oldRowCount = rdr["OldRowCount"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(rdr["OldRowCount"]);

                            int extraFutureRowCount = rdr["ExtraFutureRowCount"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(rdr["ExtraFutureRowCount"]);

                            int endDateRowCount = rdr["EndDateRowCount"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(rdr["EndDateRowCount"]);

                            needsSync = oldRowCount > 0
                                     || extraFutureRowCount > 0
                                     || endDateRowCount == 0;
                        }
                        else
                        {
                            needsSync = true;
                        }
                    }
                }
            }

            if (needsSync)
            {
                SyncAvailabilityWindow();
            }
        }

        private void DeleteAvailabilityOutsideWindow(
            SqlConnection con,
            SqlTransaction tx,
            DateTime startDate,
            DateTime endDate)
        {
            string sql = @"
DELETE FROM tblCourtAvailability
WHERE [Date] < @StartDate
   OR [Date] > @EndDate;";

            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureWindowExists(
            SqlConnection con,
            SqlTransaction tx,
            DateTime startDate,
            DateTime endDate)
        {
            DataTable courts = GetActiveCourts(con, tx);

            foreach (DataRow row in courts.Rows)
            {
                int courtId = Convert.ToInt32(row["CourtID"]);

                List<(TimeSpan StartTime, TimeSpan EndTime)> slots =
                    GetExistingSlotsForCourt(con, tx, courtId);

                if (slots.Count == 0)
                {
                    slots = GetDefaultSlots();
                }

                for (DateTime d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
                {
                    foreach (var slot in slots)
                    {
                        InsertAvailabilityIfMissing(
                            con,
                            tx,
                            courtId,
                            d.Date,
                            slot.StartTime,
                            slot.EndTime
                        );
                    }
                }
            }
        }

        private void InsertAvailabilityIfMissing(
            SqlConnection con,
            SqlTransaction tx,
            int courtId,
            DateTime date,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            string sql = @"
IF NOT EXISTS
(
    SELECT 1
    FROM tblCourtAvailability
    WHERE CourtID = @CourtID
      AND [Date] = @Date
      AND StartTime = @StartTime
      AND EndTime = @EndTime
)
BEGIN
    INSERT INTO tblCourtAvailability
    (
        CourtID,
        [Date],
        StartTime,
        EndTime,
        ModeName,
        CreatedByStaffID
    )
    VALUES
    (
        @CourtID,
        @Date,
        @StartTime,
        @EndTime,
        'PlayForAll',
        @CreatedByStaffID
    )
END";

            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId);
                cmd.Parameters.AddWithValue("@Date", date);
                cmd.Parameters.AddWithValue("@StartTime", startTime);
                cmd.Parameters.AddWithValue("@EndTime", endTime);
                cmd.Parameters.AddWithValue("@CreatedByStaffID", SYSTEM_STAFF_ID);
                cmd.ExecuteNonQuery();
            }
        }

        private DataTable GetActiveCourts(SqlConnection con, SqlTransaction tx)
        {
            string sql = @"
SELECT CourtID, CourtNumber, SportName
FROM tblCourt
WHERE IsActive = 1
ORDER BY CourtNumber;";

            using (SqlDataAdapter da = new SqlDataAdapter(sql, con))
            {
                da.SelectCommand.Transaction = tx;

                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private List<(TimeSpan StartTime, TimeSpan EndTime)> GetExistingSlotsForCourt(
            SqlConnection con,
            SqlTransaction tx,
            int courtId)
        {
            string sql = @"
SELECT DISTINCT StartTime, EndTime
FROM tblCourtAvailability
WHERE CourtID = @CourtID
ORDER BY StartTime, EndTime;";

            List<(TimeSpan StartTime, TimeSpan EndTime)> slots =
                new List<(TimeSpan StartTime, TimeSpan EndTime)>();

            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.AddWithValue("@CourtID", courtId);

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        TimeSpan start = (TimeSpan)rdr["StartTime"];
                        TimeSpan end = (TimeSpan)rdr["EndTime"];
                        slots.Add((start, end));
                    }
                }
            }

            return slots;
        }

        private List<(TimeSpan StartTime, TimeSpan EndTime)> GetDefaultSlots()
        {
            return new List<(TimeSpan StartTime, TimeSpan EndTime)>
            {
                (new TimeSpan(8, 0, 0),  new TimeSpan(8, 30, 0)),
                (new TimeSpan(8, 30, 0), new TimeSpan(9, 0, 0)),
                (new TimeSpan(9, 0, 0),  new TimeSpan(9, 30, 0)),
                (new TimeSpan(9, 30, 0), new TimeSpan(10, 0, 0)),
                (new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0)),
                (new TimeSpan(10, 30, 0), new TimeSpan(11, 0, 0)),
                (new TimeSpan(11, 0, 0), new TimeSpan(11, 30, 0)),
                (new TimeSpan(11, 30, 0), new TimeSpan(12, 0, 0)),
                (new TimeSpan(12, 0, 0), new TimeSpan(12, 30, 0)),
                (new TimeSpan(12, 30, 0), new TimeSpan(13, 0, 0)),
                (new TimeSpan(13, 0, 0), new TimeSpan(13, 30, 0)),
                (new TimeSpan(13, 30, 0), new TimeSpan(14, 0, 0)),
                (new TimeSpan(14, 0, 0), new TimeSpan(14, 30, 0)),
                (new TimeSpan(14, 30, 0), new TimeSpan(15, 0, 0)),
                (new TimeSpan(15, 0, 0), new TimeSpan(15, 30, 0)),
                (new TimeSpan(15, 30, 0), new TimeSpan(16, 0, 0)),
                (new TimeSpan(16, 0, 0), new TimeSpan(16, 30, 0)),
                (new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)),
                (new TimeSpan(17, 0, 0), new TimeSpan(17, 30, 0)),
                (new TimeSpan(17, 30, 0), new TimeSpan(18, 0, 0)),
                (new TimeSpan(18, 0, 0), new TimeSpan(18, 30, 0)),
                (new TimeSpan(18, 30, 0), new TimeSpan(19, 0, 0)),
                (new TimeSpan(19, 0, 0), new TimeSpan(19, 30, 0)),
                (new TimeSpan(19, 30, 0), new TimeSpan(20, 0, 0)),
                (new TimeSpan(20, 0, 0), new TimeSpan(20, 30, 0)),
                (new TimeSpan(20, 30, 0), new TimeSpan(21, 0, 0)),
                (new TimeSpan(21, 0, 0), new TimeSpan(21, 30, 0)),
                (new TimeSpan(21, 30, 0), new TimeSpan(22, 0, 0))
            };
        }
    }
}