USE dbsmashitFINAL
GO
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    /* =========================================================
       0) Seed dates
       ========================================================= */
    DECLARE @SeedDates TABLE ([D] DATE PRIMARY KEY);
    INSERT INTO @SeedDates ([D])
    VALUES ('2026-02-27'), ('2026-02-28'), ('2026-03-01'), ('2026-03-02');

    /* =========================================================
       1) STAFF + PLAYERS (idempotent)
       ========================================================= */

        INSERT INTO tblStaffAccount (Lastname, Firstname,Email, Username, [Password], RoleName)
        VALUES ('Olayvar', 'Hannali','hannaliolayvar@gmail.com','admin','pass','admin'),('Noda','Isaiah','isaiahandreinoda@gmail.com','receptionist','pass','receptionist');

    DECLARE @AdminStaffID INT = 1

        INSERT INTO tblPlayerAccount (Lastname, Firstname, Email, PhoneNumber, Username, [Password])
        VALUES ('Olayvar', 'Elyza','olayvarelyzarosa@gmail.com','09171234567','elyza','pass'),
        ('Olayvar','Elexali' ,'elexaliolayvar9711@gmail.com','09179876543','elexali','pass');


    DECLARE @UserElyza INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elyza');
    DECLARE @UserElexali INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elexali');


    /* =========================================================
       2) COURTS (6 fixed) (idempotent)  -- SportName now
       ========================================================= */
        INSERT INTO tblCourt(CourtNumber, SportName, IsActive) VALUES (1,'badminton',1),
        (2,'badminton',1),
        (3,'badminton',1),
        (4,'badminton',1),
        (5,'pickleball',1),
        (6,'pickleball',1);

    DECLARE @Court1 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=1);
    DECLARE @Court2 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=2);
    DECLARE @Court3 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=3);
    DECLARE @Court4 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=4);
    DECLARE @Court5 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=5);
    DECLARE @Court6 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=6);


    /* =========================================================
       3) EQUIPMENT MODELS + ITEMS (idempotent)
       ========================================================= */
     INSERT INTO tblEquipmentModel(EquipmentType, EquipmentSpec, DefaultRentalPrice)
        VALUES ('Badminton Racket','JXXS-10',100.00),
        ('Badminton Racket','JXXS-11',100.00),
        ('ShuttleCock','AULA',100.00),
        ('Pickleball Paddle','JOOLA FXS-26',100.00);

    -- ensure >= 12 items exist
    IF (SELECT COUNT(*) FROM tblEquipmentItem) < 12
    BEGIN
        DECLARE @M_J10 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentType='Badminton Racket' AND EquipmentSpec='JXXS-10');
        DECLARE @M_J11 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentType='Badminton Racket' AND EquipmentSpec='JXXS-11');
        DECLARE @M_AULA INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentType='ShuttleCock' AND EquipmentSpec='AULA');
        DECLARE @M_F26 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentType='Pickleball Paddle' AND EquipmentSpec='JOOLA FXS-26');

        INSERT INTO tblEquipmentItem(ModelID) VALUES
        (@M_J10),(@M_J10),(@M_J10),(@M_J10),
        (@M_J11),(@M_J11),(@M_J11),(@M_J11),
        (@M_AULA),(@M_AULA),
        (@M_F26),(@M_F26);
    END

    DECLARE @ItemA INT = (SELECT MIN(ItemID) FROM tblEquipmentItem);
    DECLARE @ItemB INT = (SELECT MIN(ItemID)+1 FROM tblEquipmentItem);


    /* =========================================================
       4) ABOUT US (optional)
       ========================================================= */
   INSERT INTO tblAboutUsMembers(Lastname, Firstname, Position)
        VALUES ('De Mesa', 'Richie','CEO Operations Manager'),
        ('Jonas', 'May','Reception & Queue Manager'),
        ('Laguente', 'Harvey' ,'UI/UX & Frontend Developer'),
        ('Unira', 'Mark Odrey' ,'Backend & Database Developer');


    /* =========================================================
       5) EVENTS per day + CourtPool (2,3,6) (idempotent)
          -- SportName now
       ========================================================= */
    INSERT INTO tblEvent (Title, SportName, EventDate, CreatedByStaffID, IsActive)
    SELECT
        CONCAT('Queue Event (Seed) - ', CONVERT(VARCHAR(10), d.[D], 120)),
        'badminton',
        d.[D],
        @AdminStaffID,
        1
    FROM @SeedDates d
    WHERE NOT EXISTS (
        SELECT 1 FROM tblEvent e
        WHERE e.EventDate=d.[D]
          AND e.Title = CONCAT('Queue Event (Seed) - ', CONVERT(VARCHAR(10), d.[D], 120))
    );

    INSERT INTO tblEventCourtPool (EventID, CourtID)
    SELECT e.EventID, c.CourtID
    FROM tblEvent e
    JOIN @SeedDates d ON d.[D]=e.EventDate
    JOIN tblCourt c ON c.CourtNumber IN (2,3,6)
    WHERE e.Title = CONCAT('Queue Event (Seed) - ', CONVERT(VARCHAR(10), d.[D], 120))
      AND NOT EXISTS (
          SELECT 1 FROM tblEventCourtPool p
          WHERE p.EventID=e.EventID AND p.CourtID=c.CourtID
      );


    /* =========================================================
       6) WALK-INS per day (queue + pfa) (idempotent)
       ========================================================= */
    DECLARE @W TABLE(SeedDate DATE, Lastname VARCHAR(100), Firstname VARCHAR(100),BasePaid BIT, QueuePaid BIT);

    INSERT INTO @W VALUES
   ('2026-02-27','Reyes','Juan',1,1),
('2026-02-27','Santos','Maria',1,1),
('2026-02-27','Dela Cruz','Paolo',1,1),
('2026-02-27','Garcia','Anne',1,1),
('2026-02-27','Mendoza','Carlo',1,1),
('2026-02-27','Torres','Bianca',1,1),
('2026-02-27','Navarro','Miguel',1,0),
('2026-02-27','Castillo','Jasmine',1,0),

('2026-02-28','Ramos','Daniel',1,1),
('2026-02-28','Flores','Angela',1,1),
('2026-02-28','Aquino','Mark',1,1),
('2026-02-28','Villanueva','Patricia',1,1),
('2026-02-28','Cruz','Joshua',1,1),
('2026-02-28','Valdez','Nina',1,1),
('2026-02-28','Bautista','Kevin',1,0),
('2026-02-28','Gutierrez','Lara',1,0),

('2026-03-01','Silva','Noah',1,1),
('2026-03-01','Fernandez','Sofia',1,1),
('2026-03-01','Lim','Ethan',1,1),
('2026-03-01','Chua','Isabella',1,1),
('2026-03-01','Tan','Lucas',1,1),
('2026-03-01','Go','Mika',1,1),
('2026-03-01','Morales','Andrei',1,0),
('2026-03-01','Domingo','Bea',1,0),

('2026-03-02','Rivera','Leo',1,1),
('2026-03-02','Salazar','Clara',1,1),
('2026-03-02','Ortega','James',1,1),
('2026-03-02','Del Rosario','Kim',1,1),
('2026-03-02','Pineda','Sean',1,1),
('2026-03-02','Alvarez','Ria',1,1),
('2026-03-02','Lopez','Hannah',1,0),
('2026-03-02','Sison','Caleb',1,0);

    INSERT INTO tblPlayerWalkIn (UserID, Lastname, Firstname, BasePaid, QueuePaid, CreatedAt)
    SELECT NULL, w.Lastname, w.Firstname, w.BasePaid, w.QueuePaid,
           CAST(w.SeedDate AS DATETIME) + CAST('08:00' AS DATETIME)
    FROM @W w
    WHERE NOT EXISTS (
        SELECT 1 FROM tblPlayerWalkIn x
        WHERE x.Lastname=w.Lastname AND CAST(x.CreatedAt AS DATE)=w.SeedDate
    );


    /* =========================================================
       7) COURT AVAILABILITY (18:00-22:00 slots) all courts all days
          -- ModeName now*/



-- Slot generator: 08:00 to 22:00, 30 mins
DECLARE @Slots TABLE(StartTime TIME, EndTime TIME);
DECLARE @t TIME = '08:00';

WHILE (@t < '22:00')
BEGIN
    INSERT INTO @Slots(StartTime, EndTime)
    VALUES (@t, DATEADD(MINUTE, 30, @t));

    SET @t = DATEADD(MINUTE, 30, @t);
END;

-- Insert with variations
INSERT INTO tblCourtAvailability (CourtID, [Date], StartTime, EndTime, ModeName, CreatedByStaffID)
SELECT
    c.CourtID,
    d.[D],
    s.StartTime,
    s.EndTime,

    /* ===========================
       VARIATION RULES
       =========================== */
    CASE
        /* Court 2,3,6 = Queue always */
        WHEN c.CourtNumber IN (2,3,6) THEN 'Queue'

        /* Court 4: weekdays = PFA morning, weekends = Reservation whole day */
        WHEN c.CourtNumber = 4 AND DATENAME(WEEKDAY, d.[D]) IN ('Saturday','Sunday') THEN 'Reservation'
        WHEN c.CourtNumber = 4 AND s.StartTime < '12:00' THEN 'PlayForAll'
        WHEN c.CourtNumber = 4 THEN 'Reservation'

        /* Court 1: weekday evenings queue, weekends reservation */
        WHEN c.CourtNumber = 1 AND DATENAME(WEEKDAY, d.[D]) IN ('Saturday','Sunday') THEN 'Reservation'
        WHEN c.CourtNumber = 1 AND s.StartTime >= '18:00' THEN 'Queue'
        WHEN c.CourtNumber = 1 THEN 'Reservation'

        /* Court 5: some days close early */
        WHEN c.CourtNumber = 5 AND d.[D] IN ('2026-02-28','2026-03-02') AND s.StartTime >= '20:00' THEN 'Closed'
        WHEN c.CourtNumber = 5 AND s.StartTime >= '21:30' THEN 'Closed'
        WHEN c.CourtNumber = 5 THEN 'Reservation'

        /* Default */
        ELSE 'Reservation'
    END,
    @AdminStaffID
FROM tblCourt c
CROSS JOIN @SeedDates d
CROSS JOIN @Slots s
WHERE c.IsActive = 1
  AND NOT EXISTS (
      SELECT 1
      FROM tblCourtAvailability a
      WHERE a.CourtID = c.CourtID
        AND a.[Date] = d.[D]
        AND a.StartTime = s.StartTime
        AND a.EndTime = s.EndTime
  );

    /* =========================================================
       8) RESERVATIONS (2/day)
          -- ReservationStatusName now (string)
       ========================================================= */
    INSERT INTO tblReservation
        (UserID, WalkInID, PlayerNumber, CourtID, ResDate, StartTime, EndTime,
         IsPaid, ReservationStatusName, RequiredAmount, ApprovedByStaffID)
    SELECT
        @UserElyza, NULL, 4, @Court1, d.[D], '18:00','19:00',
        1, 'Approved', 400, @AdminStaffID
    FROM @SeedDates d
    WHERE NOT EXISTS (
        SELECT 1 FROM tblReservation r
        WHERE r.ResDate=d.[D] AND r.CourtID=@Court1 AND r.StartTime='18:00' AND r.EndTime='19:00'
    );

    INSERT INTO tblReservation
        (UserID, WalkInID, PlayerNumber, CourtID, ResDate, StartTime, EndTime,
         IsPaid, ReservationStatusName, RequiredAmount, ApprovedByStaffID)
    SELECT
        @UserElexali, NULL, 2, @Court5, d.[D], '19:00','20:00',
        0, 'Pending', 630, NULL
    FROM @SeedDates d
    WHERE NOT EXISTS (
        SELECT 1 FROM tblReservation r
        WHERE r.ResDate=d.[D] AND r.CourtID=@Court5 AND r.StartTime='19:00' AND r.EndTime='20:00'
    );


    /* =========================================================
       9) EVENT PARTICIPANTS
          -- StatusName now (string)
       ========================================================= */
;WITH DayEvent AS (
    SELECT d.[D] SeedDate, e.EventID
    FROM @SeedDates d
    JOIN tblEvent e ON e.EventDate = d.[D]
    WHERE e.Title = CONCAT('Queue Event (Seed) - ', CONVERT(VARCHAR(10), d.[D], 120))
),
QPaid AS (
    SELECT
        CAST(w.CreatedAt AS DATE) AS SeedDate,
        w.WalkInID,
        w.FirstName,
        w.LastName,
        ROW_NUMBER() OVER (
            PARTITION BY CAST(w.CreatedAt AS DATE)
            ORDER BY w.CreatedAt, w.WalkInID
        ) AS RN
    FROM tblPlayerWalkIn w
    WHERE w.QueuePaid = 1
      AND CAST(w.CreatedAt AS DATE) IN (SELECT [D] FROM @SeedDates)
)
INSERT INTO tblEventParticipant (EventID, UserID, WalkInID, OrderNo, StatusName)
SELECT de.EventID, NULL, qp.WalkInID, qp.RN, 'Waiting'
FROM DayEvent de
JOIN QPaid qp ON qp.SeedDate = de.SeedDate
WHERE qp.RN BETWEEN 1 AND 4
  AND NOT EXISTS (
      SELECT 1
      FROM tblEventParticipant ep
      WHERE ep.EventID = de.EventID
        AND ep.OrderNo = qp.RN
  );


    /* =========================================================
       10) QUEUE
          -- QueueTypeName + StatusName now (strings)
       ========================================================= */
    ;WITH QPaid AS (
    SELECT
        CAST(w.CreatedAt AS DATE) AS SeedDate,
        w.WalkInID,
        w.FirstName,
        w.LastName,
        ROW_NUMBER() OVER (
            PARTITION BY CAST(w.CreatedAt AS DATE)
            ORDER BY w.CreatedAt, w.WalkInID
        ) AS RN
    FROM tblPlayerWalkIn w
    WHERE w.QueuePaid = 1
      AND CAST(w.CreatedAt AS DATE) IN (SELECT [D] FROM @SeedDates)
),
Assign AS (
    SELECT
        SeedDate, WalkInID, RN,
        CASE
            WHEN RN % 3 = 1 THEN @Court2
            WHEN RN % 3 = 2 THEN @Court3
            ELSE @Court6
        END AS CourtID
    FROM QPaid
    WHERE RN BETWEEN 1 AND 6
)
INSERT INTO tblCourtQueue
    (CourtID, UserID, WalkInID, ReservationID, QueueDate, EventID,
     QueueTypeName, QueueNumber, StatusName, LastModifiedByStaffID)
SELECT
    a.CourtID,
    NULL,
    a.WalkInID,
    NULL,
    a.SeedDate,
    e.EventID,
    'WalkIn',
    ROW_NUMBER() OVER (PARTITION BY a.SeedDate, a.CourtID ORDER BY a.RN) AS QueueNumber,
    'Waiting',
    @AdminStaffID
FROM Assign a
JOIN tblEvent e
  ON e.EventDate = a.SeedDate
 AND e.Title = CONCAT('Queue Event (Seed) - ', CONVERT(VARCHAR(10), a.SeedDate, 120))
WHERE NOT EXISTS (
    SELECT 1
    FROM tblCourtQueue q
    WHERE q.QueueDate = a.SeedDate
      AND q.CourtID = a.CourtID
      AND q.WalkInID = a.WalkInID
);
    -- Reservation queue entry on Court1 (QueueNumber=1)
    INSERT INTO tblCourtQueue
        (CourtID, UserID, WalkInID, ReservationID, QueueDate, EventID,
         QueueTypeName, QueueNumber, StatusName, LastModifiedByStaffID)
    SELECT
        @Court1,
        NULL,
        NULL,
        r.ReservationID,
        r.ResDate,
        NULL,
        'Reservation',
        1,
        'Waiting',
        @AdminStaffID
    FROM tblReservation r
    WHERE r.CourtID=@Court1 AND r.StartTime='18:00' AND r.EndTime='19:00'
      AND r.ResDate IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (
          SELECT 1 FROM tblCourtQueue q
          WHERE q.QueueDate=r.ResDate AND q.CourtID=@Court1 AND q.ReservationID=r.ReservationID
      );


    /* =========================================================
       11) PAYC (1 play-all-you-can walk-in/day)
          -- SportName + StatusName now (strings)
       ========================================================= */
   ;WITH PFA AS (
    SELECT
        CAST(w.CreatedAt AS DATE) AS SeedDate,
        w.WalkInID,
        ROW_NUMBER() OVER (
            PARTITION BY CAST(w.CreatedAt AS DATE)
            ORDER BY w.CreatedAt, w.WalkInID
        ) AS RN
    FROM tblPlayerWalkIn w
    WHERE w.QueuePaid = 0
      AND CAST(w.CreatedAt AS DATE) IN (SELECT [D] FROM @SeedDates)
)
INSERT INTO tblPlayAllYouCanRegistry (WalkInID, SportName, CheckInTime, StatusName)
SELECT
    p.WalkInID,
    CASE WHEN p.SeedDate IN ('2026-02-27','2026-02-28') THEN 'badminton' ELSE 'pickleball' END,
    DATEADD(HOUR, 18, CAST(p.SeedDate AS DATETIME)),  -- 18:00
    'Active'
FROM PFA p
WHERE p.RN = 1
  AND NOT EXISTS (
      SELECT 1
      FROM tblPlayAllYouCanRegistry x
      WHERE x.WalkInID = p.WalkInID
        AND CAST(x.CheckInTime AS DATE) = p.SeedDate
  );


    /* =========================================================
       12) ACTIVE SESSIONS
          -- StatusName now (string)
       ========================================================= */
    -- Court1 reservation session
    INSERT INTO tblActiveSession (CourtID, ReservationID, QueueID, PAYCID, StartTime, ExpectedEndTime, StatusName)
    SELECT
        @Court1,
        r.ReservationID,
        NULL,
        NULL,
        CAST(r.ResDate AS DATETIME) + CAST(r.StartTime AS DATETIME),
        CAST(r.ResDate AS DATETIME) + CAST(r.EndTime AS DATETIME),
        'Active'
    FROM tblReservation r
    WHERE r.CourtID=@Court1 AND r.StartTime='18:00' AND r.EndTime='19:00'
      AND r.ResDate IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (SELECT 1 FROM tblActiveSession s WHERE s.ReservationID=r.ReservationID);

    -- Court2 top queue session per day
    ;WITH TopQ AS (
        SELECT q.QueueDate, MIN(q.QueueID) AS TopQueueID
        FROM tblCourtQueue q
        WHERE q.CourtID=@Court2 AND q.QueueDate IN (SELECT [D] FROM @SeedDates)
        GROUP BY q.QueueDate
    )
    INSERT INTO tblActiveSession (CourtID, ReservationID, QueueID, PAYCID, StartTime, ExpectedEndTime, StatusName)
    SELECT
        @Court2,
        NULL,
        t.TopQueueID,
        NULL,
        CAST(t.QueueDate AS DATETIME) + CAST('18:00' AS DATETIME),
        CAST(t.QueueDate AS DATETIME) + CAST('18:30' AS DATETIME),
        'Active'
    FROM TopQ t
    WHERE NOT EXISTS (
        SELECT 1 FROM tblActiveSession s
        WHERE s.CourtID=@Court2 AND CAST(s.StartTime AS DATE)=t.QueueDate
    );

    -- Court4 PAYC session per day
    INSERT INTO tblActiveSession (CourtID, ReservationID, QueueID, PAYCID, StartTime, ExpectedEndTime, StatusName, ActualEndTime)
    SELECT
        @Court4,
        NULL,
        NULL,
        p.PAYCID,
        CAST(CAST(p.CheckInTime AS DATE) AS DATETIME) + CAST('18:00' AS DATETIME),
        CAST(CAST(p.CheckInTime AS DATE) AS DATETIME) + CAST('18:30' AS DATETIME),
        'Active',
        NULL
    FROM tblPlayAllYouCanRegistry p
    WHERE CAST(p.CheckInTime AS DATE) IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (SELECT 1 FROM tblActiveSession s WHERE s.PAYCID=p.PAYCID);


    /* =========================================================
       13) RENTALS (2/day)
       ========================================================= */
    -- Rental #1 paid by Elyza
    INSERT INTO tblRental (UserID, WalkInID, ReservationID, ItemID, RentalDate, ReturnedAt, UnitPrice, IsPaid)
    SELECT
        @UserElyza, NULL, NULL, @ItemA,
        CAST(d.[D] AS DATETIME) + CAST('18:10' AS DATETIME),
        CAST(d.[D] AS DATETIME) + CAST('19:10' AS DATETIME),
        0, 1
    FROM @SeedDates d
    WHERE NOT EXISTS (
        SELECT 1 FROM tblRental r WHERE r.ItemID=@ItemA AND CAST(r.RentalDate AS DATE)=d.[D]
    );

    -- Rental #2 unpaid by a queue walk-in
    INSERT INTO tblRental (UserID, WalkInID, ReservationID, ItemID, RentalDate, ReturnedAt, UnitPrice, IsPaid)
    SELECT
        NULL,
        (SELECT TOP 1 WalkInID FROM tblPlayerWalkIn w
         WHERE CAST(w.CreatedAt AS DATE)=d.[D] AND w.QueuePaid=1
         ORDER BY w.Lastname),
        NULL,
        @ItemB,
        CAST(d.[D] AS DATETIME) + CAST('19:15' AS DATETIME),
        CAST(d.[D] AS DATETIME) + CAST('20:00' AS DATETIME),
        0, 0
    FROM @SeedDates d
    WHERE NOT EXISTS (
        SELECT 1 FROM tblRental r WHERE r.ItemID=@ItemB AND CAST(r.RentalDate AS DATE)=d.[D]
    );


    /* =========================================================
       14) PAYMENTS
          -- PaymentTypeName now (string)
       ========================================================= */
    -- Walk-in payments
    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ReservationID, RentalID, PaymentDate, Amount)
    SELECT
        'WalkIn',
        NULL,
        w.WalkInID,
        NULL,
        NULL,
        CAST(w.CreatedAt AS DATE),
        CASE WHEN w.QueuePaid=1 THEN 180 ELSE 100 END
    FROM tblPlayerWalkIn w
    WHERE CAST(w.CreatedAt AS DATE) IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (SELECT 1 FROM tblPayment p WHERE p.WalkInID=w.WalkInID);

    -- Reservation payments (only IsPaid=1)
    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ReservationID, RentalID, PaymentDate, Amount)
    SELECT
        'Reservation',
        r.UserID,
        NULL,
        r.ReservationID,
        NULL,
        r.ResDate,
        r.RequiredAmount
    FROM tblReservation r
    WHERE r.IsPaid=1
      AND r.ResDate IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (SELECT 1 FROM tblPayment p WHERE p.ReservationID=r.ReservationID);

    -- Rental payments (only IsPaid=1)
    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ReservationID, RentalID, PaymentDate, Amount)
    SELECT
        'Rental',
        r.UserID,
        r.WalkInID,
        NULL,
        r.RentalID,
        CAST(r.RentalDate AS DATE),
        CAST(r.UnitPrice AS INT)
    FROM tblRental r
    WHERE r.IsPaid=1
      AND CAST(r.RentalDate AS DATE) IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (SELECT 1 FROM tblPayment p WHERE p.RentalID=r.RentalID);

    -- PAYC payments (100)
    INSERT INTO tblPayment (PaymentTypeName, UserID, WalkInID, ReservationID, RentalID, PaymentDate, Amount)
    SELECT
        'PAYC',
        NULL,
        p.WalkInID,
        NULL,
        NULL,
        CAST(p.CheckInTime AS DATE),
        100
    FROM tblPlayAllYouCanRegistry p
    WHERE CAST(p.CheckInTime AS DATE) IN (SELECT [D] FROM @SeedDates)
      AND NOT EXISTS (
          SELECT 1 FROM tblPayment pay
          WHERE pay.PaymentTypeName='PAYC'
            AND pay.WalkInID=p.WalkInID
            AND pay.PaymentDate=CAST(p.CheckInTime AS DATE)
      );

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;

    DECLARE @Err NVARCHAR(4000)=ERROR_MESSAGE();
    DECLARE @Line INT=ERROR_LINE();
    DECLARE @Num INT=ERROR_NUMBER();

    RAISERROR('SEED FAILED (%d) at line %d: %s', 16, 1, @Num, @Line, @Err);
END CATCH;
GO