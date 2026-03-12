USE dbsmashitFINAL;
GO
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    /* =========================================================
       0) Seed dates
       ========================================================= */
    DECLARE @SeedDates TABLE ([D] DATE PRIMARY KEY);
    INSERT INTO @SeedDates ([D])
    VALUES  ('2026-03-10'), ('2026-03-11'), ('2026-03-12'), ('2026-03-13'), ('2026-03-14'), ('2026-03-15'), ('2026-03-16'), ('2026-03-17'), ('2026-03-18'), ('2026-03-19'), ('2026-03-20');

    /* =========================================================
       1) STAFF + PLAYERS
       ========================================================= */
    INSERT INTO tblStaffAccount (Lastname, Firstname, Email, Username, [Password], RoleName)
    VALUES 
    ('Olayvar', 'Hannali', 'hannaliolayvar@gmail.com', 'admin', 'pass', 'admin'),
    ('Noda', 'Isaiah', 'isaiahandreinoda@gmail.com', 'receptionist', 'pass', 'receptionist');

    DECLARE @AdminStaffID INT = (SELECT StaffID FROM tblStaffAccount WHERE Username='admin');

    INSERT INTO tblPlayerAccount (Lastname, Firstname, Email, PhoneNumber, Username, [Password])
    VALUES 
    ('Olayvar', 'Elyza', 'olayvarelyzarosa@gmail.com', '09171234567', 'elyza', 'pass'),
    ('Olayvar', 'Elexali', 'elexaliolayvar9711@gmail.com', '09179876543', 'elexali', 'pass');

    DECLARE @UserElyza INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elyza');
    DECLARE @UserElexali INT = (SELECT UserID FROM tblPlayerAccount WHERE Username='elexali');

    /* =========================================================
       2) COURTS
       ========================================================= */
    INSERT INTO tblCourt(CourtNumber, SportName, IsActive) 
    VALUES 
    (1, 'badminton', 1), (2, 'badminton', 1), (3, 'badminton', 1),
    (4, 'badminton', 1), (5, 'pickleball', 1), (6, 'pickleball', 1);

    DECLARE @Court1 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=1);
    DECLARE @Court2 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=2);
    DECLARE @Court3 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=3);
    DECLARE @Court5 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=5);
    DECLARE @Court6 INT = (SELECT CourtID FROM tblCourt WHERE CourtNumber=6);

    /* =========================================================
       3) EQUIPMENT MODELS + ITEMS
       ========================================================= */
    -- Note: Added ItemCategory to satisfy CHECK constraint
    INSERT INTO tblEquipmentModel(EquipmentType, EquipmentSpec, DefaultRentalPrice, ItemCategory)
    VALUES 
    ('Badminton Racket', 'JXXS-10', 100.00, 'Rental'),
    ('Badminton Racket', 'JXXS-11', 100.00, 'Rental'),
    ('ShuttleCock', 'AULA', 50.00, 'Consumable'),
    ('Pickleball Paddle', 'JOOLA FXS-26', 100.00, 'Rental');

    DECLARE @M_J10 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JXXS-10');
    DECLARE @M_J11 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JXXS-11');
    DECLARE @M_AULA INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='AULA');
    DECLARE @M_F26 INT = (SELECT ModelID FROM tblEquipmentModel WHERE EquipmentSpec='JOOLA FXS-26');

    INSERT INTO tblEquipmentItem(ModelID) 
    VALUES (@M_J10), (@M_J10), (@M_J11), (@M_J11), (@M_F26), (@M_F26);

    DECLARE @ItemA INT = (SELECT MIN(ItemID) FROM tblEquipmentItem);
    DECLARE @ItemB INT = (SELECT MAX(ItemID) FROM tblEquipmentItem);

    /* =========================================================
       4) EVENTS & COURT POOL
       ========================================================= */
    INSERT INTO tblEvent (Title, SportName, EventDate, CreatedByStaffID, IsActive)
    SELECT 
        CONCAT('Tournament - ', FORMAT(d.[D], 'MMMM dd')),
        'badminton', d.[D], @AdminStaffID, 1
    FROM @SeedDates d;

    INSERT INTO tblEventCourtPool (EventID, CourtID)
    SELECT e.EventID, c.CourtID
    FROM tblEvent e
    CROSS JOIN tblCourt c
    WHERE c.CourtNumber IN (2, 3, 6);

    /* =========================================================
       5) WALK-INS
       ========================================================= */
    INSERT INTO tblPlayerWalkIn (UserID, Lastname, Firstname, BasePaid, QueuePaid, CreatedAt)
    VALUES 
    (NULL, 'Reyes', 'Juan', 1, 1, '2026-02-27 08:30'),
    (NULL, 'Santos', 'Maria', 1, 1, '2026-02-27 08:45'),
    (NULL, 'Dela Cruz', 'Paolo', 1, 0, '2026-02-28 09:00'),
    (NULL, 'Garcia', 'Anne', 1, 1, '2026-03-01 10:00');

    /* =========================================================
       6) COURT AVAILABILITY (Slot Generation)
       ========================================================= */
    DECLARE @Slots TABLE(StartTime TIME(0), EndTime TIME(0));
    DECLARE @t TIME(0) = '08:00';
    WHILE (@t < '22:00')
    BEGIN
        INSERT INTO @Slots VALUES (@t, CAST(DATEADD(MINUTE, 30, @t) AS TIME(0)));
        SET @t = CAST(DATEADD(MINUTE, 30, @t) AS TIME(0));
    END;

    INSERT INTO tblCourtAvailability (CourtID, [Date], StartTime, EndTime, ModeName, CreatedByStaffID)
    SELECT c.CourtID, d.[D], s.StartTime, s.EndTime,
        CASE 
            WHEN c.CourtNumber IN (2,3,6) THEN 'Queue'
            ELSE 'Reservation'
        END, @AdminStaffID
    FROM tblCourt c
    CROSS JOIN @SeedDates d
    CROSS JOIN @Slots s;

    /* =========================================================
       7) RESERVATIONS
       ========================================================= */
    INSERT INTO tblReservation (UserID, PlayerNumber, CourtID, ResDate, StartTime, EndTime, IsPaid, SportName, RequiredAmount, ApprovedByStaffID, ReservationStatusName)
    VALUES 
    (@UserElyza, 4, @Court1, '2026-02-27', '18:00', '19:00', 1, 'badminton', 400.00, @AdminStaffID, 'Approved'),
    (@UserElexali, 2, @Court5, '2026-02-27', '19:00', '20:00', 0, 'pickleball', 630.00, NULL, 'Pending');

    /* =========================================================
       8) QUEUE & SESSIONS
       ========================================================= */
    DECLARE @ResID INT = (SELECT TOP 1 ReservationID FROM tblReservation WHERE UserID=@UserElyza);

    INSERT INTO tblCourtQueue (CourtID, ReservationID, QueueDate, QueueTypeName, QueueNumber, StatusName, LastModifiedByStaffID)
    VALUES (@Court1, @ResID, '2026-02-27', 'Reservation', 1, 'Waiting', @AdminStaffID);

    INSERT INTO tblActiveSession (CourtID, ReservationID, StartTime, ExpectedEndTime, StatusName)
    VALUES (@Court1, @ResID, '2026-02-27 18:00', '2026-02-27 19:00', 'Active');

    /* =========================================================
       9) PAYMENTS
       ========================================================= */
    INSERT INTO tblPayment (PaymentTypeName, UserID, ReservationID, PaymentDate, Amount)
    VALUES ('Reservation', @UserElyza, @ResID, '2026-02-27', 400.00);

    /* =========================================================
       10) ABOUT US & CONTENT
       ========================================================= */
    INSERT INTO tblAboutUsMembers(Lastname, Firstname, Position)
    VALUES ('De Mesa', 'Richie', 'CEO Operations Manager'),
           ('Unira', 'Mark Odrey', 'Backend Developer');

    COMMIT TRAN;
    PRINT 'Seed successful!';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @Err NVARCHAR(4000)=ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
GO