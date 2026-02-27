# Pre-Launch Feature Gap Analysis

## Document Information
- **Date:** January 2025
- **Purpose:** Comprehensive review of CRS (Community Reserve Study) SaaS application for go-live readiness
- **Status:** Analysis Complete

---

## Executive Summary

### ✅ **GO-LIVE READY** 
The application has a mature, production-ready feature set. The following analysis identifies optional enhancements, not blockers.

### Core Strengths
- Complete 32-stage reserve study workflow
- Solid multi-tenant architecture with security isolation
- Full Stripe billing integration with Connect
- Comprehensive invoicing with payment tracking
- Azure Communication Services email integration
- PDF/Excel report generation
- Demo environment for onboarding

---

## 🟢 EXISTING FEATURES (Fully Implemented)

### 1. Reserve Study Core Features ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| 32-Stage Workflow | ✅ Complete | `StudyStatus` enum in `Models/Workflow/StudyStatus.cs` |
| Financial Info Collection | ✅ Complete | `FinancialInfo.cs` with 20+ financial fields |
| Building Elements | ✅ Complete | `ReserveStudyBuildingElement.cs` |
| Common Elements | ✅ Complete | `ReserveStudyCommonElement.cs` |
| Additional Elements | ✅ Complete | `ReserveStudyAdditionalElement.cs` |
| Site Visit Scheduling | ✅ Complete | `CalendarEvent.cs`, `SiteVisitPhoto.cs` |
| Reserve Calculator | ✅ Complete | `Core/ReserveStudy/Services/ReserveStudyCalculator.cs` |
| Multiple Scenarios | ✅ Complete | `ReserveStudyScenario.cs` |
| Narrative Generation | ✅ Complete | Full narrative template system |
| PDF Reports | ✅ Complete | PuppeteerSharp integration |
| Excel Export | ✅ Complete | `ReserveStudyExcelService.cs` |

### 2. Multi-Tenant SaaS Infrastructure ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Tenant Isolation | ✅ Complete | EF query filters, middleware |
| Subdomain Routing | ✅ Complete | `TenantResolverMiddleware.cs` |
| Subscription Tiers | ✅ Complete | Startup, Pro, Enterprise |
| Stripe Billing | ✅ Complete | `BillingService.cs` |
| Stripe Connect | ✅ Complete | Tenant payment processing |
| Tenant Lifecycle | ✅ Complete | Grace period, suspension, deletion |
| Custom Branding | ✅ Complete | Themes, logos, colors |
| Role-Based Access | ✅ Complete | TenantOwner, Staff, Specialist, HOAUser |

### 3. Billing & Invoicing ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Invoice Management | ✅ Complete | `Invoice.cs`, `InvoiceService.cs` |
| Payment Tracking | ✅ Complete | `PaymentRecord.cs` |
| Payment Schedules | ✅ Complete | 50/50, thirds, quarters, custom |
| Late Interest | ✅ Complete | `LateInterestInvocable.cs` |
| Credit Memos | ✅ Complete | `CreditMemo.cs`, `CreditMemoService.cs` |
| Early Payment Discounts | ✅ Complete | Configurable per proposal |
| Invoice Reminders | ✅ Complete | `InvoiceReminderInvocable.cs` |
| Invoice PDF | ✅ Complete | `InvoicePdfService.cs` |

### 4. Proposal Management ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Proposal Creation | ✅ Complete | `Proposal.cs` |
| Proposal Amendments | ✅ Complete | Multiple proposals per study |
| Click-Wrap Agreements | ✅ Complete | `ProposalAcceptance.cs` |
| Terms Templates | ✅ Complete | `AcceptanceTermsTemplate.cs` |
| Proposal PDF | ✅ Complete | `ProposalPdfService.cs` |
| Scope Comparison | ✅ Complete | `ScopeComparison.cs` |

### 5. Communication ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Azure Email | ✅ Complete | `AzureCommunicationMailer.cs` |
| Email Templates | ✅ Complete | Razor templates |
| Email Logging | ✅ Complete | `EmailLog.cs`, `EmailLogService.cs` |
| Newsletter Campaigns | ✅ Complete | `NewsletterCampaign.cs` |
| In-App Messaging | ✅ Complete | `Message.cs`, `MessageService.cs` |
| Notifications | ✅ Complete | `Notification.cs` |

### 6. Documents & Reports ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Document Storage | ✅ Complete | Azure Blob Storage |
| Generated Reports | ✅ Complete | `GeneratedReport.cs` |
| Report Versioning | ✅ Complete | `SupersedesReportId` field |
| Report Delivery | ✅ Complete | Email to clients |
| ZIP Downloads | ✅ Complete | `ReportZipService.cs` |

### 7. Workflow & Automation ✅

| Feature | Status | Implementation |
|---------|--------|----------------|
| Event System | ✅ Complete | Coravel events |
| Background Jobs | ✅ Complete | Coravel scheduler |
| Auto-Archive | ✅ Complete | `AutoArchiveInvocable.cs` |
| Tenant Lifecycle | ✅ Complete | `TenantLifecycleJob.cs` |
| Audit Logging | ✅ Complete | `AuditLog.cs` |

---

## 🟡 POTENTIAL ENHANCEMENTS (Optional)

### Priority Levels
- 🔴 **HIGH** - Consider before launch or shortly after
- 🟡 **MEDIUM** - Add in first major update
- 🟢 **LOW** - Future roadmap item

---

### 1. Reserve Study Core Enhancements

#### 1.1 Add PercentFunded Fields (🔴 HIGH)

**Why:** This is THE key metric shown on all reserve study reports. Currently calculated but not persisted.

**Add to `ReserveStudy.cs`:**
```csharp
/// <summary>
/// Percent funded = (Current Balance / Fully Funded Balance) × 100
/// Industry standard metric for reserve health.
/// </summary>
[Precision(5, 2)]
public decimal? PercentFunded { get; set; }

/// <summary>
/// The funding status level based on percent funded thresholds.
/// </summary>
public FundingStatusLevel? FundingStatus { get; set; }

/// <summary>
/// Calculated recommended monthly contribution per unit.
/// </summary>
[Precision(18, 2)]
public decimal? RecommendedMonthlyContribution { get; set; }

/// <summary>
/// The fully funded balance (total of all component reserves).
/// </summary>
[Precision(18, 2)]
public decimal? FullyFundedBalance { get; set; }

/// <summary>
/// Risk indicator when underfunding may require special assessment.
/// </summary>
public bool? SpecialAssessmentRisk { get; set; }

/// <summary>
/// Date when next update is recommended (typically 3-5 years).
/// </summary>
public DateTime? NextScheduledUpdate { get; set; }
```

**Add new enum:**
```csharp
public enum FundingStatusLevel
{
    /// <summary>70%+ funded</summary>
    Strong = 0,
    
    /// <summary>50-69% funded</summary>
    Fair = 1,
    
    /// <summary>30-49% funded</summary>
    Weak = 2,
    
    /// <summary>Under 30% funded</summary>
    Critical = 3
}
```

**Database Migration Required:** Yes - add columns to ReserveStudies table

---

#### 1.2 Historical Tracking Tables (🟡 MEDIUM)

**Why:** Allows year-over-year comparison of reserve health.

**New Models to Add:**

```csharp
// CRS/Models/ReserveStudySnapshot.cs
public class ReserveStudySnapshot : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }
    
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }
    
    public DateTime SnapshotDate { get; set; }
    
    [Precision(5, 2)]
    public decimal PercentFunded { get; set; }
    
    [Precision(18, 2)]
    public decimal RecommendedContribution { get; set; }
    
    [Precision(18, 2)]
    public decimal ActualBalance { get; set; }
    
    [Precision(18, 2)]
    public decimal FullyFundedBalance { get; set; }
    
    /// <summary>JSON blob with full calculation snapshot</summary>
    public string? CalculationDataJson { get; set; }
}
```

```csharp
// CRS/Models/ContributionHistory.cs
public class ContributionHistory : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }
    
    [ForeignKey(nameof(Community))]
    public Guid CommunityId { get; set; }
    public Community? Community { get; set; }
    
    public int FiscalYear { get; set; }
    
    [Precision(18, 2)]
    public decimal BudgetedAmount { get; set; }
    
    [Precision(18, 2)]
    public decimal? ActualAmount { get; set; }
    
    public DateTime RecordedAt { get; set; }
    
    public string? Notes { get; set; }
}
```

---

#### 1.3 Study Clone/Template Feature (🟡 MEDIUM)

**Why:** Most reserve studies are updates to previous years. Cloning saves significant data entry.

**Service Method to Add:**
```csharp
// In IReserveStudyService.cs
Task<ReserveStudy> CloneStudyAsync(Guid sourceStudyId, string newName, DateTime newStudyDate);
```

---

### 2. SaaS Platform Enhancements

#### 2.1 Free Trial Flow (🔴 HIGH)

**Current State:** `SubscriptionStatus.Trialing` exists but no trial signup flow.

**Implementation Needed:**
1. Add trial days configuration to `Tenant.cs`:
```csharp
public int? TrialDaysRemaining { get; set; }
public DateTime? TrialStartedAt { get; set; }
public DateTime? TrialExpiresAt { get; set; }
public bool HasUsedTrial { get; set; } = false;
```

2. Modify signup flow in `TenantSignUp.razor` to offer trial option
3. Add trial expiration warning emails
4. Add conversion prompts in UI

---

#### 2.2 Tenant Analytics Dashboard (🔴 HIGH)

**Why:** Tenant owners need visibility into their business performance.

**New Page:** `Components/Pages/Admin/Analytics.razor`

**Metrics to Display:**
- Studies completed (this month/quarter/year)
- Revenue collected vs. outstanding
- Average study completion time
- Client retention rate
- Most common elements
- Specialist productivity

**Implementation:**
```csharp
// CRS/Services/Interfaces/IAnalyticsService.cs
public interface IAnalyticsService
{
    Task<TenantAnalytics> GetTenantAnalyticsAsync(int tenantId, DateRange range);
    Task<List<StudyMetric>> GetStudyMetricsAsync(int tenantId, DateRange range);
    Task<RevenueMetrics> GetRevenueMetricsAsync(int tenantId, DateRange range);
}
```

---

#### 2.3 Data Export UI (🔴 HIGH - GDPR Compliance)

**Why:** GDPR and other regulations require ability to export user data.

**Current State:** `TenantArchiveService.cs` exists but no user-facing UI.

**Implementation Needed:**
1. Add page `Components/Pages/Settings/DataExport.razor`
2. Allow export of:
   - All studies and components
   - All contacts and communities
   - All invoices and payments
   - All documents (with download links)
3. Export formats: JSON, CSV, ZIP bundle

---

#### 2.4 Audit Log Viewer (🟡 MEDIUM)

**Why:** Audit logs exist but no UI to view them.

**Current State:** `AuditLog.cs` captures all changes, `AuditLogArchiveService.cs` archives old records.

**Implementation Needed:**
1. Add page `Components/Pages/Admin/AuditLogs.razor`
2. Filter by: User, Date range, Table, Action type
3. Search functionality
4. Export to CSV

---

### 3. Operational Enhancements

#### 3.1 Bulk Component Import (🔴 HIGH)

**Current State:** Excel import exists in calculator but limited scope.

**Enhancement:**
1. Standardized Excel template for component import
2. Validation with error reporting
3. Preview before commit
4. Import history tracking

---

#### 3.2 Community Deduplication (🟡 MEDIUM)

**Why:** Prevent creating duplicate communities.

**Implementation:**
```csharp
// Add to ICommunityService
Task<List<Community>> FindPotentialDuplicatesAsync(string name, string address, string city);
Task<bool> MergeCommunitiesAsync(Guid sourceId, Guid targetId);
```

**UI:** Show warning when creating community that matches existing.

---

#### 3.3 Webhook System (🟡 MEDIUM)

**Why:** Allow tenants to integrate with their own systems.

**New Models:**
```csharp
// CRS/Models/WebhookSubscription.cs
public class WebhookSubscription : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public WebhookEventType EventType { get; set; }
    public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailureCount { get; set; } = 0;
    public DateTime? LastTriggeredAt { get; set; }
}

public enum WebhookEventType
{
    StudyCreated,
    StudyCompleted,
    ProposalAccepted,
    InvoicePaid,
    ReportDelivered
}
```

---

### 4. Compliance Enhancements

#### 4.1 State-Specific Compliance Rules (🟡 MEDIUM)

**Why:** Different states have different reserve study requirements (California, Florida, etc.)

**Implementation:**
```csharp
// CRS/Models/StateComplianceRule.cs
public class StateComplianceRule
{
    public int Id { get; set; }
    public string StateCode { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int MinProjectionYears { get; set; }
    public bool RequiresPhysicalInspection { get; set; }
    public int UpdateFrequencyYears { get; set; }
    public string? SpecialRequirements { get; set; }
}
```

---

#### 4.2 Digital Signature Integration (🟢 LOW)

**Why:** Legal compliance for proposal acceptance.

**Options:**
- DocuSign integration
- Adobe Sign integration
- Custom e-signature with audit trail

---

## 📋 IMPLEMENTATION PRIORITY CHECKLIST

### Before Go-Live (Week 1)
- [x] Add `PercentFunded` and related fields to `ReserveStudy.cs` ✅ DONE
- [x] Create database migration for new fields ✅ DONE
- [x] Update calculator to persist results to ReserveStudy record ✅ DONE
- [x] Add Analytics dashboard ✅ DONE
- [x] Add Data Export page (GDPR compliance) ✅ DONE
- [x] Add Trial fields to Tenant.cs ✅ DONE
- [x] Free trial via Stripe ✅ DONE (using Stripe's built-in trial_period_days)
- [x] Trial expiration emails ✅ DONE (via Stripe webhook + TrialExpirationInvocable backup)
- [x] Add TrialBanner component ✅ DONE

### First Month Post-Launch
- [x] Build audit log viewer UI ✅ DONE (`/admin/auditlogs`)

### Quarter 1 Post-Launch
- [ ] Historical tracking tables
- [ ] Study clone feature
- [ ] Enhanced bulk import

### Future Roadmap
- [ ] Webhook system
- [ ] State compliance rules
- [ ] Digital signatures
- [ ] API documentation
- [ ] Mobile PWA

---

## Database Migration Notes

### New Columns for ReserveStudies Table
```sql
ALTER TABLE [ReserveStudies] ADD [PercentFunded] decimal(5,2) NULL;
ALTER TABLE [ReserveStudies] ADD [FundingStatus] int NULL;
ALTER TABLE [ReserveStudies] ADD [RecommendedMonthlyContribution] decimal(18,2) NULL;
ALTER TABLE [ReserveStudies] ADD [FullyFundedBalance] decimal(18,2) NULL;
ALTER TABLE [ReserveStudies] ADD [SpecialAssessmentRisk] bit NULL;
ALTER TABLE [ReserveStudies] ADD [NextScheduledUpdate] datetime2 NULL;
```

### New Columns for Tenants Table (Trial)
```sql
ALTER TABLE [Tenants] ADD [TrialDaysRemaining] int NULL;
ALTER TABLE [Tenants] ADD [TrialStartedAt] datetime2 NULL;
ALTER TABLE [Tenants] ADD [TrialExpiresAt] datetime2 NULL;
ALTER TABLE [Tenants] ADD [HasUsedTrial] bit NOT NULL DEFAULT 0;
```

---

## File Locations Reference

### Core Models
- `CRS/Models/ReserveStudy.cs`
- `CRS/Models/Tenant.cs`
- `CRS/Models/Community.cs`
- `CRS/Models/FinancialInfo.cs`
- `CRS/Models/Proposal.cs`
- `CRS/Models/Invoice.cs`

### Key Services
- `CRS/Services/ReserveStudyService.cs`
- `CRS/Services/Billing/BillingService.cs`
- `CRS/Core/ReserveStudy/Services/ReserveStudyCalculator.cs`
- `CRS/Services/ReserveCalculator/ReserveStudyCalculatorService.cs`

### Database Context
- `CRS/Data/ApplicationDbContext.cs`

### Migrations
- `CRS/Migrations/`

---

## Conclusion

The CRS application is **production-ready** with a comprehensive feature set. The enhancements listed above are optimizations that can be implemented post-launch based on user feedback and business priorities.

**Recommended Immediate Action:** Add `PercentFunded` fields before launch as this is fundamental to reserve study reporting and will require database changes.

---

*Document generated: January 2025*
*Last reviewed: [Current Date]*
