-- LostAndFound - schema database
-- chay: sqlcmd -S "(localdb)\MSSQLLocalDB" -i db\schema.sql

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'LostAndFound') IS NULL
    CREATE DATABASE [LostAndFound];
GO

USE [LostAndFound];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- cac bang cua Identity (user / role)
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id               nvarchar(450)  NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name             nvarchar(256)  NULL,
        NormalizedName   nvarchar(256)  NULL,
        ConcurrencyStamp nvarchar(max)  NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id                   nvarchar(450)     NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName             nvarchar(256)     NULL,
        NormalizedUserName   nvarchar(256)     NULL,
        Email                nvarchar(256)     NULL,
        NormalizedEmail      nvarchar(256)     NULL,
        EmailConfirmed       bit               NOT NULL CONSTRAINT DF_AspNetUsers_EmailConfirmed DEFAULT (0),
        PasswordHash         nvarchar(max)     NULL,
        SecurityStamp        nvarchar(max)     NULL,
        ConcurrencyStamp     nvarchar(max)     NULL,
        PhoneNumber          nvarchar(max)     NULL,
        PhoneNumberConfirmed bit               NOT NULL CONSTRAINT DF_AspNetUsers_PhoneConfirmed DEFAULT (0),
        TwoFactorEnabled     bit               NOT NULL CONSTRAINT DF_AspNetUsers_TwoFactor DEFAULT (0),
        LockoutEnd           datetimeoffset(7) NULL,
        LockoutEnabled       bit               NOT NULL CONSTRAINT DF_AspNetUsers_LockoutEnabled DEFAULT (0),
        AccessFailedCount    int               NOT NULL CONSTRAINT DF_AspNetUsers_AccessFailed DEFAULT (0),

        FullName             nvarchar(200)     NULL,

        IsBlocked            bit               NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0),
        IsPostingBlocked     bit               NOT NULL CONSTRAINT DF_AspNetUsers_IsPostingBlocked DEFAULT (0)
    );
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL
BEGIN
    DROP INDEX IF EXISTS UX_AspNetUsers_StudentOrStaffCode ON dbo.AspNetUsers;
    ALTER TABLE dbo.AspNetUsers DROP COLUMN IF EXISTS StudentOrStaffCode;
    ALTER TABLE dbo.AspNetUsers DROP COLUMN IF EXISTS Department;
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AspNetUsers', N'IsBlocked') IS NULL
        ALTER TABLE dbo.AspNetUsers
            ADD IsBlocked bit NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0);
    IF COL_LENGTH(N'dbo.AspNetUsers', N'IsPostingBlocked') IS NULL
        ALTER TABLE dbo.AspNetUsers
            ADD IsPostingBlocked bit NOT NULL CONSTRAINT DF_AspNetUsers_IsPostingBlocked DEFAULT (0);
END
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id         int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId     nvarchar(450) NOT NULL,
        ClaimType  nvarchar(max) NULL,
        ClaimValue nvarchar(max) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id         int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId     nvarchar(450) NOT NULL,
        ClaimType  nvarchar(max) NULL,
        ClaimValue nvarchar(max) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider       nvarchar(450) NOT NULL,
        ProviderKey         nvarchar(450) NOT NULL,
        ProviderDisplayName nvarchar(max) NULL,
        UserId              nvarchar(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId nvarchar(450) NOT NULL,
        RoleId nvarchar(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);
END
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId        nvarchar(450) NOT NULL,
        LoginProvider nvarchar(450) NOT NULL,
        Name          nvarchar(450) NOT NULL,
        Value         nvarchar(max) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END
GO

-- bo may bang khong dung nua
DROP TABLE IF EXISTS dbo.LostAlertTag;
DROP TABLE IF EXISTS dbo.LostAlert;
DROP TABLE IF EXISTS dbo.ThankYou;
GO

-- danh muc, dia diem, the
IF OBJECT_ID(N'dbo.Category', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Category (
        Id       int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Category PRIMARY KEY,
        ParentId int           NULL,
        Name     nvarchar(100) NOT NULL,
        CONSTRAINT FK_Category_Category_ParentId FOREIGN KEY (ParentId) REFERENCES dbo.Category (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_Category_ParentId ON dbo.Category (ParentId);
END
GO

IF OBJECT_ID(N'dbo.Location', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location (
        Id       int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Location PRIMARY KEY,
        Building nvarchar(100) NULL,
        Name     nvarchar(150) NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Tag', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tag (
        Id            int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Tag PRIMARY KEY,
        DisplayTag    nvarchar(100) NOT NULL,
        NormalizedTag nvarchar(100) NOT NULL,
        CONSTRAINT UX_Tag_NormalizedTag UNIQUE (NormalizedTag)
    );
END
GO

-- do nhat duoc
IF OBJECT_ID(N'dbo.FoundItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FoundItem (
        Id                        int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_FoundItem PRIMARY KEY,
        Title                     nvarchar(200)  NOT NULL,
        Description               nvarchar(2000) NULL,
        CategoryId                int            NOT NULL,
        LocationId                int            NOT NULL,
        FoundAt                   datetime2      NOT NULL,
        Status                    int            NOT NULL CONSTRAINT DF_FoundItem_Status DEFAULT (1),
        HoldingType               int            NOT NULL CONSTRAINT DF_FoundItem_HoldingType DEFAULT (0),
        StorageLocation           nvarchar(200)  NULL,
        PrivateMarks              nvarchar(1000) NULL,

        ReporterUserId            nvarchar(450)  NOT NULL,
        CustodianStaffId          nvarchar(450)  NULL,
        HolderConfirmedHandover   bit            NOT NULL CONSTRAINT DF_FoundItem_HolderConfirmed DEFAULT (0),
        ClaimantConfirmedHandover bit            NOT NULL CONSTRAINT DF_FoundItem_ClaimantConfirmed DEFAULT (0),

        HolderConfirmedAt         datetime2      NULL,
        ClaimantConfirmedAt       datetime2      NULL,
        CreatedAt                 datetime2      NOT NULL CONSTRAINT DF_FoundItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_FoundItem_Status      CHECK (Status BETWEEN 0 AND 5),
        CONSTRAINT CK_FoundItem_HoldingType CHECK (HoldingType IN (0, 1)),
        CONSTRAINT FK_FoundItem_Category_CategoryId           FOREIGN KEY (CategoryId)       REFERENCES dbo.Category (Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_FoundItem_Location_LocationId           FOREIGN KEY (LocationId)       REFERENCES dbo.Location (Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_FoundItem_AspNetUsers_ReporterUserId    FOREIGN KEY (ReporterUserId)   REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_FoundItem_AspNetUsers_CustodianStaffId  FOREIGN KEY (CustodianStaffId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_FoundItem_CategoryId       ON dbo.FoundItem (CategoryId);
    CREATE INDEX IX_FoundItem_LocationId       ON dbo.FoundItem (LocationId);
    CREATE INDEX IX_FoundItem_ReporterUserId   ON dbo.FoundItem (ReporterUserId);
    CREATE INDEX IX_FoundItem_CustodianStaffId ON dbo.FoundItem (CustodianStaffId);
    CREATE INDEX IX_FoundItem_Status           ON dbo.FoundItem (Status);
END
GO

IF OBJECT_ID(N'dbo.FoundItemTag', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FoundItemTag (
        Id          int NOT NULL IDENTITY(1,1) CONSTRAINT PK_FoundItemTag PRIMARY KEY,
        FoundItemId int NOT NULL,
        TagId       int NOT NULL,
        CONSTRAINT UX_FoundItemTag_Item_Tag UNIQUE (FoundItemId, TagId),
        CONSTRAINT FK_FoundItemTag_FoundItem_FoundItemId FOREIGN KEY (FoundItemId) REFERENCES dbo.FoundItem (Id) ON DELETE CASCADE,
        CONSTRAINT FK_FoundItemTag_Tag_TagId             FOREIGN KEY (TagId)       REFERENCES dbo.Tag (Id)       ON DELETE NO ACTION
    );
    CREATE INDEX IX_FoundItemTag_TagId ON dbo.FoundItemTag (TagId);
END
GO

IF OBJECT_ID(N'dbo.FoundItemImage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FoundItemImage (
        Id          int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_FoundItemImage PRIMARY KEY,
        FoundItemId int           NOT NULL,
        Url         nvarchar(400) NOT NULL,
        SortOrder   int           NOT NULL CONSTRAINT DF_FoundItemImage_SortOrder DEFAULT (0),
        CONSTRAINT FK_FoundItemImage_FoundItem_FoundItemId FOREIGN KEY (FoundItemId) REFERENCES dbo.FoundItem (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_FoundItemImage_FoundItemId ON dbo.FoundItemImage (FoundItemId);
END
GO

IF COL_LENGTH(N'dbo.FoundItem', N'ImagePath') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        INSERT INTO dbo.FoundItemImage (FoundItemId, Url, SortOrder)
        SELECT Id, ImagePath, 0 FROM dbo.FoundItem WHERE ImagePath IS NOT NULL AND ImagePath <> N'''';
        ALTER TABLE dbo.FoundItem DROP COLUMN ImagePath;';
END
GO

-- do bi mat
IF OBJECT_ID(N'dbo.LostItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LostItem (
        Id          int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_LostItem PRIMARY KEY,
        Title       nvarchar(200)  NOT NULL,
        Description nvarchar(2000) NULL,
        CategoryId  int            NOT NULL,
        LocationId  int            NOT NULL,
        LostAt      datetime2      NOT NULL,
        Status      int            NOT NULL CONSTRAINT DF_LostItem_Status DEFAULT (0),
        OwnerUserId nvarchar(450)  NOT NULL,
        CreatedAt   datetime2      NOT NULL CONSTRAINT DF_LostItem_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_LostItem_Status CHECK (Status BETWEEN 0 AND 2),
        CONSTRAINT FK_LostItem_Category_CategoryId     FOREIGN KEY (CategoryId)  REFERENCES dbo.Category (Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_LostItem_Location_LocationId     FOREIGN KEY (LocationId)  REFERENCES dbo.Location (Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_LostItem_AspNetUsers_OwnerUserId FOREIGN KEY (OwnerUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_LostItem_CategoryId  ON dbo.LostItem (CategoryId);
    CREATE INDEX IX_LostItem_LocationId  ON dbo.LostItem (LocationId);
    CREATE INDEX IX_LostItem_OwnerUserId ON dbo.LostItem (OwnerUserId);
    CREATE INDEX IX_LostItem_Status      ON dbo.LostItem (Status);
END
GO

IF OBJECT_ID(N'dbo.LostItemImage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LostItemImage (
        Id         int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_LostItemImage PRIMARY KEY,
        LostItemId int           NOT NULL,
        Url        nvarchar(400) NOT NULL,
        SortOrder  int           NOT NULL CONSTRAINT DF_LostItemImage_SortOrder DEFAULT (0),
        CONSTRAINT FK_LostItemImage_LostItem_LostItemId FOREIGN KEY (LostItemId) REFERENCES dbo.LostItem (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_LostItemImage_LostItemId ON dbo.LostItemImage (LostItemId);
END
GO

IF OBJECT_ID(N'dbo.LostItemTag', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LostItemTag (
        Id         int NOT NULL IDENTITY(1,1) CONSTRAINT PK_LostItemTag PRIMARY KEY,
        LostItemId int NOT NULL,
        TagId      int NOT NULL,
        CONSTRAINT UX_LostItemTag_Item_Tag UNIQUE (LostItemId, TagId),
        CONSTRAINT FK_LostItemTag_LostItem_LostItemId FOREIGN KEY (LostItemId) REFERENCES dbo.LostItem (Id) ON DELETE CASCADE,
        CONSTRAINT FK_LostItemTag_Tag_TagId           FOREIGN KEY (TagId)      REFERENCES dbo.Tag (Id)      ON DELETE NO ACTION
    );
    CREATE INDEX IX_LostItemTag_TagId ON dbo.LostItemTag (TagId);
END
GO

-- yeu cau nhan lai + tin nhan
IF OBJECT_ID(N'dbo.Claim', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Claim (
        Id                  int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Claim PRIMARY KEY,
        FoundItemId         int            NOT NULL,
        ClaimantUserId      nvarchar(450)  NOT NULL,
        VerificationDetails nvarchar(2000) NOT NULL,
        EvidenceImagePath   nvarchar(400)  NULL,

        ContactPhone        nvarchar(30)   NULL,
        ContactEmail        nvarchar(256)  NULL,
        Status              int            NOT NULL CONSTRAINT DF_Claim_Status DEFAULT (0),
        HandledByUserId     nvarchar(450)  NULL,
        RejectReason        nvarchar(1000) NULL,
        CreatedAt           datetime2      NOT NULL CONSTRAINT DF_Claim_CreatedAt DEFAULT (SYSUTCDATETIME()),
        HandledAt           datetime2      NULL,
        CONSTRAINT CK_Claim_Status CHECK (Status BETWEEN 0 AND 2),
        CONSTRAINT FK_Claim_FoundItem_FoundItemId        FOREIGN KEY (FoundItemId)     REFERENCES dbo.FoundItem (Id)   ON DELETE NO ACTION,
        CONSTRAINT FK_Claim_AspNetUsers_ClaimantUserId   FOREIGN KEY (ClaimantUserId)  REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Claim_AspNetUsers_HandledByUserId  FOREIGN KEY (HandledByUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_Claim_FoundItemId     ON dbo.Claim (FoundItemId);
    CREATE INDEX IX_Claim_ClaimantUserId  ON dbo.Claim (ClaimantUserId);
    CREATE INDEX IX_Claim_HandledByUserId ON dbo.Claim (HandledByUserId);
END
GO

IF COL_LENGTH(N'dbo.FoundItem', N'HolderConfirmedAt') IS NULL
    ALTER TABLE dbo.FoundItem ADD HolderConfirmedAt datetime2 NULL;
GO
IF COL_LENGTH(N'dbo.FoundItem', N'ClaimantConfirmedAt') IS NULL
    ALTER TABLE dbo.FoundItem ADD ClaimantConfirmedAt datetime2 NULL;
GO

IF COL_LENGTH(N'dbo.Claim', N'ContactPhone') IS NULL
    ALTER TABLE dbo.Claim ADD ContactPhone nvarchar(30) NULL;
GO
IF COL_LENGTH(N'dbo.Claim', N'ContactEmail') IS NULL
    ALTER TABLE dbo.Claim ADD ContactEmail nvarchar(256) NULL;
GO

IF OBJECT_ID(N'dbo.ClaimImage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClaimImage (
        Id        int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_ClaimImage PRIMARY KEY,
        ClaimId   int           NOT NULL,
        Url       nvarchar(400) NOT NULL,
        SortOrder int           NOT NULL CONSTRAINT DF_ClaimImage_SortOrder DEFAULT (0),
        CONSTRAINT FK_ClaimImage_Claim_ClaimId FOREIGN KEY (ClaimId) REFERENCES dbo.Claim (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ClaimImage_ClaimId ON dbo.ClaimImage (ClaimId);
END
GO

IF COL_LENGTH(N'dbo.Claim', N'EvidenceImagePath') IS NOT NULL
BEGIN
    EXEC sys.sp_executesql N'
        INSERT INTO dbo.ClaimImage (ClaimId, Url, SortOrder)
        SELECT Id, EvidenceImagePath, 0 FROM dbo.Claim WHERE EvidenceImagePath IS NOT NULL AND EvidenceImagePath <> N'''';
        ALTER TABLE dbo.Claim DROP COLUMN EvidenceImagePath;';
END
GO

IF OBJECT_ID(N'dbo.ClaimMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClaimMessage (
        Id           int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_ClaimMessage PRIMARY KEY,
        ClaimId      int            NOT NULL,
        SenderUserId nvarchar(450)  NOT NULL,
        Body         nvarchar(2000) NOT NULL,
        CreatedAt    datetime2      NOT NULL CONSTRAINT DF_ClaimMessage_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ClaimMessage_Claim_ClaimId            FOREIGN KEY (ClaimId)      REFERENCES dbo.Claim (Id)       ON DELETE CASCADE,
        CONSTRAINT FK_ClaimMessage_AspNetUsers_SenderUserId FOREIGN KEY (SenderUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_ClaimMessage_ClaimId ON dbo.ClaimMessage (ClaimId);
END
GO

-- yeu cau xem camera
IF OBJECT_ID(N'dbo.CameraCheckRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CameraCheckRequest (
        Id               int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_CameraCheckRequest PRIMARY KEY,
        RequesterUserId  nvarchar(450)  NOT NULL,
        LocationId       int            NOT NULL,
        FromTime         datetime2      NOT NULL,
        ToTime           datetime2      NOT NULL,
        ItemDescription  nvarchar(1000) NOT NULL,
        Status           int            NOT NULL CONSTRAINT DF_CameraCheckRequest_Status DEFAULT (0),
        HandledByStaffId nvarchar(450)  NULL,
        ResponseNote     nvarchar(1000) NULL,
        CreatedAt        datetime2      NOT NULL CONSTRAINT DF_CameraCheckRequest_CreatedAt DEFAULT (SYSUTCDATETIME()),
        HandledAt        datetime2      NULL,
        CONSTRAINT CK_CameraCheckRequest_Status CHECK (Status BETWEEN 0 AND 3),
        CONSTRAINT FK_CameraCheckRequest_AspNetUsers_RequesterUserId  FOREIGN KEY (RequesterUserId)  REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CameraCheckRequest_Location_LocationId          FOREIGN KEY (LocationId)       REFERENCES dbo.Location (Id)    ON DELETE NO ACTION,
        CONSTRAINT FK_CameraCheckRequest_AspNetUsers_HandledByStaffId FOREIGN KEY (HandledByStaffId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_CameraCheckRequest_RequesterUserId  ON dbo.CameraCheckRequest (RequesterUserId);
    CREATE INDEX IX_CameraCheckRequest_LocationId       ON dbo.CameraCheckRequest (LocationId);
    CREATE INDEX IX_CameraCheckRequest_HandledByStaffId ON dbo.CameraCheckRequest (HandledByStaffId);
END
GO

-- thong bao + audit log
IF OBJECT_ID(N'dbo.Notification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notification (
        Id              int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Notification PRIMARY KEY,
        RecipientUserId nvarchar(450)  NOT NULL,
        Type            nvarchar(100)  NOT NULL,
        Title           nvarchar(200)  NOT NULL,
        Message         nvarchar(1000) NULL,
        LinkUrl         nvarchar(400)  NULL,
        IsRead          bit            NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT (0),
        CreatedAt       datetime2      NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Notification_AspNetUsers_RecipientUserId FOREIGN KEY (RecipientUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_Notification_Recipient_IsRead ON dbo.Notification (RecipientUserId, IsRead);
END
GO

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog (
        Id          int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_AuditLog PRIMARY KEY,
        ActorUserId nvarchar(450)  NULL,
        Action      nvarchar(100)  NOT NULL,
        EntityType  nvarchar(100)  NOT NULL,
        EntityId    nvarchar(100)  NOT NULL,
        FromStatus  nvarchar(50)   NULL,
        ToStatus    nvarchar(50)   NULL,
        Detail      nvarchar(2000) NULL,
        IsPublic    bit            NOT NULL CONSTRAINT DF_AuditLog_IsPublic DEFAULT (0),
        CreatedAt   datetime2      NOT NULL CONSTRAINT DF_AuditLog_CreatedAt DEFAULT (SYSUTCDATETIME()),
        IpAddress   nvarchar(64)   NULL,
        CONSTRAINT FK_AuditLog_AspNetUsers_ActorUserId FOREIGN KEY (ActorUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_AuditLog_ActorUserId ON dbo.AuditLog (ActorUserId);
    CREATE INDEX IX_AuditLog_Entity      ON dbo.AuditLog (EntityType, EntityId);
    CREATE INDEX IX_AuditLog_IsPublic    ON dbo.AuditLog (IsPublic);
END
GO

PRINT 'LostAndFound schema applied.';
GO
