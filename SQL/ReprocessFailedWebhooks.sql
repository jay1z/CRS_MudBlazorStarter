-- Script to reprocess failed Stripe webhooks after fixing metadata reading logic
-- This will mark the failed events as unprocessed so the application can retry them

-- STEP 1: View current failed events
SELECT 
    Id,
    EventId,
    Type,
    ReceivedAt,
    Processed,
    Error,
    CASE 
        WHEN CHARINDEX('"metadata"', RawJson) > 0 
        THEN 'Has Metadata' 
        ELSE 'No Metadata' 
    END as MetadataStatus,
    CASE 
        WHEN CHARINDEX('"deferred_tenant":"true"', RawJson) > 0 
        THEN 'Is Deferred' 
        ELSE 'Not Deferred' 
    END as DeferredStatus
FROM crs.StripeEventLogs
WHERE Processed = 0
ORDER BY ReceivedAt DESC;

-- STEP 2: Extract metadata from most recent failed checkout.session.completed
DECLARE @EventId NVARCHAR(255);
SELECT TOP 1 @EventId = EventId
FROM crs.StripeEventLogs
WHERE Type = 'checkout.session.completed' AND Processed = 0
ORDER BY ReceivedAt DESC;

IF @EventId IS NOT NULL
BEGIN
    PRINT 'Most recent failed checkout session: ' + @EventId;
    
    SELECT 
        EventId,
        Type,
        ReceivedAt,
        -- Extract metadata section (limited view)
        SUBSTRING(
            RawJson, 
            CHARINDEX('"metadata"', RawJson), 
            CASE 
                WHEN CHARINDEX('}', RawJson, CHARINDEX('"metadata"', RawJson)) > 0
                THEN CHARINDEX('}', RawJson, CHARINDEX('"metadata"', RawJson)) - CHARINDEX('"metadata"', RawJson) + 1
                ELSE 500
            END
        ) as MetadataSection
    FROM crs.StripeEventLogs
    WHERE EventId = @EventId;
END

-- STEP 3: IMPORTANT - DO NOT RUN THIS AUTOMATICALLY
-- Only run this after deploying the fixed webhook code and verifying logs show proper metadata reading
-- This resets failed events to be reprocessed by your application

/*
-- Mark failed events as unprocessed to trigger reprocessing
-- ONLY FOR checkout.session.completed events that failed
UPDATE crs.StripeEventLogs
SET 
    Processed = 0,
    Error = NULL
WHERE Type IN ('checkout.session.completed', 'customer.subscription.created')
  AND Processed = 0
  AND ReceivedAt > DATEADD(HOUR, -24, GETUTCDATE()); -- Only last 24 hours

PRINT 'Marked events for reprocessing. The webhook endpoint will process them on next invocation.';
*/

-- ALTERNATIVE: Manual tenant creation if webhook reprocessing doesn't work
-- Use this to manually create the tenant from the failed webhook data

/*
DECLARE @RawJson NVARCHAR(MAX);
DECLARE @CompanyName NVARCHAR(500);
DECLARE @Subdomain NVARCHAR(500);
DECLARE @AdminEmail NVARCHAR(500);
DECLARE @StripeCustomerId NVARCHAR(500);
DECLARE @StripeSessionId NVARCHAR(500);

-- Get the most recent failed checkout.session.completed event
SELECT TOP 1 
    @RawJson = RawJson,
    @StripeSessionId = EventId
FROM crs.StripeEventLogs
WHERE Type = 'checkout.session.completed' AND Processed = 0
ORDER BY ReceivedAt DESC;

-- Parse JSON manually (SQL Server 2016+)
-- Note: You'll need to manually extract these values from the RawJson
-- Look for: "metadata":{"company_name":"...","subdomain":"...","admin_email":"..."}

PRINT 'TODO: Extract these values from RawJson:';
PRINT '  company_name';
PRINT '  subdomain';
PRINT '  admin_email';
PRINT '  customer (Stripe Customer ID)';
PRINT '';
PRINT 'Then run the manual tenant creation script provided earlier.';
*/
