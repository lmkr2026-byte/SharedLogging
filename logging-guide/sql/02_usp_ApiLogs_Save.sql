/* =====================================================================
   usp_ApiLogs_Save
   The ONE stored procedure every one of the 29 services calls (via
   LMKR.Shared.Logging.Repositories.ApiLoggingRepository) for both the
   request-insert and the response-update. It is the single place that
   decides which physical table a row lands in, by consulting
   dbo.LogRoutingConfig and defaulting to dbo.ApiLogs.
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.usp_ApiLogs_Save
(
    @Action         VARCHAR(10),        -- 'INSERT' or 'UPDATE'
    @Id             BIGINT = NULL,       -- required for UPDATE
    @ServiceName    NVARCHAR(100),
    @CorrelationId  NVARCHAR(100),
    @ClientId       NVARCHAR(100) = NULL,
    @APIMethod      NVARCHAR(10)  = NULL,
    @APIURL         NVARCHAR(2048)= NULL,
    @RequestBody    NVARCHAR(MAX) = NULL,
    @ResponseBody   NVARCHAR(MAX) = NULL,
    @StatusCode     INT           = NULL,
    @DurationMs     BIGINT        = NULL,
    @ClientIP       NVARCHAR(50)  = NULL,
    @UserAgent      NVARCHAR(512) = NULL,
    @CreatedBy      BIGINT        = 0,
    @UpdatedBy      BIGINT        = NULL,
    @LogCategory    NVARCHAR(50)  = NULL,   -- optional: e.g. 'Request', 'Error', 'Audit' - only used for routing lookup
    @OutId          BIGINT OUTPUT,
    @ErrorCode      BIGINT OUTPUT,
    @ErrorMessage   NVARCHAR(MAX) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @ErrorCode = 0;
    SET @ErrorMessage = N'';
    SET @OutId = 0;

    BEGIN TRY
        ----------------------------------------------------------------
        -- 1. Resolve the target table for this service/category.
        ----------------------------------------------------------------
        DECLARE @TargetTable NVARCHAR(128);

        SELECT TOP (1) @TargetTable = TargetTable
        FROM dbo.LogRoutingConfig
        WHERE ServiceName = @ServiceName
          AND IsActive = 1
          AND (LogCategory = @LogCategory OR LogCategory IS NULL)
        ORDER BY CASE WHEN LogCategory IS NULL THEN 1 ELSE 0 END; -- prefer the category-specific row if both exist

        IF @TargetTable IS NULL
            SET @TargetTable = N'ApiLogs';

        -- Guard against a bad/typo'd config row silently failing or, worse,
        -- being used for SQL injection via dynamic SQL below.
        IF OBJECT_ID(N'dbo.' + @TargetTable, 'U') IS NULL
        BEGIN
            SET @TargetTable = N'ApiLogs';
        END

        DECLARE @QualifiedTable NVARCHAR(258) = N'dbo.' + QUOTENAME(@TargetTable);
        DECLARE @Sql NVARCHAR(MAX);

        ----------------------------------------------------------------
        -- 2. INSERT (request half) or UPDATE (response half).
        ----------------------------------------------------------------
        IF @Action = 'INSERT'
        BEGIN
            SET @Sql = N'
                INSERT INTO ' + @QualifiedTable + N'
                    (ServiceName, CorrelationId, ClientId, APIMethod, APIURL,
                     RequestBody, ClientIP, UserAgent, CreatedBy, CreatedOn)
                OUTPUT INSERTED.Id
                VALUES
                    (@ServiceName, @CorrelationId, @ClientId, @APIMethod, @APIURL,
                     @RequestBody, @ClientIP, @UserAgent, @CreatedBy, SYSUTCDATETIME());';

            -- NOTE: this INSERT...EXEC captures sp_executesql's OUTPUT result
            -- set. SQL Server does not allow nesting INSERT...EXEC, so this
            -- procedure can only be called directly (ADO.NET, as the shared
            -- class library does) - it can never itself be the target of a
            -- caller's own "INSERT ... EXEC dbo.usp_ApiLogs_Save".
            DECLARE @InsertedIds TABLE (Id BIGINT);
            INSERT INTO @InsertedIds
            EXEC sp_executesql @Sql,
                N'@ServiceName NVARCHAR(100), @CorrelationId NVARCHAR(100), @ClientId NVARCHAR(100),
                  @APIMethod NVARCHAR(10), @APIURL NVARCHAR(2048), @RequestBody NVARCHAR(MAX),
                  @ClientIP NVARCHAR(50), @UserAgent NVARCHAR(512), @CreatedBy BIGINT',
                @ServiceName, @CorrelationId, @ClientId, @APIMethod, @APIURL, @RequestBody,
                @ClientIP, @UserAgent, @CreatedBy;

            SELECT TOP (1) @OutId = Id FROM @InsertedIds;
        END
        ELSE IF @Action = 'UPDATE'
        BEGIN
            IF @Id IS NULL
            BEGIN
                SET @ErrorCode = -2;
                SET @ErrorMessage = N'@Id is required for UPDATE.';
                RETURN;
            END

            SET @Sql = N'
                UPDATE ' + @QualifiedTable + N'
                SET ResponseBody = @ResponseBody,
                    StatusCode   = @StatusCode,
                    DurationMs   = @DurationMs,
                    UpdatedBy    = @UpdatedBy,
                    UpdatedOn    = SYSUTCDATETIME()
                WHERE Id = @Id;';

            EXEC sp_executesql @Sql,
                N'@ResponseBody NVARCHAR(MAX), @StatusCode INT, @DurationMs BIGINT, @UpdatedBy BIGINT, @Id BIGINT',
                @ResponseBody, @StatusCode, @DurationMs, @UpdatedBy, @Id;

            SET @OutId = @Id;
        END
        ELSE
        BEGIN
            SET @ErrorCode = -1;
            SET @ErrorMessage = N'Unknown @Action: ' + @Action;
        END
    END TRY
    BEGIN CATCH
        SET @ErrorCode = ERROR_NUMBER();
        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH
END
GO
