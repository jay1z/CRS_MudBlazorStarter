# Subscription Lifecycle Management - Testing Guide

## 📋 Overview

This guide provides step-by-step instructions for testing the subscription lifecycle management system that handles expired/failed payments with grace periods and data retention.

**Lifecycle States**:
- **Active** → Full access
- **PastDue** (Days 0-7) → Full access + warning banner
- **GracePeriod** (Days 8-30) → Read-only access
- **Suspended** (Days 31-90) → No access, data preserved
- **MarkedForDeletion** (Days 91-365) → Soft delete, final warnings
- **Deleted** (Day 365+) → Permanent deletion

---

## 🚀 Pre-Testing Setup

### 1. Apply Database Migration

```bash
cd CRS
dotnet ef database update
```

**Expected Output**:
```
Build started...
Build succeeded.
Applying migration '20241130_AddTenantLifecycleFields'...
Done.
```

**Verification Query**:
```sql
-- Verify new columns exist
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'crs' 
  AND TABLE_NAME = 'Tenants'
  AND COLUMN_NAME IN (
      'SuspendedAt',
      'GracePeriodEndsAt',
      'DeletionScheduledAt',
      'IsMarkedForDeletion',
      'DeletionReason',
      'ReactivationCount',
      'LastReactivatedAt',
      'LastPaymentFailureAt'
  );
```

**Expected**: Should return 8 rows with the new columns.

---

### 2. Verify Scheduler Configuration

**Check**: Open `Program.cs` and verify these lines exist:

```csharp
// Around line 47
builder.Services.AddScheduler();

// Around line 51-56
app.Services.UseScheduler(scheduler =>
{
    scheduler
        .Schedule<CRS.Jobs.TenantLifecycleJob>()
        .DailyAtHour(2)
        .PreventOverlapping(nameof(CRS.Jobs.TenantLifecycleJob));
});
```

**Verification**: Build the project
```bash
dotnet build
```

**Expected**: No errors, build successful.

---

### 3. Create Test Tenant

**Option A: Via Signup Flow**
1. Navigate to `/tenant/signup`
2. Complete signup with test Stripe card: `4242 4242 4242 4242`
3. Note the Tenant ID from the database

**Option B: Via SQL**
```sql
-- Insert test tenant
INSERT INTO crs.Tenants (
    Name, 
    Subdomain, 
    IsActive, 
    CreatedAt, 
    ProvisioningStatus, 
    SubscriptionStatus,
    Tier,
    StripeCustomerId,
    MaxCommunities,
    MaxSpecialistUsers
)
VALUES (
    'Test Lifecycle Company',
    'testlifecycle',
    1,
    GETUTCDATE(),
    2, -- Active
    2, -- Active
    1, -- Pro
    'cus_test123', -- Replace with real customer ID
    50,
    10
);

-- Get the tenant ID
SELECT Id, Name, Subdomain FROM crs.Tenants WHERE Subdomain = 'testlifecycle';
```

**Note your Test Tenant ID**: _____________

---

## 🧪 Test Scenarios

### Test 1: PastDue State (Payment Failed)

**Objective**: Verify tenant receives warning but keeps full access.

#### Step 1.1: Simulate Payment Failure

```sql
-- Set tenant to PastDue
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 3, -- PastDue
    LastPaymentFailureAt = GETUTCDATE(),
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID; -- Replace with your test tenant ID
```

#### Step 1.2: Verify Webhook Handler

**Test**: Trigger a `invoice.payment_failed` webhook from Stripe dashboard:
1. Go to Stripe Dashboard → Developers → Webhooks
2. Find your webhook endpoint
3. Send test event: `invoice.payment_failed`

**Expected Webhook Logs**:
```
Payment failed for tenant {TenantId} - entering PastDue status
```

#### Step 1.3: Verify Access

**Actions**:
1. Navigate to `https://testlifecycle.yourdomain.com` (or localhost equivalent)
2. Login as tenant user
3. Try to create/edit data

**Expected Results**:
- ✅ Can access all pages
- ✅ Can create/edit/delete data
- ✅ See warning banner: "Your payment failed. Please update your payment method..."
- ✅ Banner shows prominently at top of pages

**Verification Query**:
```sql
SELECT 
    Id,
    Name,
    SubscriptionStatus,
    IsActive,
    LastPaymentFailureAt,
    GracePeriodEndsAt
FROM crs.Tenants
WHERE Id = YOUR_TENANT_ID;
```

**Expected Values**:
- `SubscriptionStatus`: 3 (PastDue)
- `IsActive`: 1 (true)
- `LastPaymentFailureAt`: Recent timestamp
- `GracePeriodEndsAt`: NULL

---

### Test 2: GracePeriod State (Read-Only Access)

**Objective**: Verify tenant can view data but cannot edit.

#### Step 2.1: Transition to Grace Period

**Option A: Manual SQL (Fast)**
```sql
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 7, -- GracePeriod
    LastPaymentFailureAt = DATEADD(DAY, -8, GETUTCDATE()), -- 8 days ago
    GracePeriodEndsAt = DATEADD(DAY, 22, GETUTCDATE()), -- 22 days from now
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID;
```

**Option B: Run Background Job (Realistic)**
```sql
-- Set past due 8 days ago
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 3, -- PastDue
    LastPaymentFailureAt = DATEADD(DAY, -8, GETUTCDATE()),
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID;

-- Manually trigger background job via C# immediate window:
-- var job = new CRS.Jobs.TenantLifecycleJob(dbFactory, logger);
-- await job.ExecuteAsync();
```

Or wait until 2 AM for automatic processing.

#### Step 2.2: Verify Access

**Actions**:
1. Navigate to tenant homepage
2. Try to view existing data (communities, studies, etc.)
3. Try to create new community
4. Try to edit existing community
5. Try to delete data

**Expected Results**:
- ✅ Can view all data (read access)
- ✅ GET requests succeed
- ❌ Cannot create new data
- ❌ Cannot edit existing data
- ❌ Cannot delete data
- ❌ POST/PUT/DELETE requests redirect to `/Account/Billing/GracePeriod`
- ✅ Banner shows: "Your account is in read-only mode. Update payment by {date}..."

**Test URLs**:
- View: `/Communities` → Should work
- Create: Submit form on `/Communities/Create` → Should redirect
- Edit: Submit form on `/Communities/Edit/{id}` → Should redirect

**Verification Query**:
```sql
SELECT 
    Id,
    Name,
    SubscriptionStatus,
    IsActive,
    GracePeriodEndsAt,
    DATEDIFF(DAY, GETUTCDATE(), GracePeriodEndsAt) AS DaysRemaining
FROM crs.Tenants
WHERE Id = YOUR_TENANT_ID;
```

**Expected Values**:
- `SubscriptionStatus`: 7 (GracePeriod)
- `IsActive`: 1 (true - read access)
- `GracePeriodEndsAt`: ~22 days in future
- `DaysRemaining`: ~22

#### Step 2.3: Test Grace Period Page

**Actions**:
1. Navigate to `/Account/Billing/GracePeriod`
2. Review page content

**Expected Display**:
- ⚠️ Warning alert: "Read-Only Mode Active"
- 📅 Countdown: "X days until suspension"
- 📋 List of what user can/cannot do
- 💳 "Update Payment Method" button (prominent)
- ⏱️ Timeline showing what happens after payment update

**Test Button Click**:
- Click "Update Payment Method" → Should redirect to Stripe billing portal

---

### Test 3: Suspended State (No Access)

**Objective**: Verify tenant cannot access application but data is preserved.

#### Step 3.1: Transition to Suspended

**Option A: Manual SQL**
```sql
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 8, -- Suspended
    SuspendedAt = GETUTCDATE(),
    IsActive = 0, -- No access
    GracePeriodEndsAt = NULL,
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID;
```

**Option B: Background Job**
```sql
-- Set grace period expired
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 7, -- GracePeriod
    GracePeriodEndsAt = DATEADD(DAY, -1, GETUTCDATE()), -- Expired yesterday
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID;

-- Run job or wait for 2 AM
```

#### Step 3.2: Verify Access

**Actions**:
1. Navigate to tenant homepage
2. Try to access any tenant page

**Expected Results**:
- ❌ All tenant pages redirect to `/Account/Billing/Reactivate`
- ✅ Reactivation page loads successfully
- ✅ Can still access `/Account/Login` and `/Account/Billing/*` pages

**Test URLs**:
- `/` → Redirects to reactivation
- `/Communities` → Redirects to reactivation
- `/ReserveStudies` → Redirects to reactivation
- `/Account/Billing/Reactivate` → Loads successfully ✅

#### Step 3.3: Test Reactivation Page

**Expected Display**:
- ⚠️ Alert: "Account Suspended"
- 💚 "Your Data is Safe!" section (prominent)
- 📊 Data summary: X communities, Y studies preserved
- 📋 "What Happens When You Reactivate?" section
- 💳 "Update Payment & Reactivate Now" button (large, prominent)
- 🔐 Trust signals and support contact

**Verify Data Preservation**:
```sql
-- Confirm data still exists
SELECT 
    (SELECT COUNT(*) FROM crs.Communities WHERE TenantId = YOUR_TENANT_ID) AS Communities,
    (SELECT COUNT(*) FROM crs.ReserveStudies WHERE TenantId = YOUR_TENANT_ID) AS Studies,
    (SELECT COUNT(*) FROM crs.Contacts WHERE TenantId = YOUR_TENANT_ID) AS Contacts;
```

**Expected**: All counts should match pre-suspension values. **No data loss**.

#### Step 3.4: Test Reactivation Flow

**Actions**:
1. Click "Update Payment & Reactivate Now" button
2. Should redirect to Stripe billing portal
3. Update payment method in Stripe
4. Stripe processes successful payment
5. Webhook triggers reactivation

**Expected Webhook Logs**:
```
Payment succeeded for tenant {TenantId} - account reactivated (reactivation #1)
```

**Verify Reactivation**:
```sql
SELECT 
    Id,
    Name,
    SubscriptionStatus,
    IsActive,
    SuspendedAt,
    ReactivationCount,
    LastReactivatedAt
FROM crs.Tenants
WHERE Id = YOUR_TENANT_ID;
```

**Expected After Reactivation**:
- `SubscriptionStatus`: 2 (Active)
- `IsActive`: 1 (true)
- `SuspendedAt`: NULL
- `ReactivationCount`: 1
- `LastReactivatedAt`: Recent timestamp

---

### Test 4: MarkedForDeletion State

**Objective**: Verify tenant sees deletion warning and requires support contact.

#### Step 4.1: Transition to MarkedForDeletion

```sql
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 9, -- MarkedForDeletion
    IsMarkedForDeletion = 1,
    DeletionReason = 'payment_failed',
    DeletionScheduledAt = DATEADD(DAY, 275, GETUTCDATE()), -- 275 days from now
    SuspendedAt = DATEADD(DAY, -90, GETUTCDATE()), -- Suspended 90 days ago
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TENANT_ID;
```

#### Step 4.2: Verify Reactivation Page Behavior

**Actions**:
1. Navigate to `/Account/Billing/Reactivate`

**Expected Display**:
- 🚨 Red alert: "Scheduled for Deletion"
- 📅 Shows deletion date
- 📊 Data summary still shown
- ℹ️ Info message: "Reactivation requires manual verification by our support team"
- ✉️ "Contact Support to Reactivate" button (instead of self-service)
- ☎️ Support phone number displayed

**Test Button**:
- Click "Contact Support" → Opens email client with pre-filled subject

**Verify Email Subject**:
```
To: support@alxreservecloud.com
Subject: Reactivate Account - testlifecycle
```

---

### Test 5: Background Job Execution

**Objective**: Verify automated lifecycle transitions work correctly.

#### Step 5.1: Setup Test Data

```sql
-- Create multiple test tenants in different states
INSERT INTO crs.Tenants (Name, Subdomain, IsActive, CreatedAt, ProvisioningStatus, SubscriptionStatus, Tier, MaxCommunities, MaxSpecialistUsers, LastPaymentFailureAt)
VALUES 
    ('Test PastDue', 'pastdue1', 1, GETUTCDATE(), 2, 3, 1, 50, 10, DATEADD(DAY, -8, GETUTCDATE())),
    ('Test PastDue', 'pastdue2', 1, GETUTCDATE(), 2, 3, 1, 50, 10, DATEADD(DAY, -10, GETUTCDATE())),
    ('Test Grace', 'grace1', 1, GETUTCDATE(), 2, 7, 1, 50, 10, NULL);

UPDATE crs.Tenants SET GracePeriodEndsAt = DATEADD(DAY, -1, GETUTCDATE()) WHERE Subdomain = 'grace1';

-- Get IDs for verification
SELECT Id, Subdomain, SubscriptionStatus, LastPaymentFailureAt, GracePeriodEndsAt FROM crs.Tenants WHERE Subdomain LIKE '%test%' OR Subdomain LIKE 'pastdue%' OR Subdomain LIKE 'grace%';
```

#### Step 5.2: Run Job Manually

**Option A: Via C# Immediate Window (Visual Studio)**
```csharp
var job = ((IServiceProvider)this).GetRequiredService<CRS.Jobs.TenantLifecycleJob>();
await job.ExecuteAsync();
```

**Option B: Via Controller (Development Only)**
```csharp
// Add temporary endpoint in Program.cs (Development only)
app.MapGet("/dev/lifecycle/run", async (CRS.Jobs.TenantLifecycleJob job) => 
{
    await job.ExecuteAsync();
    return Results.Ok("Job completed");
}).WithDisplayName("Dev: Run Lifecycle Job");

// Navigate to: https://localhost:XXXX/dev/lifecycle/run
```

#### Step 5.3: Verify Transitions

**Check Logs**:
Look for log entries in console or log file:
```
Processing 2 tenants from PastDue to GracePeriod
Tenant 123 (pastdue1) entered GracePeriod. Read-only access until {date}
Transitioned 2 tenants to GracePeriod

Processing 1 tenants from GracePeriod to Suspended
Tenant 456 (grace1) suspended. Data will be retained for 90 days.
Suspended 1 tenants
```

**Verify Database Changes**:
```sql
SELECT 
    Id,
    Subdomain,
    SubscriptionStatus,
    GracePeriodEndsAt,
    SuspendedAt
FROM crs.Tenants
WHERE Subdomain IN ('pastdue1', 'pastdue2', 'grace1');
```

**Expected Results**:
- `pastdue1` and `pastdue2`: `SubscriptionStatus` = 7 (GracePeriod), `GracePeriodEndsAt` set
- `grace1`: `SubscriptionStatus` = 8 (Suspended), `SuspendedAt` set

#### Step 5.4: Get Statistics

**Run Statistics Query**:
```csharp
// Via immediate window
var job = ((IServiceProvider)this).GetRequiredService<CRS.Jobs.TenantLifecycleJob>();
var stats = await job.GetStatisticsAsync();
Console.WriteLine($"Active: {stats.ActiveTenants}, PastDue: {stats.PastDueTenants}, GracePeriod: {stats.GracePeriodTenants}, Suspended: {stats.SuspendedTenants}");
```

**Expected Output**:
```
Active: X, PastDue: X, GracePeriod: X, Suspended: X, MarkedForDeletion: X, Total: X
```

---

### Test 6: Permanent Deletion

**⚠️ CAUTION**: This test permanently deletes data. Use only with test tenants.

#### Step 6.1: Setup for Deletion

```sql
UPDATE crs.Tenants
SET 
    SubscriptionStatus = 9, -- MarkedForDeletion
    IsMarkedForDeletion = 1,
    DeletionScheduledAt = DATEADD(DAY, -1, GETUTCDATE()), -- Scheduled for yesterday
    UpdatedAt = GETUTCDATE()
WHERE Id = YOUR_TEST_TENANT_ID;
```

#### Step 6.2: Run Job

```csharp
var job = ((IServiceProvider)this).GetRequiredService<CRS.Jobs.TenantLifecycleJob>();
await job.ExecuteAsync();
```

#### Step 6.3: Verify Deletion

**Check Logs**:
```
Processing 1 tenants for permanent deletion
Permanently deleting tenant {TenantId} ({Name}) - IRREVERSIBLE
Tenant {TenantId} data deleted successfully
Permanently deleted 1 tenants
```

**Verify Data Deletion**:
```sql
-- Check tenant record (soft delete)
SELECT 
    Id,
    Name,
    Subdomain,
    DateDeleted,
    SubscriptionStatus,
    IsActive
FROM crs.Tenants
WHERE Id = YOUR_TEST_TENANT_ID;

-- Check related data deleted
SELECT 
    (SELECT COUNT(*) FROM crs.Communities WHERE TenantId = YOUR_TEST_TENANT_ID) AS Communities,
    (SELECT COUNT(*) FROM crs.ReserveStudies WHERE TenantId = YOUR_TEST_TENANT_ID) AS Studies,
    (SELECT COUNT(*) FROM crs.Contacts WHERE TenantId = YOUR_TEST_TENANT_ID) AS Contacts;
```

**Expected Results**:
- Tenant: `Name` prefixed with `[DELETED]`, `DateDeleted` set, `Subdomain` changed to `deleted-{id}-{guid}`
- Related data: All counts should be 0

---

## 📊 Monitoring Queries

### Daily Health Check

```sql
-- Lifecycle state distribution
SELECT 
    SubscriptionStatus,
    COUNT(*) AS Count,
    CASE SubscriptionStatus
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'Incomplete'
        WHEN 2 THEN 'Active'
        WHEN 3 THEN 'PastDue'
        WHEN 4 THEN 'Canceled'
        WHEN 5 THEN 'Unpaid'
        WHEN 6 THEN 'Trialing'
        WHEN 7 THEN 'GracePeriod'
        WHEN 8 THEN 'Suspended'
        WHEN 9 THEN 'MarkedForDeletion'
        ELSE 'Unknown'
    END AS StatusName
FROM crs.Tenants
WHERE DateDeleted IS NULL
GROUP BY SubscriptionStatus
ORDER BY SubscriptionStatus;
```

### Tenants At Risk

```sql
-- Tenants in grace period or suspended
SELECT 
    Id,
    Name,
    Subdomain,
    SubscriptionStatus,
    CASE SubscriptionStatus
        WHEN 3 THEN 'PastDue - ' + CAST(DATEDIFF(DAY, LastPaymentFailureAt, GETUTCDATE()) AS VARCHAR) + ' days'
        WHEN 7 THEN 'GracePeriod - ' + CAST(DATEDIFF(DAY, GETUTCDATE(), GracePeriodEndsAt) AS VARCHAR) + ' days left'
        WHEN 8 THEN 'Suspended - ' + CAST(DATEDIFF(DAY, SuspendedAt, GETUTCDATE()) AS VARCHAR) + ' days'
        WHEN 9 THEN 'Deletion in ' + CAST(DATEDIFF(DAY, GETUTCDATE(), DeletionScheduledAt) AS VARCHAR) + ' days'
    END AS Status,
    LastPaymentFailureAt,
    GracePeriodEndsAt,
    SuspendedAt,
    DeletionScheduledAt
FROM crs.Tenants
WHERE SubscriptionStatus IN (3, 7, 8, 9)
  AND DateDeleted IS NULL
ORDER BY SubscriptionStatus DESC, Id;
```

### Reactivation Opportunities

```sql
-- Recently suspended tenants (good candidates for win-back campaigns)
SELECT 
    Id,
    Name,
    Subdomain,
    PendingOwnerEmail,
    DATEDIFF(DAY, SuspendedAt, GETUTCDATE()) AS DaysSuspended,
    ReactivationCount AS PreviousReactivations
FROM crs.Tenants
WHERE SubscriptionStatus = 8 -- Suspended
  AND SuspendedAt > DATEADD(DAY, -30, GETUTCDATE()) -- Last 30 days
  AND DateDeleted IS NULL
ORDER BY DaysSuspended;
```

### Job Execution History

```sql
-- Check Coravel job logs (if you have logging table)
-- Adjust based on your logging setup
```

---

## ✅ Test Checklist

### Pre-Deployment Checklist
- [ ] Database migration applied successfully
- [ ] Build completed without errors
- [ ] Coravel scheduler configured
- [ ] Test tenant created

### Functional Tests
- [ ] **Test 1**: PastDue state - Full access + warning banner
- [ ] **Test 2**: GracePeriod state - Read-only access enforced
- [ ] **Test 3**: Suspended state - No access, data preserved
- [ ] **Test 4**: MarkedForDeletion - Support contact required
- [ ] **Test 5**: Background job - Automated transitions work
- [ ] **Test 6**: Permanent deletion - Data removed correctly

### Access Control Tests
- [ ] GET requests work in GracePeriod
- [ ] POST/PUT/DELETE blocked in GracePeriod
- [ ] All requests blocked in Suspended state
- [ ] Billing pages accessible in all states
- [ ] Middleware redirects correct for each state

### Reactivation Tests
- [ ] Stripe billing portal link works
- [ ] Payment success triggers reactivation
- [ ] `ReactivationCount` increments correctly
- [ ] Full access restored after payment
- [ ] Warning banners removed after reactivation

### Background Job Tests
- [ ] Scheduled execution at 2 AM works
- [ ] Manual execution completes successfully
- [ ] State transitions happen correctly
- [ ] Logs show proper information
- [ ] Statistics method returns accurate counts

### Edge Cases
- [ ] Multiple tenants transition simultaneously
- [ ] Reactivation during grace period works
- [ ] Reactivation during suspension works
- [ ] Job handles database errors gracefully
- [ ] No race conditions during transitions

---

## 🐛 Common Issues & Solutions

### Issue 1: Migration Fails

**Error**: `Column names in each table must be unique...`

**Solution**:
```bash
# Remove migration
dotnet ef migrations remove

# Rebuild project
dotnet clean
dotnet build

# Re-add migration
dotnet ef migrations add AddTenantLifecycleFields
dotnet ef database update
```

---

### Issue 2: Background Job Not Running

**Check**:
```csharp
// Verify job is registered in Program.cs
builder.Services.AddScheduler();
builder.Services.AddScoped<CRS.Jobs.TenantLifecycleJob>();
```

**Test Manually**:
```csharp
// Via immediate window or dev endpoint
var job = sp.GetRequiredService<CRS.Jobs.TenantLifecycleJob>();
await job.ExecuteAsync();
```

---

### Issue 3: Access Not Blocked in GracePeriod

**Check Middleware Order in Program.cs**:
```csharp
app.UseMiddleware<TenantResolverMiddleware>(); // Must be before license gate
app.UseLicenseGate(); // Must be before authentication
app.UseAuthentication();
app.UseAuthorization();
```

**Verify Middleware Registration**:
```csharp
// In LicenseGateMiddleware.cs constructor
public LicenseGateMiddleware(RequestDelegate next, IDbContextFactory<ApplicationDbContext> dbFactory)
```

---

### Issue 4: Reactivation Button Not Working

**Check Stripe Configuration**:
```json
// appsettings.json
{
  "Stripe": {
    "SecretKey": "sk_test_...",  // Must be valid
    "PublishableKey": "pk_test_..."
  },
  "Billing": {
    "Urls": {
      "PortalReturnUrl": "https://yourdomain.com/Account/Billing"
    }
  }
}
```

**Test Billing Service**:
```csharp
var billingService = sp.GetRequiredService<IBillingService>();
var portalUrl = await billingService.CreateBillingPortalSessionAsync(tenantId);
Console.WriteLine(portalUrl); // Should return Stripe portal URL
```

---

### Issue 5: Webhook Not Triggering Reactivation

**Check Webhook Endpoint**:
1. Stripe Dashboard → Developers → Webhooks
2. Verify endpoint: `https://yourdomain.com/api/stripe/webhook`
3. Verify events: `invoice.payment_succeeded` is enabled

**Check Webhook Logs**:
```sql
SELECT TOP 10
    EventId,
    Type,
    Processed,
    Error,
    ReceivedAt
FROM crs.StripeEventLogs
ORDER BY ReceivedAt DESC;
```

**Manual Webhook Test**:
```bash
# Use Stripe CLI
stripe trigger invoice.payment_succeeded
```

---

## 📈 Success Metrics

After implementation, monitor these KPIs:

### Revenue Recovery
- **Target**: 15-20% of PastDue accounts reactivate in GracePeriod
- **Target**: 10-15% of Suspended accounts reactivate before deletion
- **Target**: Overall 30-40% recovery rate

**Measurement Query**:
```sql
SELECT 
    COUNT(CASE WHEN ReactivationCount > 0 THEN 1 END) AS Reactivated,
    COUNT(*) AS Total,
    CAST(COUNT(CASE WHEN ReactivationCount > 0 THEN 1 END) AS FLOAT) / COUNT(*) * 100 AS RecoveryRate
FROM crs.Tenants
WHERE SubscriptionCanceledAt > DATEADD(DAY, -90, GETUTCDATE());
```

### Average Time to Reactivate
- **Target**: < 7 days from suspension

```sql
SELECT 
    AVG(DATEDIFF(DAY, SuspendedAt, LastReactivatedAt)) AS AvgDaysToReactivate
FROM crs.Tenants
WHERE LastReactivatedAt IS NOT NULL
  AND SuspendedAt IS NOT NULL;
```

### Churn Reduction
- **Before**: Immediate termination on payment failure
- **After**: 30-day grace period + 90-day data retention
- **Target**: 25-35% reduction in final churn

---

## 📞 Support

### For Testing Issues
- Check logs: Application logs, Coravel logs, Stripe webhook logs
- Review database state: Run monitoring queries
- Verify configuration: `appsettings.json`, middleware order

### For Production Issues
- Contact: support@alxreservecloud.com
- Include: Tenant ID, timestamp, error messages, relevant logs

---

## 🎯 Next Steps After Testing

1. **Email Integration**
   - Implement email notifications in `TenantLifecycleJob.cs`
   - Create email templates for each lifecycle stage
   - Test email delivery

2. **Admin Dashboard**
   - Create admin page showing lifecycle statistics
   - Add manual reactivation approval workflow
   - Display at-risk tenants

3. **Customer Communication**
   - Document lifecycle policy for customers
   - Update terms of service
   - Create help articles

4. **Monitoring & Alerts**
   - Set up alerts for high numbers in PastDue/Suspended
   - Monitor daily job execution
   - Track reactivation rates

5. **Business Rules Tuning**
   - Adjust grace period length based on recovery data
   - Fine-tune email timing based on response rates
   - Configure automatic vs. manual deletion approval

---

**Last Updated**: 2024-11-30  
**Version**: 1.0  
**Status**: Ready for Testing

