-- Create database first
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'dbSmashIT')
BEGIN
    CREATE DATABASE dbSmashIT;
END
GO

USE dbSmashIT;
GO

-- Table: tblPlayer
CREATE TABLE tblPlayer (
    PlayerID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100),
    Email NVARCHAR(150),
    PhoneNumber NVARCHAR(30),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Table: tblStaff
CREATE TABLE tblStaff (
    StaffID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100),
    Email NVARCHAR(150),
    PhoneNumber NVARCHAR(30),
    Username NVARCHAR(50),
    Password NVARCHAR(50)
);
GO

-- Table: tblCourt
CREATE TABLE tblCourt (
    CourtID INT IDENTITY(1,1) PRIMARY KEY,
    Sports NVARCHAR(50),
    Mode NVARCHAR(50),
    Status NVARCHAR(50),
    QueueID INT NULL
);
GO

-- Table: tblQueuesSession
CREATE TABLE tblQueuesSession (
    SessionID INT IDENTITY(1,1) PRIMARY KEY,
    CourtID INT NOT NULL,
    StartedAt DATETIME NULL,
    EndedAt DATETIME NULL,
    IsActive BIT DEFAULT 1,

    CONSTRAINT FK_QueuesSession_Court FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID)
);
GO

-- Table: tblQueuePrio
CREATE TABLE tblQueuePrio (
    PriorityQueueID INT IDENTITY(1,1) PRIMARY KEY,
    PlayerID INT NOT NULL,
    AssignedCourtID INT NOT NULL,
    QueuePosition INT NULL,
    Status NVARCHAR(50) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_QueuePrio_Player FOREIGN KEY (PlayerID) REFERENCES tblPlayer(PlayerID),
    CONSTRAINT FK_QueuePrio_Court FOREIGN KEY (AssignedCourtID) REFERENCES tblCourt(CourtID)
);
GO

-- Table: tblQueueAssignment
CREATE TABLE tblQueueAssignment (
    IDAssignmentID INT IDENTITY(1,1) PRIMARY KEY,
    PriorityQueueID INT NOT NULL,
    CourtID INT NOT NULL,
    SessionID INT NOT NULL,
    AssignedAt DATETIME NULL,
    CompletedAt DATETIME NULL,

    CONSTRAINT FK_QueueAssignment_QueuePrio FOREIGN KEY (PriorityQueueID) REFERENCES tblQueuePrio(PriorityQueueID),
    CONSTRAINT FK_QueueAssignment_Court FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID),
    CONSTRAINT FK_QueueAssignment_Session FOREIGN KEY (SessionID) REFERENCES tblQueuesSession(SessionID)
);
GO

-- Table: tblReservation
CREATE TABLE tblReservation (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    PlayerID INT NOT NULL,
    CourtID INT NOT NULL,
    ResDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Status NVARCHAR(50) NULL,
    TimeStamp DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Reservation_Player FOREIGN KEY (PlayerID) REFERENCES tblPlayer(PlayerID),
    CONSTRAINT FK_Reservation_Court FOREIGN KEY (CourtID) REFERENCES tblCourt(CourtID)
);
GO

-- Table: tblPayment
CREATE TABLE tblPayment (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    ReservationID INT NOT NULL,
    PlayerID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,

    ProofImage VARBINARY(MAX) NULL,
    ProofImageFileName NVARCHAR(255) NULL,
    ProofImageFileType NVARCHAR(100) NULL,

    PaymentStatus NVARCHAR(50) NULL,
    PaymentDate DATETIME NULL,

    CONSTRAINT FK_Payment_Reservation FOREIGN KEY (ReservationID) REFERENCES tblReservation(ReservationID),
    CONSTRAINT FK_Payment_Player FOREIGN KEY (PlayerID) REFERENCES tblPlayer(PlayerID)
);
GO

-- Table: tblWalkIn
CREATE TABLE tblWalkIn (
    WalkInID INT IDENTITY(1,1) PRIMARY KEY,
    PlayerID INT NOT NULL,
    BasePaid DECIMAL(10,2) NULL,
    QueuePaid DECIMAL(10,2) NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_WalkIn_Player FOREIGN KEY (PlayerID) REFERENCES tblPlayer(PlayerID)
);
GO

-- Insert initial staff
INSERT INTO tblStaff (FullName, PhoneNumber, Username, Password)
VALUES ('Hannali Olayvar', '09398409243', 'user', 'user');
INSERT INTO tblStaff (FullName, PhoneNumber, Username, Password)
VALUES ('Isaiah Noda', '09157726116', 'admin', 'admin');
GO
