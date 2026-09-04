/* =================================================================
   RaceDay Event Management System - Database Creation Script
   PROG6212 - Programming 2B | PoE Part 1 - Section C

   Target: Microsoft SQL Server, run in SQL Server Management Studio.
   This script creates the full RaceDay schema exactly as designed in
   RaceDay_ERD.png and seeds it with realistic sample data.

   Run the entire script top to bottom on a clean SQL Server instance.
   It is safe to re-run: existing RaceDay objects are dropped first.
   ================================================================= */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

-- Drop tables in reverse dependency order so the script can be re-run safely
IF OBJECT_ID('dbo.Results', 'U')      IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U')   IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U')   IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U')       IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Participants', 'U') IS NOT NULL DROP TABLE dbo.Participants;
IF OBJECT_ID('dbo.Organisers', 'U')   IS NOT NULL DROP TABLE dbo.Organisers;
GO

/* -----------------------------------------------------------------
   Organisers
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Organisers (
    OrganiserID      INT IDENTITY(1,1) NOT NULL,
    FullName         NVARCHAR(100)     NOT NULL,
    Email            NVARCHAR(150)     NOT NULL,
    PasswordHash     NVARCHAR(255)     NOT NULL,
    PhoneNumber      NVARCHAR(20)      NULL,
    OrganisationName NVARCHAR(150)     NULL,
    CreatedAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_Organisers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Organisers PRIMARY KEY (OrganiserID),
    CONSTRAINT UQ_Organisers_Email UNIQUE (Email)
);
GO

/* -----------------------------------------------------------------
   Participants
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Participants (
    ParticipantID          INT IDENTITY(1,1) NOT NULL,
    FullName               NVARCHAR(100)     NOT NULL,
    Email                  NVARCHAR(150)     NOT NULL,
    PasswordHash           NVARCHAR(255)     NOT NULL,
    PhoneNumber            NVARCHAR(20)      NULL,
    DateOfBirth            DATE              NOT NULL,
    EmergencyContactName   NVARCHAR(100)     NULL,
    EmergencyContactPhone  NVARCHAR(20)      NULL,
    CreatedAt              DATETIME2(0)      NOT NULL CONSTRAINT DF_Participants_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Participants PRIMARY KEY (ParticipantID),
    CONSTRAINT UQ_Participants_Email UNIQUE (Email)
);
GO

/* -----------------------------------------------------------------
   Events - every event belongs to exactly one Organiser
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Events (
    EventID      INT IDENTITY(1,1) NOT NULL,
    OrganiserID  INT               NOT NULL,
    EventName    NVARCHAR(150)     NOT NULL,
    Description  NVARCHAR(1000)    NULL,
    EventDate    DATE              NOT NULL,
    Location     NVARCHAR(150)     NOT NULL,
    Distance     DECIMAL(6,2)      NOT NULL,
    EventType    NVARCHAR(20)      NOT NULL,
    CreatedAt    DATETIME2(0)      NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Events PRIMARY KEY (EventID),
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID) REFERENCES dbo.Organisers (OrganiserID),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_Events_Distance CHECK (Distance > 0)
);
GO

/* -----------------------------------------------------------------
   Categories - every category belongs to exactly one Event
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Categories (
    CategoryID   INT IDENTITY(1,1) NOT NULL,
    EventID      INT               NOT NULL,
    CategoryName NVARCHAR(100)     NOT NULL,
    MinAge       INT               NOT NULL CONSTRAINT DF_Categories_MinAge DEFAULT (0),
    MaxAge       INT               NOT NULL CONSTRAINT DF_Categories_MaxAge DEFAULT (99),
    DistanceKM   DECIMAL(6,2)      NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryID),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES dbo.Events (EventID) ON DELETE CASCADE,
    CONSTRAINT CK_Categories_AgeRange CHECK (MinAge <= MaxAge)
);
GO

/* -----------------------------------------------------------------
   Enrolments - the associative entity resolving the many-to-many
   relationship between Participants and Events. Also records which
   Category the Participant selected for that Event.
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Enrolments (
    EnrolmentID    INT IDENTITY(1,1) NOT NULL,
    ParticipantID  INT               NOT NULL,
    EventID        INT               NOT NULL,
    CategoryID     INT               NOT NULL,
    EnrolmentDate  DATETIME2(0)      NOT NULL CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSUTCDATETIME(),
    Status         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Enrolments_Status DEFAULT ('Pending'),
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentID),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID) REFERENCES dbo.Participants (ParticipantID),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventID) REFERENCES dbo.Events (EventID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories (CategoryID),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    -- A Participant may only enrol once per Event (their category choice belongs to that one enrolment)
    CONSTRAINT UQ_Enrolments_ParticipantEvent UNIQUE (ParticipantID, EventID)
);
GO

/* -----------------------------------------------------------------
   Results - each Enrolment produces at most one Result (1 : 0..1),
   captured by the Organiser who recorded it.
   ----------------------------------------------------------------- */
CREATE TABLE dbo.Results (
    ResultID              INT IDENTITY(1,1) NOT NULL,
    EnrolmentID           INT               NOT NULL,
    FinishTimeSeconds     INT               NOT NULL,
    FinishPosition        INT               NOT NULL,
    RecordedByOrganiserID INT               NOT NULL,
    RecordedAt            DATETIME2(0)      NOT NULL CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Results PRIMARY KEY (ResultID),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments (EnrolmentID),
    CONSTRAINT FK_Results_Organisers FOREIGN KEY (RecordedByOrganiserID) REFERENCES dbo.Organisers (OrganiserID),
    -- Enforces the 1 : 0..1 cardinality: at most one Result per Enrolment
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentID),
    CONSTRAINT CK_Results_Positive CHECK (FinishTimeSeconds > 0 AND FinishPosition > 0)
);
GO

/* =================================================================
   Sample data
   NOTE: PasswordHash values below are illustrative placeholders only
   (representing where a real bcrypt/PBKDF2 hash produced by the
   Part 2 API would be stored) - they are not real password hashes.
   ================================================================= */

-- 2 Organisers
INSERT INTO dbo.Organisers (FullName, Email, PasswordHash, PhoneNumber, OrganisationName)
VALUES
    ('Thandiwe Mokoena',    'thandiwe.mokoena@raceday.co.za',    'PLACEHOLDER_HASH_ORG_0001', '0712345678', 'Johannesburg Road Running Club'),
    ('Pieter van der Merwe','pieter.vandermerwe@raceday.co.za',  'PLACEHOLDER_HASH_ORG_0002', '0823456789', 'Cape Peninsula Multisport');
GO

-- 2 Participants
INSERT INTO dbo.Participants (FullName, Email, PasswordHash, PhoneNumber, DateOfBirth, EmergencyContactName, EmergencyContactPhone)
VALUES
    ('Lindiwe Dlamini', 'lindiwe.dlamini@example.com', 'PLACEHOLDER_HASH_PAR_0001', '0734567890', '1996-03-14', 'Nomvula Dlamini', '0834567890'),
    ('Sipho Ndlovu',    'sipho.ndlovu@example.com',    'PLACEHOLDER_HASH_PAR_0002', '0845678901', '1989-11-02', 'Zanele Ndlovu',   '0856789012');
GO

-- 3 Events (OrganiserID 1 = Thandiwe / JRRC, OrganiserID 2 = Pieter / Cape Peninsula Multisport)
INSERT INTO dbo.Events (OrganiserID, EventName, Description, EventDate, Location, Distance, EventType)
VALUES
    (1, 'Joburg City 10K Challenge', 'A flat, fast 10km road race through the Johannesburg CBD, open to runners of all levels.', '2026-11-08', 'Johannesburg, Gauteng',   10.00,  'Run'),
    (2, 'Peninsula Cycle Classic',   'A scenic road cycling event along the Cape Peninsula coastline, with elite and amateur distances.', '2026-10-18', 'Cape Town, Western Cape', 109.00, 'Cycle'),
    (1, 'Soweto Heritage Fun Walk',  'A family-friendly community walk celebrating Soweto''s heritage, open to walkers of every age.', '2026-09-27', 'Soweto, Gauteng',        5.00,   'Walk');
GO

-- Categories for each Event (EventID 1 = Joburg 10K, 2 = Peninsula Cycle, 3 = Soweto Walk)
INSERT INTO dbo.Categories (EventID, CategoryName, MinAge, MaxAge, DistanceKM)
VALUES
    (1, 'Open 10K',        18, 99, 10.00),
    (1, 'Junior 5K',       12, 17, 5.00),
    (2, 'Elite 109KM',     19, 99, 109.00),
    (2, 'Amateur 55KM',    16, 99, 55.00),
    (3, 'Family 5K Walk',  0,  99, 5.00);
GO

-- Sample Enrolments (ParticipantID 1 = Lindiwe, 2 = Sipho)
INSERT INTO dbo.Enrolments (ParticipantID, EventID, CategoryID, Status)
VALUES
    (1, 1, 1, 'Confirmed'), -- Lindiwe -> Joburg City 10K Challenge -> Open 10K
    (2, 1, 1, 'Confirmed'), -- Sipho   -> Joburg City 10K Challenge -> Open 10K
    (1, 2, 4, 'Pending'),   -- Lindiwe -> Peninsula Cycle Classic  -> Amateur 55KM
    (2, 3, 5, 'Confirmed'); -- Sipho   -> Soweto Heritage Fun Walk -> Family 5K Walk
GO

-- Sample Results (only for enrolments in the already-completed Joburg City 10K Challenge)
INSERT INTO dbo.Results (EnrolmentID, FinishTimeSeconds, FinishPosition, RecordedByOrganiserID)
VALUES
    (1, 2715, 12, 1), -- Lindiwe finished the Joburg City 10K in 45:15, 12th place
    (2, 2890, 18, 1); -- Sipho finished the Joburg City 10K in 48:10, 18th place
GO

/* -----------------------------------------------------------------
   Verification queries (optional - run manually to inspect the data)
   ----------------------------------------------------------------- */
-- SELECT * FROM dbo.Organisers;
-- SELECT * FROM dbo.Participants;
-- SELECT * FROM dbo.Events;
-- SELECT * FROM dbo.Categories;
-- SELECT * FROM dbo.Enrolments;
-- SELECT * FROM dbo.Results;

-- Confirms every enrolment resolves to a Participant, Event and Category correctly
-- SELECT p.FullName, e.EventName, c.CategoryName, en.Status
-- FROM dbo.Enrolments en
-- JOIN dbo.Participants p ON p.ParticipantID = en.ParticipantID
-- JOIN dbo.Events e       ON e.EventID = en.EventID
-- JOIN dbo.Categories c   ON c.CategoryID = en.CategoryID;

