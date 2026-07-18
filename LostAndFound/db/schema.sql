/* =====================================================================  
   LostAndFound — Database schema (DB-First, source of truth)  
   ---------------------------------------------------------------------  
   This script is the AUTHORITATIVE data model. The EF Core entities in  
   Models/Entities are GENERATED from this DB via `dotnet ef dbcontext  
   scaffold` (see Data/Scaffolded/README.md). Edit the schema HERE first,  
   then re-run the DB + scaffold. Do not design tables in C#.  
  
   Target: SQL Server LocalDB  (localdb)\MSSQLLocalDB  
   Run   : sqlcmd -S "(localdb)\MSSQLLocalDB" -i db\schema.sql  
  
   Conventions (decided in docs/BASE_SETUP.md):  
   - User FK columns are nvarchar(450) to match AspNetUsers.Id.  
   - Every FK is ON DELETE NO ACTION (avoids SQL Server "multiple cascade  
     paths" errors AND preserves history). Only the tag-join -> parent  
     legs use CASCADE.  
   - Enums stored as int. CHECK constraints guard their ranges.  
   - All date/time columns are datetime2; created-at default SYSUTCDATETIME().  
   ===================================================================== */  
  
-- Filtered indexes (used below on AspNetUsers/AspNetRoles) require these parse-time  
-- SET options ON. sqlcmd leaves QUOTED_IDENTIFIER OFF by default, so assert it here.  
SET ANSI_NULLS ON;  
SET QUOTED_IDENTIFIER ON;  
GO  
  
IF DB_ID(N'LostAndFound') IS NULL  
    CREATE DATABASE [LostAndFound];  
GO  
  
USE [LostAndFound];  
GO  
  
-- Re-assert for this session/DB context before any filtered-index creation.  
SET ANSI_NULLS ON;  
SET QUOTED_IDENTIFIER ON;  
GO  
  
/* =====================================================================  
   1. ASP.NET Core Identity tables (standard EF Core 8 schema, string keys)  
      AspNetUsers carries the project's custom columns.  
   ===================================================================== */  
  
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
        -- Project-specific profile column (nullable so the default Identity register UI still  
        -- works; the FR-AUTH feature makes FullName required at the app layer). This is a generic  
        -- *school* lost-and-found for ANY level — user profile = FullName + Email + PhoneNumber only.  
        FullName             nvarchar(200)     NULL,  
        -- User management columns for admin control  
        IsBlocked            bit               NOT NULL CONSTRAINT DF_AspNetUsers_IsBlocked DEFAULT (0),  
        IsPostingBlocked     bit               NOT NULL CONSTRAINT DF_AspNetUsers_IsPostingBlocked DEFAULT (0)  
    );  
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;  
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);  
END  
GO  
  
-- Reconcile older DBs: drop the legacy university-only profile columns if present (idempotent, no-op on fresh DB).  
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL  
BEGIN  
    DROP INDEX IF EXISTS UX_AspNetUsers_StudentOrStaffCode ON dbo.AspNetUsers;  
    ALTER TABLE dbo.AspNetUsers DROP COLUMN IF EXISTS StudentOrStaffCode;  
    ALTER TABLE dbo.AspNetUsers DROP COLUMN IF EXISTS Department;  
END  
GO  
  
-- Reconcile older DBs: add the admin user-management columns if missing (idempotent, no-op on fresh DB).  
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
  
/* =====================================================================  
   2. Lookup / reference tables  
   ===================================================================== */  
  
-- Category: 2-level, system-defined (self-referencing ParentId).  
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
  
-- Location: a place inside the school / site.  
IF OBJECT_ID(N'dbo.Location', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.Location (  
        Id       int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_Location PRIMARY KEY,  
        Building nvarchar(100) NULL,  
        Name     nvarchar(150) NOT NULL  
    );  
END  
GO  
  
-- Tag: keyword label. NormalizedTag is the unique dedupe/match key.  
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
  
/* =====================================================================  
   3. Core domain tables  
   ===================================================================== */  
  
-- LostAlert: a Member's "watch" subscription (publish/subscribe matching).  
IF OBJECT_ID(N'dbo.LostAlert', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.LostAlert (  
        Id          int           NOT NULL IDENTITY(1,1) CONSTRAINT PK_LostAlert PRIMARY KEY,  
        OwnerUserId nvarchar(450) NOT NULL,  
        CategoryId  int           NULL,  
        LocationId  int           NULL,  
        FromDate    datetime2     NULL,  
        ToDate      datetime2     NULL,  
        Keyword     nvarchar(200) NULL,  
        IsActive    bit           NOT NULL CONSTRAINT DF_LostAlert_IsActive DEFAULT (1),  
        CreatedAt   datetime2     NOT NULL CONSTRAINT DF_LostAlert_CreatedAt DEFAULT (SYSUTCDATETIME()),  
        CONSTRAINT FK_LostAlert_AspNetUsers_OwnerUserId FOREIGN KEY (OwnerUserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,  
        CONSTRAINT FK_LostAlert_Category_CategoryId     FOREIGN KEY (CategoryId)  REFERENCES dbo.Category (Id)    ON DELETE NO ACTION,  
        CONSTRAINT FK_LostAlert_Location_LocationId     FOREIGN KEY (LocationId)  REFERENCES dbo.Location (Id)    ON DELETE NO ACTION  
    );  
    CREATE INDEX IX_LostAlert_OwnerUserId ON dbo.LostAlert (OwnerUserId);  
    CREATE INDEX IX_LostAlert_CategoryId  ON dbo.LostAlert (CategoryId);  
    CREATE INDEX IX_LostAlert_LocationId  ON dbo.LostAlert (LocationId);  
END  
GO  
  
-- FoundItem: the reported found item + public listing. Status/HoldingType are int enums.  
IF OBJECT_ID(N'dbo.FoundItem', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.FoundItem (  
        Id                        int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_FoundItem PRIMARY KEY,  
        Title                     nvarchar(200)  NOT NULL,  
        Description               nvarchar(2000) NULL,  
        CategoryId                int            NOT NULL,  
        LocationId                int            NOT NULL,  
        FoundAt                   datetime2      NOT NULL,  
        Status                    int            NOT NULL CONSTRAINT DF_FoundItem_Status DEFAULT (1), -- 1 = Open  
        HoldingType               int            NOT NULL CONSTRAINT DF_FoundItem_HoldingType DEFAULT (0), -- 0 = SelfHeld  
        StorageLocation           nvarchar(200)  NULL,  
        PrivateMarks              nvarchar(1000) NULL,  -- HIDDEN: never shown on public views  
        -- NOTE: images live in the child table dbo.FoundItemImage (1-to-many). The legacy single  
        -- ImagePath column was removed; existing values are migrated below.  
        ReporterUserId            nvarchar(450)  NOT NULL,  
        CustodianStaffId          nvarchar(450)  NULL,  
        HolderConfirmedHandover   bit            NOT NULL CONSTRAINT DF_FoundItem_HolderConfirmed DEFAULT (0),  
        ClaimantConfirmedHandover bit            NOT NULL CONSTRAINT DF_FoundItem_ClaimantConfirmed DEFAULT (0),  
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
  
-- FoundItemTag: M-N join (surrogate Id so it scaffolds as an explicit entity).  
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
  
-- FoundItemImage: 1-to-many photos of a FoundItem. The COVER image is the row with the lowest  
-- SortOrder (0); the rest are the "other" images. Cascade-deletes with its FoundItem.  
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
  
-- Reconcile older DBs: migrate the legacy single FoundItem.ImagePath into FoundItemImage (as the  
-- cover, SortOrder 0) and drop the column. Idempotent: runs only while the column still exists.  
-- Wrapped in dynamic SQL: on a FRESH DB the column doesn't exist, and column names aren't covered by  
-- deferred name resolution, so a static reference would fail the whole batch with "Invalid column name".  
IF COL_LENGTH(N'dbo.FoundItem', N'ImagePath') IS NOT NULL  
BEGIN  
    EXEC sys.sp_executesql N'  
        INSERT INTO dbo.FoundItemImage (FoundItemId, Url, SortOrder)  
        SELECT Id, ImagePath, 0 FROM dbo.FoundItem WHERE ImagePath IS NOT NULL AND ImagePath <> N'''';  
        ALTER TABLE dbo.FoundItem DROP COLUMN ImagePath;';  
END  
GO  
  
-- LostAlertTag: M-N join for watch-subscription tags.  
IF OBJECT_ID(N'dbo.LostAlertTag', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.LostAlertTag (  
        Id          int NOT NULL IDENTITY(1,1) CONSTRAINT PK_LostAlertTag PRIMARY KEY,  
        LostAlertId int NOT NULL,  
        TagId       int NOT NULL,  
        CONSTRAINT UX_LostAlertTag_Alert_Tag UNIQUE (LostAlertId, TagId),  
        CONSTRAINT FK_LostAlertTag_LostAlert_LostAlertId FOREIGN KEY (LostAlertId) REFERENCES dbo.LostAlert (Id) ON DELETE CASCADE,  
        CONSTRAINT FK_LostAlertTag_Tag_TagId             FOREIGN KEY (TagId)       REFERENCES dbo.Tag (Id)       ON DELETE NO ACTION  
    );  
    CREATE INDEX IX_LostAlertTag_TagId ON dbo.LostAlertTag (TagId);  
END  
GO  
  
-- Claim: a request to reclaim a FoundItem. Status is an int enum.  
IF OBJECT_ID(N'dbo.Claim', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.Claim (  
        Id                  int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_Claim PRIMARY KEY,  
        FoundItemId         int            NOT NULL,  
        ClaimantUserId      nvarchar(450)  NOT NULL,  
        VerificationDetails nvarchar(2000) NOT NULL,  
        EvidenceImagePath   nvarchar(400)  NULL,  
        Status              int            NOT NULL CONSTRAINT DF_Claim_Status DEFAULT (0), -- 0 = Pending  
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
  
-- CameraCheckRequest: a request for staff to review camera footage. Status int enum.  
IF OBJECT_ID(N'dbo.CameraCheckRequest', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.CameraCheckRequest (  
        Id               int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_CameraCheckRequest PRIMARY KEY,  
        RequesterUserId  nvarchar(450)  NOT NULL,  
        LocationId       int            NOT NULL,  
        FromTime         datetime2      NOT NULL,  
        ToTime           datetime2      NOT NULL,  
        ItemDescription  nvarchar(1000) NOT NULL,  
        Status           int            NOT NULL CONSTRAINT DF_CameraCheckRequest_Status DEFAULT (0), -- 0 = Pending  
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
  
-- ThankYou: rating + thanks after a successful return. At most one per FoundItem.  
IF OBJECT_ID(N'dbo.ThankYou', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.ThankYou (  
        Id          int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_ThankYou PRIMARY KEY,  
        FoundItemId int            NOT NULL,  
        FromUserId  nvarchar(450)  NOT NULL,  
        ToUserId    nvarchar(450)  NOT NULL,  
        Rating      int            NOT NULL,  
        Message     nvarchar(1000) NULL,  
        CreatedAt   datetime2      NOT NULL CONSTRAINT DF_ThankYou_CreatedAt DEFAULT (SYSUTCDATETIME()),  
        CONSTRAINT CK_ThankYou_Rating CHECK (Rating BETWEEN 1 AND 5),  
        CONSTRAINT UX_ThankYou_FoundItemId UNIQUE (FoundItemId), -- 1:0..1 with FoundItem  
        CONSTRAINT FK_ThankYou_FoundItem_FoundItemId   FOREIGN KEY (FoundItemId) REFERENCES dbo.FoundItem (Id)   ON DELETE NO ACTION,  
        CONSTRAINT FK_ThankYou_AspNetUsers_FromUserId  FOREIGN KEY (FromUserId)  REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION,  
        CONSTRAINT FK_ThankYou_AspNetUsers_ToUserId    FOREIGN KEY (ToUserId)    REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION  
    );  
    CREATE INDEX IX_ThankYou_FromUserId ON dbo.ThankYou (FromUserId);  
    CREATE INDEX IX_ThankYou_ToUserId   ON dbo.ThankYou (ToUserId);  
END  
GO  
  
-- Notification: stored in-app notification (also pushed via SignalR by the service layer).  
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
  
-- AuditLog: business event log; the source for the public timeline (IsPublic = 1 rows).  
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL  
BEGIN  
    CREATE TABLE dbo.AuditLog (  
        Id          int            NOT NULL IDENTITY(1,1) CONSTRAINT PK_AuditLog PRIMARY KEY,  
        ActorUserId nvarchar(450)  NULL,  -- nullable: system-initiated actions may have no actor  
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