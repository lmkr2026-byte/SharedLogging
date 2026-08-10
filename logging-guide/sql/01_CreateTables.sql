/* =====================================================================
   Unified logging schema for the ApisLogsManagement database.

   Design decision: ONE default table (dbo.ApiLogs) that every one of the
   29 services writes to by default - simplest to query/dashboard across
   services. dbo.LogRoutingConfig lets you carve specific services or log
   categories out into their own dedicated table later (e.g. a very
   high-volume service, or a service whose logs need a longer retention
   policy) WITHOUT changing the stored procedure or the shared class
   library - you just add a config row.
   ===================================================================== */

IF OBJECT_ID('dbo.ApiLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiLogs
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ServiceName     NVARCHAR(100)   NOT NULL,
        CorrelationId   NVARCHAR(100)   NOT NULL,
        ClientId        NVARCHAR(100)   NULL,
        APIMethod       NVARCHAR(10)    NULL,
        APIURL          NVARCHAR(2048)  NULL,
        RequestBody     NVARCHAR(MAX)   NULL,
        ResponseBody    NVARCHAR(MAX)   NULL,
        StatusCode      INT             NULL,
        DurationMs      BIGINT          NULL,
        ClientIP        NVARCHAR(50)    NULL,
        UserAgent       NVARCHAR(512)   NULL,
        CreatedBy       BIGINT          NOT NULL DEFAULT (0),
        CreatedOn       DATETIME2(3)    NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedBy       BIGINT          NULL,
        UpdatedOn       DATETIME2(3)    NULL
    );

    CREATE INDEX IX_ApiLogs_ServiceName_CreatedOn ON dbo.ApiLogs (ServiceName, CreatedOn DESC);
    CREATE INDEX IX_ApiLogs_CorrelationId ON dbo.ApiLogs (CorrelationId);
END
GO

/* Optional per-service/per-category override table.
   If a (ServiceName) or (ServiceName + LogCategory) row exists here and is
   active, usp_ApiLogs_Save routes that service's rows into TargetTable
   instead of dbo.ApiLogs. TargetTable must be a table that already exists
   with the same shape as dbo.ApiLogs (same column list) - see the template
   at the bottom of this file. */
IF OBJECT_ID('dbo.LogRoutingConfig', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogRoutingConfig
    (
        Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ServiceName   NVARCHAR(100)  NOT NULL,
        LogCategory   NVARCHAR(50)   NULL,          -- NULL = applies to all categories for this service
        TargetTable   NVARCHAR(128)  NOT NULL,       -- table name only, always resolved under schema dbo
        IsActive      BIT            NOT NULL DEFAULT (1)
    );

    CREATE UNIQUE INDEX UX_LogRoutingConfig_Service_Category
        ON dbo.LogRoutingConfig (ServiceName, ISNULL(LogCategory, N''));
END
GO

/* Example: give one particularly high-volume service its own table.
   1) Create the dedicated table with the identical column list as dbo.ApiLogs:

        SELECT TOP (0) * INTO dbo.ApiLogs_HighVolumeService FROM dbo.ApiLogs;
        ALTER TABLE dbo.ApiLogs_HighVolumeService ADD CONSTRAINT PK_ApiLogs_HighVolumeService PRIMARY KEY (Id);

   2) Point that service at it:

        INSERT INTO dbo.LogRoutingConfig (ServiceName, LogCategory, TargetTable, IsActive)
        VALUES ('HighVolumeService.API', NULL, 'ApiLogs_HighVolumeService', 1);

   No change to usp_ApiLogs_Save or to LMKR.Shared.Logging is required. */
