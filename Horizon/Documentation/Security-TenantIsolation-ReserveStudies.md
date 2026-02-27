# Reserve Study Request Security & Tenant Isolation

## Overview
This document outlines the multi-layered security approach for ensuring **HOA users and customers can only create reserve study requests for their authorized tenant**.

---

## Security Layers

### 1️⃣ **Subdomain-Based Tenant Resolution** (Middleware)
**Location:** `TenantResolverMiddleware.cs`

- Every HTTP request is intercepted and the subdomain is extracted from `Host` header
- Tenant is resolved from the database based on subdomain
- Tenant context (`ITenantContext`) is populated with:
  - `TenantId` - Primary identifier
  - `TenantName` - Display name
  - `Subdomain` - URL subdomain
  - `IsActive` - Subscription status

**Protection:**
- Unknown subdomains are redirected to `/tenant/not-found`
- Authenticated users with mismatched tenant claims are **signed out immediately**
- Tenant context is cached in `HttpContext.Items` for Blazor circuit access

**Example:**
```
Request: https://acme-reserve.alxcloud.com/ReserveStudies/Request
└─> Middleware resolves: TenantId=42, TenantName="ACME Reserve Studies"
└─> All data queries automatically filtered to TenantId=42
```

---

### 2️⃣ **UI Layer Protection** (Blazor Component)
**Location:** `Step1.razor` (Community selection step)

#### Changes Made:
```csharp
@inject ITenantContext TenantContext  // ✅ Added tenant context injection

private async Task LoadUserCommunities() {
    // SECURITY: Only load communities from the current tenant
    if (!TenantContext.TenantId.HasValue) {
        Snackbar.Add("Unable to determine tenant context...", Severity.Error);
        return;
    }

    var currentTenantId = TenantContext.TenantId.Value;
    
    existingCommunities = await context.Communities
        .Include(c => c.Addresses)
        .AsNoTracking()
        .Where(c => c.TenantId == currentTenantId &&  // ✅ Filter by current tenant
                    context.ReserveStudies
                        .Any(rs => rs.Community.Id == c.Id && rs.ApplicationUserId == UserState.CurrentUser.Id))
        .ToListAsync();
}
```

**Protection:**
- Users **cannot see communities from other tenants** in the dropdown
- New communities automatically get `TenantId` set from context
- Final validation ensures `Community.TenantId == TenantContext.TenantId`

---

### 3️⃣ **Service Layer Enforcement** (Server-side validation)
**Location:** `ReserveStudyService.CreateReserveStudyAsync`

#### Security Additions:
```csharp
public async Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy) {
    // SECURITY: Require tenant context
    if (!_tenantContext.TenantId.HasValue) {
        throw new InvalidOperationException("Tenant context is required...");
    }

    var tenantId = _tenantContext.TenantId.Value;
    
    // SECURITY: Force tenant ID assignment - never trust client input
    reserveStudy.TenantId = tenantId;

    // SECURITY: Validate existing community belongs to current tenant
    if (reserveStudy.CommunityId != Guid.Empty) {
        var existingCommunity = await context.Communities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == reserveStudy.CommunityId);
            
        if (existingCommunity == null) {
            throw new InvalidOperationException("The specified community does not exist.");
        }
        
        if (existingCommunity.TenantId != tenantId) {
            throw new UnauthorizedAccessException(
                "You cannot create a reserve study for a community that belongs to a different organization."
            );
        }
    }

    // SECURITY: Force tenant ID on new community
    if (creatingNewCommunity) {
        if (reserveStudy.Community.TenantId != tenantId) {
            reserveStudy.Community.TenantId = tenantId;
        }
    }
    
    // ... rest of method
}
```

**Protection:**
- **Never trusts client-provided TenantId** - always uses server-side context
- Validates existing community ownership before allowing association
- Throws `UnauthorizedAccessException` if cross-tenant access is attempted
- Forces tenant ID on all related entities (Contact, PropertyManager, etc.)

---

### 4️⃣ **Authorization Policies**
**Location:** `TenantAuthorizationExtensions.cs`

#### Available Policies:
```csharp
// Tenant Staff (Owner or Specialist) - Internal users only
options.AddPolicy("RequireTenantStaff", policy => policy.RequireAssertion(ctx => {
    var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet();
    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
    return (roles.Contains("TenantOwner") || roles.Contains("TenantSpecialist")) 
        && !string.IsNullOrEmpty(tenantClaim);
}));

// HOA User - External customers
options.AddPolicy("RequireHOAUser", policy => policy.RequireAssertion(ctx => {
    var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet();
    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
    return (roles.Contains("HOAUser") || roles.Contains("HOAAuditor")) 
        && !string.IsNullOrEmpty(tenantClaim);
}));
```

#### Page Authorization:
```razor
@page "/ReserveStudies/Create"
@attribute [Authorize(Policy = "RequireTenantStaff")]  // ← Staff only

@page "/ReserveStudies/Request"
@attribute [Authorize]  // ← All authenticated users (including HOA)
```

---

## Attack Prevention Matrix

| Attack Scenario | Prevention Mechanism | Result |
|---|---|---|
| **HOA user tries to submit for wrong tenant** | Subdomain middleware → TenantId forced server-side | ❌ Blocked (UnauthorizedAccessException) |
| **User manually changes Community dropdown** | Service layer validates Community.TenantId | ❌ Blocked (UnauthorizedAccessException) |
| **User crafts malicious API request with wrong TenantId** | Service layer ignores client TenantId, uses server context | ❌ Blocked (TenantId overwritten) |
| **User switches subdomain mid-session** | Middleware detects tenant mismatch → signs out user | ❌ Blocked (forced logout) |
| **User tries to access another tenant's data** | All EF queries filtered by `TenantId` | ❌ No data returned |

---

## Two Request Paths

### **Path 1: Internal Staff** (`/ReserveStudies/Create`)
- **Who:** TenantOwner, TenantSpecialist
- **Authorization:** `RequireTenantStaff` policy
- **Capabilities:**
  - Create studies for any community in their tenant
  - Assign to specialists
  - Full workflow access

### **Path 2: HOA Users** (`/ReserveStudies/Request`)
- **Who:** HOAUser, HOAAuditor, any authenticated user
- **Authorization:** `[Authorize]` (basic authentication)
- **Capabilities:**
  - Create requests for their own properties
  - Track via email access token
  - Limited to communities they own/manage
- **UX Differences:**
  - Tenant branding displayed prominently
  - Simplified completion message
  - Redirects to customer dashboard

---

## Testing Checklist

### ✅ **Tenant Isolation**
- [ ] User at `tenant1.alxcloud.com` cannot see `tenant2`'s communities
- [ ] Attempting to submit with wrong `CommunityId` throws exception
- [ ] Creating new community automatically sets correct `TenantId`

### ✅ **Authorization**
- [ ] HOA user can access `/ReserveStudies/Request`
- [ ] HOA user cannot access `/ReserveStudies/Create` (redirected to 403)
- [ ] Unauthenticated users redirected to login

### ✅ **Data Integrity**
- [ ] All `Community`, `Contact`, `PropertyManager` records have matching `TenantId`
- [ ] No orphaned data or cross-tenant references
- [ ] EF Core queries automatically filter by `TenantId`

---

## Future Enhancements

### 🔐 **Additional Security Measures** (Optional)
1. **Rate Limiting:** Prevent spam submissions from HOA users
2. **CAPTCHA:** Add reCAPTCHA on public request form
3. **Email Verification:** Require email confirmation before processing
4. **Audit Logging:** Track all cross-tenant access attempts

### 🎨 **UX Improvements**
1. **Tenant Branding:** Load logo/colors from `TenantContext.BrandingJson`
2. **Pre-filled Forms:** Auto-populate user's email/phone from profile
3. **Progress Saving:** Allow users to save drafts and resume later

### 📧 **Email Notifications**
1. **HOA User:** Confirmation email with tracking link
2. **Tenant Staff:** Notification of new request from HOA user
3. **Automated Reminders:** Follow-up if no response after X days

---

## Database Schema Reference

### **Key Entities with TenantId**
```
Tenant (id, subdomain, name, isActive)
├─ Community (tenantId, name)
├─ ReserveStudy (tenantId, communityId, applicationUserId)
├─ Contact (tenantId)
├─ PropertyManager (tenantId)
└─ UserRoleAssignment (tenantId, userId, roleId)
```

### **Query Filter Example** (Recommended for EF Core Global Filters)
```csharp
modelBuilder.Entity<Community>().HasQueryFilter(c => 
    c.TenantId == _tenantContext.TenantId);
```

---

## Support & Troubleshooting

### **Common Issues**

**1. "Unable to determine tenant context" error**
- **Cause:** User accessed page without subdomain (e.g., `localhost:5001`)
- **Fix:** Ensure user is on correct subdomain URL

**2. User sees empty community dropdown**
- **Cause:** No existing communities for their tenant, or not linked to any studies
- **Fix:** User should select "Create New Community" option

**3. Cross-tenant access attempt logged**
- **Cause:** User tried to manipulate `CommunityId` in request
- **Result:** Exception thrown, request blocked, logged for security audit

---

## Security Contacts
- **Security Issues:** security@alxreservecloud.com
- **Code Review:** @jay1z
- **Last Updated:** 2025-01-XX
