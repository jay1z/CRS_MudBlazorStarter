# Schema Enhancements - Pre-Launch Checklist

> **Purpose**: Track database schema additions and improvements before SaaS launch.  
> **Last Updated**: December 8, 2024  
> **Status**: Click-Wrap Agreements Complete - 2 items remaining (Invoicing, Tasks)

---

## Priority Legend

| Priority | Meaning |
|----------|---------|
| 🔴 **High** | Critical for launch - core functionality |
| 🟡 **Medium** | Important for user experience |
| 🟢 **Low** | Nice to have - can add post-launch |

---

## 🔴 High Priority (Pre-Launch Required)

### 1. Document Management
**Status**: ✅ Complete (including UI and dedicated storage service)  
**Why**: Core to deliverables - studies generate reports that need to be stored and shared with clients.

**Files Created**: 
- `CRS\Models\Document.cs`
- `CRS\Services\Interfaces\IDocumentService.cs`
- `CRS\Services\DocumentService.cs`
- `CRS\Services\Storage\IDocumentStorageService.cs`
- `CRS\Services\Storage\DocumentStorageService.cs`
- `CRS\Components\Pages\Documents\Index.razor`
- `CRS\Components\Pages\Documents\DocumentUploadDialog.razor`
- `CRS\Components\Pages\Documents\DocumentEditDialog.razor`

**Tasks**:
- [x] Create `Document.cs` model
- [x] Create `DocumentType` enum (includes: Other, Report, Invoice, Photo, Contract, FinancialStatement, ProposalDocument, SiteVisitNotes, Insurance, Warranty, MaintenanceRecord)
- [x] Add `DbSet<Document>` to `ApplicationDbContext`
- [x] Add index configuration (Tenant, Study, Community, Type/Public)
- [x] Create migration (`AddHighPrioritySchemaEnhancements`)
- [x] Create `IDocumentService` interface
- [x] Implement `DocumentService`
- [x] Create `IDocumentStorageService` interface (dedicated document storage)
- [x] Implement `DocumentStorageService` (uploads to `documents` container)
- [x] Implement `NullDocumentStorageService` (development fallback)
- [x] Register document storage services in `Program.cs`
- [x] Add document upload UI component
- [x] Create Document Library page (`/Documents`)
- [x] Create `DocumentUploadDialog` component (uses native InputFile for Blazor Server compatibility)
- [x] Create `DocumentEditDialog` component
- [x] Add navigation link to NavMenu.razor

**Storage Architecture**:
| Service | Container | Purpose |
|---------|-----------|---------|
| `ILogoStorageService` | `tenant-logos` | Tenant branding logos |
| `IPhotoStorageService` | `site-visit-photos` | Site visit photos with thumbnails |
| `IDocumentStorageService` | `documents` | General documents (PDF, Word, Excel, etc.) |

---

### 2. Email Log/History
**Status**: ✅ Complete (including UI and logging integration)  
**Why**: Compliance and support - track all system-sent emails for troubleshooting and audit.

**Files Created**: 
- `CRS\Models\EmailLog.cs`
- `CRS\Services\Interfaces\IEmailLogService.cs`
- `CRS\Services\EmailLogService.cs`
- `CRS\Services\Email\LoggingMailer.cs`
- `CRS\Components\Pages\Admin\EmailLogs.razor`
- `CRS\Components\Pages\Admin\EmailLogDetailDialog.razor`

**Tasks**:
- [x] Create `EmailLog.cs` model
- [x] Create `EmailStatus` enum (includes: Queued, Sending, Sent, Delivered, Opened, Clicked, Bounced, Failed, Unsubscribed, SpamComplaint)
- [x] Add `DbSet<EmailLog>` to `ApplicationDbContext`
- [x] Add indexes (Tenant, Email/Sent, Status/Sent, ExternalMessageId)
- [x] Create migration (`AddHighPrioritySchemaEnhancements`)
- [x] Create `IEmailLogService` interface
- [x] Implement `EmailLogService`
- [x] Add email history view in admin (`/Admin/EmailLogs`)
- [x] Create `EmailLogDetailDialog` component
- [x] Add navigation link to TenantNavMenu
- [x] Create `LoggingMailer` decorator to wrap Coravel IMailer
- [x] Register logging in `Program.cs` with `AddEmailLogging()`

---

### 3. Notification Model Fixes
**Status**: ✅ Complete  
**Why**: Current `Notification` model was incomplete - missing tenant scope and user targeting.

**File Updated**: `CRS\Models\Notification.cs`

**Changes Made**:
- Added `TenantId` (tenant-scoped)
- Added `UserId` with FK to ApplicationUser
- Added `Title` field
- Added `IsRead` and `ReadAt` fields
- Added `EntityType`, `EntityId`, `ActionUrl` for deep linking
- Added `Category` for grouping
- Added `NotificationType` enum (Info, Success, Warning, Error, Action)
- Added `NotificationPriority` enum (Low, Normal, High, Urgent)
- Removed old `Status` string field (replaced by `Type` enum)

**Tasks**:
- [x] Update `Notification.cs` model with new fields
- [x] Add `NotificationType` enum
- [x] Add `NotificationPriority` enum
- [x] Implement `ITenantScoped`
- [x] Add `UserId` FK and navigation
- [x] Update `ApplicationDbContext` configuration
- [x] Create migration (`AddHighPrioritySchemaEnhancements`)
- [x] Update `IAppNotificationService` interface
- [x] Update `AppNotificationService` implementation

---

## 🟡 Medium Priority (Post-Launch Phase 1)

### 4. Site Visit Photos
**Status**: ✅ Complete (including UI with Blazor Server file upload fix)  
**Why**: Core workflow support - capture and organize photos during site inspections.

**Files Created**: 
- `CRS\Models\SiteVisitPhoto.cs`
- `CRS\Services\Interfaces\ISiteVisitPhotoService.cs`
- `CRS\Services\SiteVisitPhotoService.cs`
- `CRS\Services\Storage\IPhotoStorageService.cs`
- `CRS\Services\Storage\PhotoStorageService.cs`
- `CRS\Components\Shared\SiteVisitPhotos\SiteVisitPhotoGallery.razor`
- `CRS\Components\Shared\SiteVisitPhotos\PhotoUploadDialog.razor`
- `CRS\Components\Shared\SiteVisitPhotos\PhotoViewerDialog.razor`

**Features**:
- Links to ReserveStudy (required)
- Optional element association (ElementId, ElementType)
- Photo storage with thumbnail support
- GPS coordinates (Latitude, Longitude)
- `ElementCondition` enum (NotAssessed, Excellent, Good, Fair, Poor, Critical, NeedsReplacement)
- `PhotoCategory` enum (General, Exterior, Interior, Roof, Foundation, Mechanical, Electrical, Plumbing, CommonArea, Amenity, Damage, BeforeAfter)
- Organization fields (SortOrder, IncludeInReport, IsPrimary)

**Tasks**:
- [x] Create `SiteVisitPhoto.cs` model
- [x] Create `ElementCondition` enum
- [x] Create `PhotoCategory` enum
- [x] Add DbContext configuration
- [x] Create migration (`AddMediumPrioritySchemaEnhancements`)
- [x] Create `ISiteVisitPhotoService` interface
- [x] Implement `SiteVisitPhotoService`
- [x] Add photo gallery component for site visits
- [x] Create `IPhotoStorageService` for Azure Blob Storage
- [x] Implement `PhotoStorageService` with thumbnail generation
- [x] Add `UploadPhotoFromBytesAsync` method (pre-read bytes for Blazor Server modal compatibility)
- [x] Create `SiteVisitPhotoGallery` component
- [x] Create `PhotoUploadDialog` component (uses native InputFile for Blazor Server compatibility)
- [x] Create `PhotoViewerDialog` component
- [x] Add "Site Photos" tab to reserve study details page
- [x] Implement role-based access (HOA users view only, staff can edit)

**Blazor Server File Upload Fix**:
The `PhotoUploadDialog` was updated to use native `<InputFile>` with immediate byte reading to avoid the `_blazorFilesById` error that occurs when `IBrowserFile` references become stale in modal dialogs.

---

### 5. Study Notes/Comments
**Status**: ✅ Complete (including UI)  
**Why**: Team collaboration - internal notes for staff working on studies.

**Files Created**: 
- `CRS\Models\StudyNote.cs`
- `CRS\Services\Interfaces\IStudyNoteService.cs`
- `CRS\Services\StudyNoteService.cs`
- `CRS\Components\Shared\StudyNotes\StudyNotesPanel.razor`

**Features**:
- Links to ReserveStudy (required)
- Author tracking with FK to ApplicationUser
- `NoteVisibility` enum (Internal, ClientVisible, Private)
- `NoteType` enum (General, Question, ActionItem, Issue, Resolution, ClientFeedback, InternalReview, StatusUpdate)
- Threading support (ParentNoteId for replies)
- Pin and resolve functionality
- User mentions support (MentionedUserIds)
- Optional link to workflow status

**Tasks**:
- [x] Create `StudyNote.cs` model
- [x] Create `NoteVisibility` enum
- [x] Create `NoteType` enum
- [x] Add DbContext configuration
- [x] Create migration (`AddMediumPrioritySchemaEnhancements`)
- [x] Create `IStudyNoteService` interface
- [x] Implement `StudyNoteService`
- [x] Create `StudyNotesPanel` component with CRUD, pinning, replies
- [x] Add Notes tab to ReserveStudy Details page

---

### 6. Generated Reports Tracking
**Status**: ✅ Complete (including UI)  
**Why**: Track deliverables - know what reports were generated and shared.

**Files Created**: 
- `CRS\Models\GeneratedReport.cs`
- `CRS\Services\Interfaces\IGeneratedReportService.cs`
- `CRS\Services\GeneratedReportService.cs`
- `CRS\Components\Shared\GeneratedReports\GeneratedReportsPanel.razor`

**Features**:
- Links to ReserveStudy (required)
- Version tracking with supersedes support
- `ReportType` enum (Draft, Final, ExecutiveSummary, FundingPlan, ComponentInventory, FullReport, UpdateReport, Addendum, CorrectionNotice)
- `ReportStatus` enum (Draft, PendingReview, InReview, RevisionRequired, Approved, Published, Superseded, Archived)
- Generation tracking (GeneratedBy, GeneratedAt, TemplateUsed)
- Review workflow (ReviewedBy, ReviewedAt)
- Client publishing (PublishedAt, SentToClientAt, SentToEmail)
- Download tracking (DownloadCount, LastDownloadedAt)

**Tasks**:
- [x] Create `GeneratedReport.cs` model
- [x] Create `ReportType` enum
- [x] Create `ReportStatus` enum
- [x] Add DbContext configuration
- [x] Create migration (`AddMediumPrioritySchemaEnhancements`)
- [x] Create `IGeneratedReportService` interface
- [x] Implement `GeneratedReportService`
- [x] Create `GeneratedReportsPanel` component with generation, versioning, status workflow
- [x] Add Reports tab to ReserveStudy Details page

---

## 🟢 Low Priority (Future Enhancements)

### 7. Click-Wrap Agreements (Proposal Acceptance)
**Status**: ✅ Complete (including UI)  
**Why**: Proposal acceptance tracking without third-party e-signature integration. Provides legally binding click-wrap agreements with typed signatures, IP logging, and terms version tracking.

**Files Created**: 
- `CRS\Models\ProposalAcceptance.cs`
- `CRS\Models\AcceptanceTermsTemplate.cs`
- `CRS\Services\Interfaces\IProposalAcceptanceService.cs`
- `CRS\Services\ProposalAcceptanceService.cs`
- `CRS\Components\Shared\ProposalAcceptance\ProposalAcceptanceDialog.razor`
- `CRS\Components\Shared\ProposalAcceptance\AcceptanceConfirmationPanel.razor`

**Features**:
- **ProposalAcceptance Model**:
  - Links to ReserveStudy (required) and Proposal (optional)
  - Typed signature (full legal name)
  - Acceptor title, organization, email
  - IP address and user agent capture
  - Terms version tracking with content hash (SHA-256)
  - Checkbox confirmation flag
  - Revocation support with reason
  - Audit timestamps

- **AcceptanceTermsTemplate Model**:
  - Version tracking for legal terms
  - `TermsType` enum (ProposalAcceptance, TermsOfService, PrivacyPolicy, DataProcessingAgreement, ServiceLevelAgreement, Other)
  - Markdown-formatted terms text
  - Configurable checkbox text and button text
  - Effective date and expiration date support
  - Default template per type per tenant
  - Content hash for verification

- **ProposalAcceptanceDialog**:
  - Displays terms with Markdown rendering
  - Typed signature input with validation
  - Optional title/role and organization fields
  - "I agree" checkbox confirmation
  - IP address and user agent capture
  - Legal notice about electronic signature

- **AcceptanceConfirmationPanel**:
  - Shows acceptance details on reserve study page
  - Verification details (terms version, hash, IP)
  - Revocation status display
  - "Not yet accepted" state for pending proposals

**Tasks**:
- [x] Create `ProposalAcceptance.cs` model
- [x] Create `AcceptanceTermsTemplate.cs` model  
- [x] Add `TermsType` enum
- [x] Add DbSet configurations to `ApplicationDbContext`
- [x] Add entity configurations with indexes
- [x] Create migration (`AddClickWrapAgreements`)
- [x] Create `IProposalAcceptanceService` interface
- [x] Implement `ProposalAcceptanceService` with terms hash, seeding
- [x] Create `ProposalAcceptanceDialog` component
- [x] Create `AcceptanceConfirmationPanel` component
- [x] Add "Acceptance" tab to Reserve Study Details page
- [x] Integrate with proposal workflow (auto-advance on accept)
- [x] Register service in `Program.cs`

**Legal Compliance Features**:
- SHA-256 hash of terms content at time of acceptance
- IP address and user agent logging
- Explicit checkbox confirmation tracking
- Terms version history
- Revocation tracking with reason

---

### 8. Study Invoicing
**Status**: ⬜ Not Started  
**Why**: Only if billing per-study (vs. subscription) is needed.

### 9. Study Reminders/Tasks
**Status**: ⬜ Not Started  
**Why**: Beyond calendar events - study-specific task management.

---

## Schema Fixes (Existing Models)

### CalendarEvent - Missing Tenant Scope
**Status**: ✅ Complete

**File Updated**: `CRS\Models\CalendarEvent.cs`

**Changes Made**:
- Added `TenantId` (tenant-scoped)
- Added `ReserveStudyId` with FK to ReserveStudy (for site visits)
- Added `EventType` enum (Other, SiteVisit, ClientMeeting, InternalMeeting, Deadline, Reminder, Training, Review)
- Implemented `ITenantScoped`
- Added indexes for Tenant, DateRange, Study, and EventType

**Tasks**:
- [x] Add `TenantId` to `CalendarEvent`
- [x] Add `ReserveStudyId` to `CalendarEvent`
- [x] Add `EventType` enum
- [x] Implement `ITenantScoped`
- [x] Create migration (`AddSchemaFixes`)

---

### ServiceContact - Missing Tenant Scope
**Status**: ✅ Complete

**File Updated**: `CRS\Models\ServiceContact.cs`

**Changes Made**:
- Added `TenantId` (tenant-scoped)
- Added `ContactType` enum (Other, Roofing, HVAC, Plumbing, Electrical, Landscaping, Paving, Painting, GeneralContractor, PoolService, Elevator, FireSafety, Security)
- Added `Notes` field
- Added `IsActive` field
- Added MaxLength constraints to existing fields
- Implemented `ITenantScoped`
- Added indexes for Tenant, Type/Active, and Company

**Tasks**:
- [x] Add `TenantId` to `ServiceContact`
- [x] Add `ContactType` enum
- [x] Implement `ITenantScoped`
- [x] Create migration (`AddSchemaFixes`)

---

### SupportTicket - Add Priority and Assignment
**Status**: ✅ Complete

**File Updated**: `CRS\Models\SupportTicket.cs`

**Changes Made**:
- Changed `Status` from string to `TicketStatus` enum (Open, InProgress, WaitingOnCustomer, WaitingOnThirdParty, Resolved, Closed, Cancelled)
- Added `Priority` enum (Low, Medium, High, Urgent, Critical)
- Added `Category` enum (General, Technical, Billing, FeatureRequest, Bug, Training, DataRequest, AccountAccess)
- Added `CreatedByUserId` with FK to ApplicationUser
- Added `AssignedToUserId` with FK to ApplicationUser
- Added `ReserveStudyId` with FK to ReserveStudy
- Added `AssignedAt`, `ClosedAt` timestamps
- Added `Resolution` field
- Added indexes for Status/Priority, Assigned/Status, and Study

**Tasks**:
- [x] Add `TicketStatus` enum (replaces string Status)
- [x] Add `TicketPriority` enum and field
- [x] Add `TicketCategory` enum and field
- [x] Add `CreatedByUserId` FK
- [x] Add `AssignedToUserId` FK
- [x] Add `ReserveStudyId` FK
- [x] Create migration (`AddSchemaFixes`)
- [x] Update `TicketService` to use enums

---

## Migration Strategy

### Recommended Order
1. **High Priority** - Complete before launch
   - Notification fixes (breaking change to existing model)
   - Document management
   - Email logging

2. **Schema Fixes** - Non-breaking additions
   - CalendarEvent tenant scope
   - ServiceContact tenant scope
   - SupportTicket enhancements

3. **Medium Priority** - Phase 1 post-launch
   - Site visit photos
   - Study notes
   - Generated reports tracking

### Migration Commands
```bash
# After each model change:
dotnet ef migrations add <MigrationName> --project CRS

# To apply:
dotnet ef database update --project CRS
```

---

## Progress Summary

| Category | Total | Complete | Remaining |
|----------|-------|----------|-----------|
| High Priority | 3 | 3 | 0 |
| Medium Priority | 3 | 3 | 0 |
| Low Priority | 3 | 1 | 2 |
| Schema Fixes | 3 | 3 | 0 |
| **Total** | **12** | **10** | **2** |

---

## Migrations Created

| Migration | Date | Description |
|-----------|------|-------------|
| `AddHighPrioritySchemaEnhancements` | Dec 7, 2024 | Document, EmailLog tables; Notification model updates |
| `AddSchemaFixes` | Dec 7, 2024 | CalendarEvent, ServiceContact, SupportTicket tenant scope and enhancements |
| `AddMediumPrioritySchemaEnhancements` | Dec 7, 2024 | SiteVisitPhoto, StudyNote, GeneratedReport tables |
| `AddClickWrapAgreements` | Dec 8, 2024 | ProposalAcceptance, AcceptanceTermsTemplate tables |

---

## Notes

- All new models should implement `ITenantScoped` for multi-tenancy
- Use `Guid.CreateVersion7()` for new IDs (time-ordered)
- Add appropriate indexes for tenant-filtered queries
- Consider soft-delete pattern (`DateDeleted`) for audit trail

---

## Blazor Server File Upload Pattern

**Problem**: In Blazor Server, `IBrowserFile` references become stale after component re-renders. This causes the error:
```
Cannot read properties of null (reading '_blazorFilesById')
```

**Solution**: Read file bytes **immediately** in the `OnChange` event handler before any UI updates or async operations that could trigger re-renders.

**Pattern Used in This Project**:

```razor
<!-- Use native InputFile with hidden style -->
<InputFile @ref="fileInput"
           OnChange="HandleFileUpload" 
           accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png" 
           style="display: none;" />

<MudButton OnClick="TriggerFileInput">Choose File</MudButton>

@code {
    private InputFile? fileInput;
    private byte[]? fileBytes;
    
    private async Task TriggerFileInput()
    {
        if (fileInput?.Element != null)
        {
            await JS.InvokeVoidAsync("clickElement", fileInput.Element);
        }
    }
    
    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        
        // Read bytes IMMEDIATELY - before any await that could cause re-render
        await using var stream = file.OpenReadStream(MaxFileSize);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        fileBytes = memoryStream.ToArray();
        
        // Store metadata
        originalFileName = file.Name;
        contentType = file.ContentType;
        fileSize = file.Size;
    }
}
```

**Components Using This Pattern**:
- `DocumentUploadDialog.razor` - Uses `IDocumentStorageService.UploadAsync()`
- `PhotoUploadDialog.razor` - Uses `IPhotoStorageService.UploadPhotoFromBytesAsync()`

**Storage Service Methods for Pre-Read Bytes**:
- `IDocumentStorageService.UploadAsync(tenantId, studyId, fileName, contentType, byte[] fileBytes)`
- `IPhotoStorageService.UploadFromBytesAsync(tenantId, studyId, fileName, contentType, byte[] fileBytes)`
- `IPhotoStorageService.UploadPhotoFromBytesAsync(tenantId, studyId, fileName, contentType, byte[] fileBytes)` - includes thumbnail generation
