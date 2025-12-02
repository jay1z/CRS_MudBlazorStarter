# Duplicate Prevention Implementation

## Overview
This document explains how the system prevents duplicate subdomains and emails during tenant signup, including both frontend validation and backend enforcement.

## Architecture

### 1. Frontend Validation (Real-Time)
**File**: `CRS\Components\Pages\TenantSignUp.razor`

- **Subdomain Validation**: Triggers on blur (when user leaves field)
- **Email Validation**: Triggers on blur
- **API Endpoints**:
  - `GET /api/validation/subdomain/{subdomain}` - Check subdomain availability
  - `GET /api/validation/email?email={email}` - Check email availability

**User Experience**:
- ✅ Green checkmark if available
- ❌ Red error message if taken
- ⏳ Loading spinner while checking
- Instant feedback without form submission

### 2. Backend Validation (API Layer)
**File**: `CRS\Controllers\ValidationController.cs`

**Subdomain Validation**:
```csharp
// Format validation: 3-63 chars, lowercase, numbers, hyphens
^[a-z0-9-]{3,63}$

// Reserved subdomains blocked:
www, api, app, admin, support, help, mail, ftp, 
localhost, staging, dev, test, demo, platform, dashboard

// Database check: Query Tenants table
```

**Email Validation**:
```csharp
// Format validation: Basic email regex
^[^@\s]+@[^@\s]+\.[^@\s]+$

// Database checks:
1. AspNetUsers table (existing accounts)
2. Tenants.PendingOwnerEmail (pending registrations)
```

### 3. Server-Side Enforcement (Pre-Stripe)
**File**: `CRS\Controllers\PublicSignupController.cs`

**Before creating Stripe checkout session**:
1. Validate all required fields
2. Check subdomain format and reserved names
3. ✅ **Check subdomain doesn't exist in Tenants table**
4. ✅ **Check email not in AspNetUsers table**
5. ✅ **Check email not in Tenants.PendingOwnerEmail**
6. Only create Stripe session if all checks pass

**Returns**:
- `409 Conflict` if subdomain/email already exists
- `400 BadRequest` for validation errors
- `200 OK` with checkout URL if successful

### 4. Webhook Duplicate Handling
**File**: `CRS\Controllers\StripeWebhookController.cs` (lines 138-155)

**Idempotency built-in**:
```csharp
// Check if tenant already exists before creating
var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
if (existing == null) {
    // Create new tenant
} else {
    // Update existing tenant (idempotent)
}
```

**Stripe's Built-in Protection**:
- Event IDs are unique and logged in `StripeEventLogs`
- Duplicate webhook events are detected and skipped
- Race conditions are handled by database transaction isolation

## Stripe Considerations

### Do You Need to Configure Anything in Stripe?
**Answer: No additional configuration needed**

Stripe already provides:
1. **Event Idempotency**: Each webhook event has a unique `event_id`
2. **Automatic Retries**: Webhooks retry on failure (up to 3 days)
3. **Event Deduplication**: Same event won't create duplicate records

### Recommended Stripe Best Practices:
1. ✅ Use Test Mode for testing duplicate prevention
2. ✅ Enable webhook signature verification (already done via `WebhookSecret`)
3. ✅ Monitor webhook delivery in Stripe Dashboard → Developers → Webhooks
4. ✅ Set up webhook endpoint: `https://yourdomain.com/api/stripe/webhook`

## Race Condition Protection

### Scenario: Two Users Try Same Subdomain Simultaneously

**Frontend Layer**:
- Not protected (both see "available")
- Both proceed to checkout

**Backend Layer** (PublicSignupController):
- ✅ **First request**: Checks DB, subdomain available → creates Stripe session
- ✅ **Second request**: Checks DB, subdomain now exists → returns 409 Conflict
- Database constraint ensures atomicity

**Webhook Layer**:
- Only first webhook creates tenant
- Second webhook sees existing tenant and updates it (idempotent)
- Database UNIQUE constraint on Subdomain column prevents duplicates

### Database Constraints
**File**: `CRS\Data\ApplicationDbContext.cs`

```csharp
// Unique index on Subdomain
builder.Entity<Tenant>()
    .HasIndex(t => t.Subdomain)
    .IsUnique();
```

This provides **final enforcement** - even if all checks fail, database will reject duplicate.

## Testing Duplicate Prevention

### Test Cases

#### 1. Test Subdomain Duplicate (Frontend)
```
1. Open signup page
2. Enter subdomain: "testcompany"
3. Tab out → See green checkmark
4. Open incognito/another browser
5. Enter same subdomain: "testcompany"
6. Tab out → See red error: "already taken"
```

#### 2. Test Email Duplicate (Frontend)
```
1. Register user: test@example.com
2. Try signup with same email
3. Tab out → See error: "already registered"
```

#### 3. Test Server-Side Enforcement
```bash
# Using curl or Postman
POST /api/signup/start
{
  "companyName": "Test Company",
  "subdomain": "existing-subdomain",  # Already exists
  "adminEmail": "test@example.com",
  "tier": "Pro",
  "interval": "Monthly"
}

# Expected Response:
409 Conflict: "Subdomain already in use"
```

#### 4. Test Race Condition
```
1. Disable frontend validation temporarily
2. Open two browser tabs
3. Submit identical signups simultaneously
4. First one creates Stripe session successfully
5. Second one gets 409 Conflict from server
```

## Error Messages

### User-Friendly Messages
- ✅ Subdomain: "The subdomain 'acme' is already taken. Please choose another."
- ✅ Email: "This email address is already registered. Please use a different email or sign in."
- ✅ Reserved: "The subdomain 'admin' is reserved and cannot be used"
- ✅ Pending: "This email has a pending registration. Please check your email."

### Avoid Security Leaks
- ❌ Don't say: "User with email X exists"
- ✅ Do say: "Email already registered" (doesn't confirm existence)

## Monitoring & Logs

### Key Log Entries
```
// Subdomain check
"Subdomain check: '{Subdomain}' is already taken"
"Subdomain check: '{Subdomain}' is available"

// Signup attempt with duplicate
"Signup attempt with duplicate subdomain: {Subdomain}"
"Signup attempt with existing email: {Email}"

// Successful signup
"Creating Stripe checkout session for subdomain {Subdomain}"
"Stripe checkout session created successfully"
```

### Database Queries for Monitoring
```sql
-- Check for duplicate signup attempts (logs)
SELECT * FROM Logs 
WHERE Message LIKE '%Signup attempt with duplicate%'
ORDER BY Timestamp DESC;

-- Check for failed webhook events
SELECT * FROM crs.StripeEventLogs
WHERE Processed = 0 AND Error IS NOT NULL
ORDER BY ReceivedAt DESC;

-- Check for tenants with same subdomain (should be zero)
SELECT Subdomain, COUNT(*) as Count
FROM crs.Tenants
GROUP BY Subdomain
HAVING COUNT(*) > 1;
```

## Security Considerations

### CSRF Protection
- API endpoints are POST/GET and use [ApiController] which validates anti-forgery tokens for state-changing operations

### Rate Limiting (Recommended Enhancement)
```csharp
// Add to ValidationController to prevent abuse
[RateLimit(PermitLimit = 10, Window = 60)] // 10 requests per minute
```

### Input Sanitization
- All inputs are trimmed and normalized
- Regex validation prevents injection
- Email format validated before database query

## Future Enhancements

1. **Debounced Validation**: Wait 500ms after typing stops before checking API
2. **Suggested Alternatives**: If "acme" is taken, suggest "acme-1", "acme-2"
3. **Domain Verification**: Verify company owns the email domain
4. **Duplicate Detection by Company Name**: Warn if similar company name exists
5. **Admin Dashboard**: View duplicate signup attempts for fraud detection

## Summary

✅ **What's Protected**:
- Duplicate subdomains (frontend + backend + database)
- Duplicate emails (frontend + backend + database)
- Reserved subdomains (backend)
- Race conditions (database constraints)
- Webhook idempotency (Stripe + application logic)

✅ **No Stripe Configuration Needed**:
- Built-in event idempotency handles duplicates
- Webhook signatures prevent tampering
- Existing setup is sufficient

✅ **User Experience**:
- Real-time validation feedback
- Clear error messages
- No form submission required for validation
- Loading states during API calls
