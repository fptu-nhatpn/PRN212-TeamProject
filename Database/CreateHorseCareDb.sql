IF DB_ID(N'HorseCareDb') IS NULL
BEGIN
    CREATE DATABASE HorseCareDb;
END;
GO

USE HorseCareDb;
GO

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUsers
    (
        UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY,
        DisplayName  NVARCHAR(100) NOT NULL,
        UserName     NVARCHAR(120) NOT NULL,
        Role         NVARCHAR(20) NOT NULL,
        CONSTRAINT UQ_AppUsers_UserName UNIQUE (UserName),
        CONSTRAINT CK_AppUsers_Role CHECK (Role IN (N'Admin', N'Owner', N'Trainer', N'Doctor'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Horses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Horses
    (
        HorseId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Horses PRIMARY KEY,
        Name          NVARCHAR(100) NOT NULL,
        Breed         NVARCHAR(80) NULL,
        Sex           NVARCHAR(20) NOT NULL,
        DateOfBirth   DATE NULL,
        OwnerUserId   INT NULL,
        Notes         NVARCHAR(500) NULL,
        CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_Horses_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Horses_AppUsers FOREIGN KEY (OwnerUserId) REFERENCES dbo.AppUsers(UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.TrainingSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrainingSessions
    (
        TrainingSessionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TrainingSessions PRIMARY KEY,
        HorseId           INT NOT NULL,
        TrainerUserId     INT NOT NULL,
        ScheduledAt       DATETIME2 NOT NULL,
        Activity          NVARCHAR(200) NOT NULL,
        DurationMinutes   INT NOT NULL,
        Status            NVARCHAR(40) NOT NULL CONSTRAINT DF_TrainingSessions_Status DEFAULT (NCHAR(272)+NCHAR(227)+N' l'+NCHAR(234)+N'n l'+NCHAR(7883)+N'ch'),
        Notes             NVARCHAR(500) NULL,
        CreatedAt         DATETIME2 NOT NULL CONSTRAINT DF_TrainingSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TrainingSessions_Duration CHECK (DurationMinutes > 0),
        CONSTRAINT FK_TrainingSessions_Horses FOREIGN KEY (HorseId) REFERENCES dbo.Horses(HorseId),
        CONSTRAINT FK_TrainingSessions_AppUsers FOREIGN KEY (TrainerUserId) REFERENCES dbo.AppUsers(UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.MedicalRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicalRecords
    (
        MedicalRecordId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MedicalRecords PRIMARY KEY,
        HorseId         INT NOT NULL,
        DoctorUserId    INT NOT NULL,
        VisitDate       DATE NOT NULL,
        Diagnosis       NVARCHAR(500) NOT NULL,
        Treatment       NVARCHAR(1000) NULL,
        Status          NVARCHAR(40) NOT NULL CONSTRAINT DF_MedicalRecords_Status DEFAULT (NCHAR(272)+N'ang theo d'+NCHAR(245)+N'i'),
        CreatedAt       DATETIME2 NOT NULL CONSTRAINT DF_MedicalRecords_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MedicalRecords_Horses FOREIGN KEY (HorseId) REFERENCES dbo.Horses(HorseId),
        CONSTRAINT FK_MedicalRecords_AppUsers FOREIGN KEY (DoctorUserId) REFERENCES dbo.AppUsers(UserId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE UserName = N'admin@horsecare.local')
    INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
    VALUES (N'Qu'+NCHAR(7843)+N'n tr'+NCHAR(7883)+N' vi'+NCHAR(234)+N'n', N'admin@horsecare.local', N'Admin');

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE UserName = N'owner@horsecare.local')
    INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
    VALUES (N'Nguy'+NCHAR(7877)+N'n Minh Anh', N'owner@horsecare.local', N'Owner');

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE UserName = N'trainer@horsecare.local')
    INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
    VALUES (N'Tr'+NCHAR(7847)+N'n Ho'+NCHAR(224)+N'ng Nam', N'trainer@horsecare.local', N'Trainer');

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE UserName = N'doctor@horsecare.local')
    INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
    VALUES (N'B'+NCHAR(225)+N'c s'+NCHAR(297)+N' L'+NCHAR(234)+N' Thu H'+NCHAR(224), N'doctor@horsecare.local', N'Doctor');
GO

UPDATE dbo.AppUsers
SET DisplayName = CASE UserName
    WHEN N'admin@horsecare.local' THEN N'Qu'+NCHAR(7843)+N'n tr'+NCHAR(7883)+N' vi'+NCHAR(234)+N'n'
    WHEN N'owner@horsecare.local' THEN N'Nguy'+NCHAR(7877)+N'n Minh Anh'
    WHEN N'trainer@horsecare.local' THEN N'Tr'+NCHAR(7847)+N'n Ho'+NCHAR(224)+N'ng Nam'
    WHEN N'doctor@horsecare.local' THEN N'B'+NCHAR(225)+N'c s'+NCHAR(297)+N' L'+NCHAR(234)+N' Thu H'+NCHAR(224)
END
WHERE UserName IN
    (N'admin@horsecare.local', N'owner@horsecare.local',
     N'trainer@horsecare.local', N'doctor@horsecare.local');
GO

INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
SELECT sampleOwner.DisplayName, sampleOwner.UserName, N'Owner'
FROM (VALUES
    (N'L'+NCHAR(234)+N' Thu'+NCHAR(7923)+N' D'+NCHAR(432)+NCHAR(417)+N'ng', N'owner.thuy@horsecare.local'),
    (N'Ph'+NCHAR(7841)+N'm Qu'+NCHAR(7889)+N'c B'+NCHAR(7843)+N'o', N'owner.quang@horsecare.local'),
    (NCHAR(272)+NCHAR(7863)+N'ng Mai Hoa', N'owner.hoa@horsecare.local'),
    (N'V'+NCHAR(245)+N' Th'+NCHAR(224)+N'nh Nam', N'owner.nam@horsecare.local'),
    (N'Tr'+NCHAR(7847)+N'n Ng'+NCHAR(7885)+N'c Linh', N'owner.linh@horsecare.local')
) AS sampleOwner(DisplayName, UserName)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.AppUsers AS existingOwner
    WHERE existingOwner.UserName = sampleOwner.UserName
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Horses WHERE Name = N'Sao Mai')
BEGIN
    INSERT INTO dbo.Horses (Name, Breed, Sex, DateOfBirth, OwnerUserId, Notes)
    SELECT N'Sao Mai', N'Ng'+NCHAR(7921)+N'a '+NCHAR(273)+N'ua',
           N'C'+NCHAR(225)+N'i', '2022-04-15', UserId,
           N'Ng'+NCHAR(7921)+N'a m'+NCHAR(7851)+N'u'
    FROM dbo.AppUsers
    WHERE UserName = N'owner@horsecare.local';
END;

UPDATE dbo.Horses
SET Name = N'B'+NCHAR(227)+N'o '+NCHAR(272)+NCHAR(234)+N'm',
    Sex = NCHAR(272)+NCHAR(7921)+N'c',
    Notes = N'Ng'+NCHAR(7921)+N'a m'+NCHAR(7851)+N'u'
WHERE Breed = N'Thoroughbred' AND DateOfBirth = '2021-08-03';

IF NOT EXISTS (SELECT 1 FROM dbo.Horses WHERE Name = N'B'+NCHAR(227)+N'o '+NCHAR(272)+NCHAR(234)+N'm')
BEGIN
    INSERT INTO dbo.Horses (Name, Breed, Sex, DateOfBirth, OwnerUserId, Notes)
    SELECT N'B'+NCHAR(227)+N'o '+NCHAR(272)+NCHAR(234)+N'm',
           N'Thoroughbred', NCHAR(272)+NCHAR(7921)+N'c', '2021-08-03', UserId,
           N'Ng'+NCHAR(7921)+N'a m'+NCHAR(7851)+N'u'
    FROM dbo.AppUsers
    WHERE UserName = N'owner@horsecare.local';
END;

DELETE duplicateHorse
FROM dbo.Horses AS duplicateHorse
WHERE duplicateHorse.Name = N'B'+NCHAR(227)+N'o '+NCHAR(272)+NCHAR(234)+N'm'
  AND duplicateHorse.DateOfBirth = '2021-08-03'
  AND EXISTS
  (
      SELECT 1
      FROM dbo.Horses AS originalHorse
      WHERE originalHorse.Name = duplicateHorse.Name
        AND originalHorse.DateOfBirth = duplicateHorse.DateOfBirth
        AND originalHorse.OwnerUserId = duplicateHorse.OwnerUserId
        AND originalHorse.HorseId < duplicateHorse.HorseId
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.TrainingSessions AS t
      WHERE t.HorseId = duplicateHorse.HorseId
  )
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.MedicalRecords AS m
      WHERE m.HorseId = duplicateHorse.HorseId
  );
GO

INSERT INTO dbo.Horses (Name, Breed, Sex, DateOfBirth, OwnerUserId, Notes)
SELECT sampleHorse.Name, sampleHorse.Breed, sampleHorse.Sex, sampleHorse.DateOfBirth,
       owner.UserId, N'Seed horse'
FROM (VALUES
    (N'River Star', N'Thoroughbred', NCHAR(272)+NCHAR(7921)+N'c', CONVERT(date, '2020-03-12'), N'owner.thuy@horsecare.local'),
    (N'Moonlight', N'Arabian', N'C'+NCHAR(225)+N'i', CONVERT(date, '2021-06-08'), N'owner.thuy@horsecare.local'),
    (N'Golden Wind', N'Quarter Horse', NCHAR(272)+NCHAR(7921)+N'c', CONVERT(date, '2019-11-21'), N'owner.quang@horsecare.local'),
    (N'Ruby', N'Arabian', N'C'+NCHAR(225)+N'i', CONVERT(date, '2022-01-17'), N'owner.quang@horsecare.local'),
    (N'Silver Arrow', N'Thoroughbred', NCHAR(272)+NCHAR(7921)+N'c', CONVERT(date, '2020-09-04'), N'owner.hoa@horsecare.local'),
    (N'Autumn', N'Friesian', N'C'+NCHAR(225)+N'i', CONVERT(date, '2021-10-29'), N'owner.hoa@horsecare.local'),
    (N'Mistral', N'Arabian', NCHAR(272)+NCHAR(7921)+N'c', CONVERT(date, '2019-05-14'), N'owner.nam@horsecare.local'),
    (N'Bluebell', N'Friesian', N'C'+NCHAR(225)+N'i', CONVERT(date, '2022-07-03'), N'owner.nam@horsecare.local'),
    (N'Storm Chaser', N'Quarter Horse', NCHAR(272)+NCHAR(7921)+N'c', CONVERT(date, '2020-12-11'), N'owner.linh@horsecare.local'),
    (N'Cedar', N'Thoroughbred', N'C'+NCHAR(225)+N'i', CONVERT(date, '2021-02-25'), N'owner.linh@horsecare.local')
) AS sampleHorse(Name, Breed, Sex, DateOfBirth, OwnerUserName)
INNER JOIN dbo.AppUsers AS owner ON owner.UserName = sampleHorse.OwnerUserName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Horses AS existingHorse
    WHERE existingHorse.Name = sampleHorse.Name
      AND existingHorse.DateOfBirth = sampleHorse.DateOfBirth
);
GO

ALTER TABLE dbo.TrainingSessions DROP CONSTRAINT DF_TrainingSessions_Status;
ALTER TABLE dbo.TrainingSessions
    ADD CONSTRAINT DF_TrainingSessions_Status
    DEFAULT (NCHAR(272)+NCHAR(227)+N' l'+NCHAR(234)+N'n l'+NCHAR(7883)+N'ch') FOR Status;

ALTER TABLE dbo.MedicalRecords DROP CONSTRAINT DF_MedicalRecords_Status;
ALTER TABLE dbo.MedicalRecords
    ADD CONSTRAINT DF_MedicalRecords_Status
    DEFAULT (NCHAR(272)+N'ang theo d'+NCHAR(245)+N'i') FOR Status;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TrainingSessions)
BEGIN
    INSERT INTO dbo.TrainingSessions (HorseId, TrainerUserId, ScheduledAt, Activity, DurationMinutes, Status, Notes)
    SELECT h.HorseId, u.UserId, DATEADD(HOUR, 1, SYSDATETIME()),
           N'L'+NCHAR(224)+N'm quen '+NCHAR(273)+NCHAR(432)+NCHAR(7901)+N'ng '+NCHAR(273)+N'ua',
           60, NCHAR(272)+NCHAR(227)+N' l'+NCHAR(234)+N'n l'+NCHAR(7883)+N'ch',
           N'Bu'+NCHAR(7893)+N'i t'+NCHAR(7853)+N'p m'+NCHAR(7851)+N'u'
    FROM dbo.Horses AS h
    CROSS JOIN dbo.AppUsers AS u
    WHERE h.Name = N'Sao Mai' AND u.UserName = N'trainer@horsecare.local';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.MedicalRecords)
BEGIN
    INSERT INTO dbo.MedicalRecords (HorseId, DoctorUserId, VisitDate, Diagnosis, Treatment, Status)
    SELECT h.HorseId, u.UserId, CONVERT(date, SYSDATETIME()),
           N'Kh'+NCHAR(225)+N'm s'+NCHAR(7913)+N'c kh'+NCHAR(7887)+N'e '+NCHAR(273)+NCHAR(7883)+N'nh k'+NCHAR(7923),
           N'Theo d'+NCHAR(245)+N'i th'+NCHAR(7875)+N' tr'+NCHAR(7841)+N'ng v'+NCHAR(224)+N' b'+NCHAR(7893)+N' sung n'+NCHAR(432)+NCHAR(7899)+N'c',
           NCHAR(272)+N'ang theo d'+NCHAR(245)+N'i'
    FROM dbo.Horses AS h
    CROSS JOIN dbo.AppUsers AS u
    WHERE h.Name = N'Sao Mai' AND u.UserName = N'doctor@horsecare.local';
END;
GO

UPDATE dbo.Horses
SET Breed = N'Ng'+NCHAR(7921)+N'a '+NCHAR(273)+N'ua',
    Sex = N'C'+NCHAR(225)+N'i',
    Notes = N'Ng'+NCHAR(7921)+N'a m'+NCHAR(7851)+N'u'
WHERE Name = N'Sao Mai' AND DateOfBirth = '2022-04-15';

UPDATE dbo.TrainingSessions
SET Activity = N'L'+NCHAR(224)+N'm quen '+NCHAR(273)+NCHAR(432)+NCHAR(7901)+N'ng '+NCHAR(273)+N'ua',
    Status = NCHAR(272)+NCHAR(227)+N' l'+NCHAR(234)+N'n l'+NCHAR(7883)+N'ch',
    Notes = N'Bu'+NCHAR(7893)+N'i t'+NCHAR(7853)+N'p m'+NCHAR(7851)+N'u'
WHERE HorseId IN (SELECT HorseId FROM dbo.Horses WHERE Name = N'Sao Mai' AND DateOfBirth = '2022-04-15')
  AND TrainerUserId IN (SELECT UserId FROM dbo.AppUsers WHERE UserName = N'trainer@horsecare.local')
  AND Notes LIKE N'Bu%';

UPDATE dbo.MedicalRecords
SET Diagnosis = N'Kh'+NCHAR(225)+N'm s'+NCHAR(7913)+N'c kh'+NCHAR(7887)+N'e '+NCHAR(273)+NCHAR(7883)+N'nh k'+NCHAR(7923),
    Treatment = N'Theo d'+NCHAR(245)+N'i th'+NCHAR(7875)+N' tr'+NCHAR(7841)+N'ng v'+NCHAR(224)+N' b'+NCHAR(7893)+N' sung n'+NCHAR(432)+NCHAR(7899)+N'c',
    Status = NCHAR(272)+N'ang theo d'+NCHAR(245)+N'i'
WHERE HorseId IN (SELECT HorseId FROM dbo.Horses WHERE Name = N'Sao Mai' AND DateOfBirth = '2022-04-15')
  AND DoctorUserId IN (SELECT UserId FROM dbo.AppUsers WHERE UserName = N'doctor@horsecare.local')
  AND Diagnosis LIKE N'Kh%';
GO
