CREATE DATABASE dbsmashitFINAL;
GO
USE dbsmashitFINAL;
GO

/* =========================
   ACCOUNTS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblStaffAccount (
    StaffID INT IDENTITY PRIMARY KEY,
    Firstname VARCHAR(100) NOT NULL,
     Lastname VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    Username VARCHAR(50) NOT NULL UNIQUE,
    [Password] VARCHAR(255) NOT NULL,

    RoleName VARCHAR(20) NOT NULL, -- admin, receptionist
    ImgPath VARCHAR(MAX) NOT NULL DEFAULT 'uploads/avatars/person.jpg',

    CONSTRAINT CHK_StaffAccount_RoleName CHECK (RoleName IN ('admin','receptionist'))
);
GO

CREATE TABLE tblPlayerAccount (
    UserID INT IDENTITY PRIMARY KEY,
    Firstname VARCHAR(100) NOT NULL,
     Lastname VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NULL UNIQUE,
    PhoneNumber VARCHAR(30) NULL,
    Username VARCHAR(50) NULL UNIQUE,
    [Password] VARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ImgPath VARCHAR(MAX) NOT NULL DEFAULT 'uploads/avatars/person.jpg'
);
GO

CREATE TABLE tblPlayerWalkIn (
    WalkInID INT IDENTITY PRIMARY KEY,
    UserID INT NULL, -- optional link if later register
    Firstname VARCHAR(100) NOT NULL,
    Lastname VARCHAR(100) NOT NULL,

    BasePaid BIT DEFAULT 1,
    QueuePaid BIT DEFAULT 0,

    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID)
);
GO


/* =========================
   COURTS + AVAILABILITY (NO LOOKUPS)
   ========================= */

CREATE TABLE tblCourt (
    CourtID INT IDENTITY PRIMARY KEY,
    CourtNumber INT NOT NULL UNIQUE,

    SportName VARCHAR(20) NOT NULL, -- badminton, pickleball
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT CHK_Court_SportName CHECK (SportName IN ('badminton','pickleball'))
);
GO

CREATE TABLE tblCourtAvailability (
    AvailabilityID INT IDENTITY PRIMARY KEY,
    CourtID INT NOT NULL,

    [Date] DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,

    ModeName VARCHAR(20) NOT NULL, -- PlayForAll, Queue, Reservation, Closed
    CreatedByStaffID INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),
    FOREIGN KEY (CreatedByStaffID) REFERENCES tblStaffAccount(StaffID),

    CONSTRAINT UQ_CourtAvailability UNIQUE (CourtID, [Date], StartTime, EndTime),
    CONSTRAINT CHK_Availability_Time CHECK (StartTime < EndTime),
    CONSTRAINT CHK_Availability_Mode CHECK (ModeName IN ('PlayForAll','Queue','Reservation','Closed'))
);
GO


/* =========================
   RESERVATIONS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblReservation (
    ReservationID INT IDENTITY PRIMARY KEY,

    UserID INT NULL,
    WalkInID INT NULL,

    PlayerNumber INT NULL,
    CourtID INT NOT NULL,

    ResDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,

    IsPaid BIT NOT NULL DEFAULT 0,

    ReservationStatusName VARCHAR(30) NOT NULL DEFAULT 'Pending', -- Pending/Approved/Cancelled/Completed
    RequestStatus VARCHAR(40) NULL, -- ChangeRequest / CancelRequest etc.

    PaymongoCheckoutSessionID VARCHAR(64) NULL,
    PaymentStatus VARCHAR(30) NULL,

    RequiredAmount INT NOT NULL,

    CreatedAt DATETIME DEFAULT GETDATE(),
    ApprovedByStaffID INT NULL,

    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),
    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID),
    FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),
    FOREIGN KEY (ApprovedByStaffID) REFERENCES tblStaffAccount(StaffID),

    CONSTRAINT CHK_Reservation_Time CHECK (StartTime < EndTime),
    CONSTRAINT CHK_Reservation_Person CHECK (UserID IS NOT NULL OR WalkInID IS NOT NULL),
    CONSTRAINT CHK_Reservation_Status CHECK (ReservationStatusName IN ('Pending','Approved','Cancelled','Completed'))
);
GO


/* =========================
   EVENTS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblEvent (
    EventID INT IDENTITY PRIMARY KEY,
    Title VARCHAR(100) NOT NULL,

    SportName VARCHAR(20) NULL, -- optional: badminton/pickleball
    EventDate DATE NOT NULL,

    CreatedByStaffID INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,

    FOREIGN KEY (CreatedByStaffID) REFERENCES tblStaffAccount(StaffID),

    CONSTRAINT CHK_Event_SportName CHECK (SportName IS NULL OR SportName IN ('badminton','pickleball'))
);
GO

CREATE TABLE tblEventCourtPool (
    EventCourtID INT IDENTITY PRIMARY KEY,
    EventID INT NOT NULL,
    CourtID INT NOT NULL,

    FOREIGN KEY (EventID) REFERENCES tblEvent(EventID),
    FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),

    CONSTRAINT UQ_Event_Court UNIQUE (EventID, CourtID)
);
GO

CREATE TABLE tblEventParticipant (
    EventParticipantID INT IDENTITY PRIMARY KEY,
    EventID INT NOT NULL,

    UserID INT NULL,
    WalkInID INT NULL,

    OrderNo INT NOT NULL,

    StatusName VARCHAR(20) NOT NULL DEFAULT 'Waiting', -- Active/Completed/Cancelled/Waiting

    FOREIGN KEY (EventID) REFERENCES tblEvent(EventID),
    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID),
    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),

    CONSTRAINT CHK_EventParticipant_Player CHECK (
        (UserID IS NOT NULL AND WalkInID IS NULL) OR
        (UserID IS NULL AND WalkInID IS NOT NULL)
    ),
    CONSTRAINT UQ_EventParticipant_Order UNIQUE (EventID, OrderNo),
    CONSTRAINT CHK_EventParticipant_Status CHECK (StatusName IN ('Active','Completed','Cancelled','Waiting'))
);
GO


/* =========================
   QUEUE (NO LOOKUPS)
   ========================= */

CREATE TABLE tblCourtQueue (
    QueueID INT IDENTITY PRIMARY KEY,
    CourtID INT NOT NULL,

    UserID INT NULL,
    WalkInID INT NULL,
    ReservationID INT NULL,

    QueueDate DATE NOT NULL,
    EventID INT NULL,

    QueueTypeName VARCHAR(20) NOT NULL, -- Reservation/Queue/WalkIn
    QueueNumber INT NOT NULL,

    StatusName VARCHAR(20) NOT NULL DEFAULT 'Waiting',

    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    LastModifiedByStaffID INT NULL,

    FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),
    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID),
    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),
    FOREIGN KEY (ReservationID) REFERENCES tblReservation(ReservationID),
    FOREIGN KEY (EventID) REFERENCES tblEvent(EventID),
    FOREIGN KEY (LastModifiedByStaffID) REFERENCES tblStaffAccount(StaffID),

    CONSTRAINT CHK_CourtQueue_Type CHECK (QueueTypeName IN ('Reservation','Queue','WalkIn')),
    CONSTRAINT CHK_CourtQueue_Status CHECK (StatusName IN ('Active','Completed','Cancelled','Waiting'))
);
GO


/* =========================
   PLAY ALL YOU CAN (NO LOOKUPS)
   ========================= */

CREATE TABLE tblPlayAllYouCanRegistry (
    PAYCID INT IDENTITY PRIMARY KEY,
    WalkInID INT NOT NULL,

    SportName VARCHAR(20) NOT NULL,
    CheckInTime DATETIME NOT NULL DEFAULT GETDATE(),

    StatusName VARCHAR(20) NOT NULL DEFAULT 'Active',

    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),

    CONSTRAINT CHK_PAYC_Sport CHECK (SportName IN ('badminton','pickleball')),
    CONSTRAINT CHK_PAYC_Status CHECK (StatusName IN ('Active','Completed','Cancelled','Waiting'))
);
GO


/* =========================
   ACTIVE SESSIONS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblActiveSession (
    SessionID INT IDENTITY PRIMARY KEY,
    CourtID INT NOT NULL,

    ReservationID INT NULL,
    QueueID INT NULL,
    PAYCID INT NULL,

    StartTime DATETIME NOT NULL,
    ExpectedEndTime DATETIME NOT NULL,
    ActualEndTime DATETIME NULL,

    StatusName VARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),
    FOREIGN KEY (ReservationID) REFERENCES tblReservation(ReservationID),
    FOREIGN KEY (QueueID) REFERENCES tblCourtQueue(QueueID),
    FOREIGN KEY (PAYCID) REFERENCES tblPlayAllYouCanRegistry(PAYCID),

    CONSTRAINT CHK_ActiveSession_Status CHECK (StatusName IN ('Active','Completed','Cancelled','Waiting'))
);
GO


/* =========================
   EQUIPMENT + RENTAL (NO LOOKUPS)
   ========================= */

CREATE TABLE tblEquipmentModel (
    ModelID INT IDENTITY PRIMARY KEY,
    EquipmentSpec VARCHAR(100) NULL,
    EquipmentType VARCHAR(50) NOT NULL,
    DefaultRentalPrice DECIMAL(10,2) NOT NULL,

    CONSTRAINT UQ_EquipmentModel UNIQUE (EquipmentType, EquipmentSpec)
);
GO

CREATE TABLE tblEquipmentItem (
    ItemID INT IDENTITY PRIMARY KEY,
    ModelID INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    FOREIGN KEY (ModelID) REFERENCES tblEquipmentModel(ModelID)
);
GO

CREATE TABLE tblRental (
    RentalID INT IDENTITY PRIMARY KEY,

    UserID INT NULL,
    WalkInID INT NULL,
    ReservationID INT NULL,

    ItemID INT NOT NULL,

    RentalDate DATETIME NOT NULL DEFAULT GETDATE(),
    ReturnedAt DATETIME NULL,

    UnitPrice DECIMAL(10,2) NOT NULL DEFAULT 0,
    IsPaid BIT NOT NULL DEFAULT 0,

    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID),
    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),
    FOREIGN KEY (ReservationID) REFERENCES tblReservation(ReservationID),
    FOREIGN KEY (ItemID) REFERENCES tblEquipmentItem(ItemID)
);
GO

CREATE UNIQUE INDEX UX_ActiveRental_Item
ON tblRental(ItemID)
WHERE ReturnedAt IS NULL;
GO

CREATE TRIGGER trg_Rental_SetUnitPrice
ON tblRental
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET r.UnitPrice = m.DefaultRentalPrice
    FROM tblRental r
    JOIN inserted i ON i.RentalID = r.RentalID
    JOIN tblEquipmentItem ei ON ei.ItemID = r.ItemID
    JOIN tblEquipmentModel m ON m.ModelID = ei.ModelID
    WHERE r.UnitPrice = 0;
END;
GO


/* =========================
   PAYMENTS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblPayment (
    PaymentID INT IDENTITY PRIMARY KEY,

    PaymentTypeName VARCHAR(20) NULL, -- Reservation, Queue, WalkIn, Rental, PAYC

    UserID INT NULL,
    WalkInID INT NULL,
    ReservationID INT NULL,
    RentalID INT NULL,

    PaymentDate DATE NULL,
    Amount INT NOT NULL DEFAULT(0),

    FOREIGN KEY (UserID) REFERENCES tblPlayerAccount(UserID),
    FOREIGN KEY (WalkInID) REFERENCES tblPlayerWalkIn(WalkInID),
    FOREIGN KEY (ReservationID) REFERENCES tblReservation(ReservationID),
    FOREIGN KEY (RentalID) REFERENCES tblRental(RentalID),

    CONSTRAINT CHK_Payment_Type CHECK (
        PaymentTypeName IS NULL OR PaymentTypeName IN ('Reservation','Queue','WalkIn','Rental','PAYC')
    )
);
GO


/* =========================
   CONTENT / CMS (NO LOOKUPS)
   ========================= */

CREATE TABLE tblAnnouncement (
    AnnouncementID INT IDENTITY PRIMARY KEY,
    Title VARCHAR(150) NOT NULL,
    Content VARCHAR(MAX) NOT NULL,
    FilePath VARCHAR(MAX) NULL,
    StartDate DATETIME NOT NULL DEFAULT GETDATE(),
    EndDate DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ViewStatus BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NULL,
    CreatedByStaffID INT NOT NULL,

    FOREIGN KEY (CreatedByStaffID) REFERENCES tblStaffAccount(StaffID)
);
GO

CREATE TABLE tblAboutUsMembers (
    MemberID INT IDENTITY PRIMARY KEY,
    Firstname VARCHAR(150) NOT NULL,
    Lastname VARCHAR(150) NOT NULL,
    Position VARCHAR(100) NULL,
    ImgPath VARCHAR(MAX) NOT NULL DEFAULT 'uploads/avatars/person.jpg'
);
GO

CREATE TABLE tblContent (
    ContentID INT IDENTITY PRIMARY KEY,
    Section VARCHAR(150) NOT NULL,
    Title VARCHAR(100) NULL,
    Subtitle VARCHAR(MAX) NULL,
    Content VARCHAR(MAX) NULL,
    ImgPath VARCHAR(MAX) NULL,
    LastModifiedByStaffID INT NOT NULL,

    FOREIGN KEY (LastModifiedByStaffID) REFERENCES tblStaffAccount(StaffID)
);
GO