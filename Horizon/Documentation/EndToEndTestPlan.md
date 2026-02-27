# Reserve Study End-to-End Test Plan

## Overview

This document provides a comprehensive end-to-end testing guide for the complete Reserve Study workflow, from initial request creation to final report delivery and archival. The test plan covers all user roles (HOA Board Member, Tenant Company Staff, Inspector/Specialist) and system interactions (SaaS Platform).

---

## Table of Contents

### Happy Path Testing
1. [Prerequisites](#prerequisites)
2. [Test Environment Setup](#test-environment-setup)
3. [User Roles & Accounts](#user-roles--accounts)
4. [Phase 1: HOA Request Submission](#phase-1-hoa-request-submission)
5. [Phase 2: Tenant Company Review & Assignment](#phase-2-tenant-company-review--assignment)
6. [Phase 3: Proposal Generation & Acceptance](#phase-3-proposal-generation--acceptance)
7. [Phase 4: Financial Information Collection](#phase-4-financial-information-collection)
8. [Phase 5: Site Visit & Inspection](#phase-5-site-visit--inspection)
9. [Phase 6: Report Generation & QA Review](#phase-6-report-generation--qa-review)
10. [Phase 7: Report Delivery & Completion](#phase-7-report-delivery--completion)
11. [Phase 8: Archival & Renewal](#phase-8-archival--renewal)

### Failure & Edge Case Testing
12. [Failure Testing & Edge Cases](#failure-testing--edge-cases)
    - [F1: Authentication & Authorization Failures](#phase-f1-authentication--authorization-failures)
    - [F2: Input Validation Failures](#phase-f2-input-validation-failures)
    - [F3: Workflow State Transition Failures](#phase-f3-workflow-state-transition-failures)
    - [F4: File Upload Failures](#phase-f4-file-upload-failures)
    - [F5: Tenant Isolation Failures](#phase-f5-tenant-isolation-failures)
    - [F6: Database & System Failures](#phase-f6-database--system-failures)
    - [F7: Browser & Client-Side Failures](#phase-f7-browser--client-side-failures)
    - [F8: Security Vulnerability Testing](#phase-f8-security-vulnerability-testing)
    - [F9: Boundary & Edge Case Testing](#phase-f9-boundary--edge-case-testing)

### Reference
13. [Workflow Status Reference](#workflow-status-reference)
14. [Verification Queries](#verification-queries)
15. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### System Requirements
- [ ] Application is running and accessible
- [ ] Database is initialized with seed data
- [ ] Email service is configured (or use test/mock email)
- [ ] At least one active Tenant exists with valid subscription
- [ ] Notification service is operational

### Required Test Data
- Valid tenant subdomain (e.g., `testcompany.alxreservecloud.com`)
- Test user accounts for each role (see [User Roles & Accounts](#user-roles--accounts))
- Sample community/property data
- Sample building elements data

---

## Test Environment Setup

### 1. Verify Tenant Configuration

```sql
-- Verify test tenant exists and is active
SELECT 
    Id,
    Name,
    Subdomain,
    IsActive,
    SubscriptionStatus,
    ProvisioningStatus
FROM crs.Tenants
WHERE Subdomain = 'testcompany';
```

**Expected**: Tenant is active with `SubscriptionStatus = 2` (Active) and `ProvisioningStatus = 2` (Active).

### 2. Create Test Users

Create or verify the following test accounts exist:

| Role | Email | Purpose |
|------|-------|---------|
| TenantOwner | owner@testcompany.com | Tenant administration, review, approval |
| TenantSpecialist | specialist@testcompany.com | Inspections, proposals, reports |
| HOAUser | boardmember@testhoa.com | Submit requests, accept proposals |

### 3. Verify Role Assignments

```sql
-- Verify role assignments for test users
SELECT 
    u.Email,
    r.Name AS RoleName,
    ura.TenantId
FROM crs.UserRoleAssignments ura
JOIN crs.AspNetRoles r ON ura.RoleId = r.Id
JOIN crs.AspNetUsers u ON ura.UserId = u.Id
WHERE u.Email IN ('owner@testcompany.com', 'specialist@testcompany.com', 'boardmember@testhoa.com');
```

---

## User Roles & Accounts

### HOA Board Member (External Customer)
- **Access**: `/ReserveStudies/Request` page
- **Capabilities**: 
  - Create new reserve study requests
  - Add community and element details
  - Submit requests for review
  - Accept/reject proposals
  - Provide financial information
  - View progress and final reports

### Tenant Company Staff

#### TenantOwner
- **Access**: Full tenant administration
- **Capabilities**:
  - Review submitted requests
  - Approve/request more info
  - Assign specialists
  - QA review reports
  - Finalize and deliver reports

#### TenantSpecialist
- **Access**: Assigned studies only
- **Capabilities**:
  - Receive assignments
  - Draft proposals
  - Schedule appointments
  - Perform inspections
  - Upload photos and notes
  - Generate draft reports

### SaaS Platform (Automated)
- **Functions**:
  - Create study records
  - Send notifications
  - Track status and progress
  - Archive reports
  - Schedule renewal reminders

---

## Phase 1: HOA Request Submission

### Test Case 1.1: Submit New Reserve Study Request

**Actor**: HOA Board Member  
**Starting URL**: `https://testcompany.alxreservecloud.com/ReserveStudies/Request`  
**Initial Status**: None → `NewRequest`

#### Steps:

1. **Login as HOA Board Member**
   - [ ] Navigate to tenant subdomain login page
   - [ ] Enter credentials for `boardmember@testhoa.com`
   - [ ] Verify successful login and redirect to dashboard

2. **Navigate to Request Form**
   - [ ] Click "New Reserve Study Request" or navigate to `/ReserveStudies/Request`
   - [ ] Verify request form loads correctly
   - [ ] Verify tenant branding is displayed

3. **Step 1: Community Selection**
   - [ ] Select existing community OR create new community
   - [ ] If new: Enter community name, address, unit count
   - [ ] Verify tenant isolation (only see current tenant's communities)
   - [ ] Click "Next"

4. **Step 2: Contact Information**
   - [ ] Enter primary contact details (name, email, phone)
   - [ ] Optionally add property manager information
   - [ ] Click "Next"

5. **Step 3: Add Element Details**
   - [ ] Add building elements (roofing, HVAC, elevators, etc.)
   - [ ] Add common area elements (pool, clubhouse, parking, etc.)
   - [ ] For each element, specify:
     - [ ] Quantity/measurement
     - [ ] Last replacement date
     - [ ] Condition assessment
     - [ ] Service contact (if applicable)
   - [ ] Click "Next"

6. **Step 4: Review and Submit**
   - [ ] Review all entered information
   - [ ] Accept terms and conditions
   - [ ] Click "Submit Request"
   - [ ] Verify success message displayed
   - [ ] Verify email confirmation received

#### Verification:

```sql
-- Verify request was created
SELECT 
    rs.Id,
    rs.Status,
    rs.DateCreated,
    rs.TenantId,
    c.Name AS CommunityName,
    sr.CurrentStatus AS WorkflowStatus
FROM crs.ReserveStudies rs
LEFT JOIN crs.Communities c ON rs.CommunityId = c.Id
LEFT JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE rs.ApplicationUserId = (SELECT Id FROM crs.AspNetUsers WHERE Email = 'boardmember@testhoa.com')
ORDER BY rs.DateCreated DESC;
```

**Expected Results**:
- [ ] Reserve study record created with correct TenantId
- [ ] Status = `RequestCreated` (0)
- [ ] WorkflowStatus = `NewRequest` (0)
- [ ] Community linked correctly
- [ ] All elements saved
- [ ] Notification sent to tenant staff

---

## Phase 2: Tenant Company Review & Assignment

### Test Case 2.1: Review Uploaded Data

**Actor**: TenantOwner  
**Starting URL**: `https://testcompany.alxreservecloud.com/ReserveStudies`  
**Status Transition**: `NewRequest` → `PendingDetails` → `ReadyForReview`

#### Steps:

1. **Login as Tenant Owner**
   - [ ] Navigate to tenant login
   - [ ] Login with `owner@testcompany.com`
   - [ ] Verify dashboard shows pending requests

2. **Review New Request**
   - [ ] Navigate to Reserve Studies list
   - [ ] Find newly submitted request
   - [ ] Click to view details
   - [ ] Review:
     - [ ] Community information
     - [ ] Contact details
     - [ ] Property elements
     - [ ] Element conditions

3. **Decision: Approve or Request Info**

   **Option A: Approve Request**
   - [ ] Click "Approve" button
   - [ ] Verify status changes to `Approved`
   - [ ] Proceed to assignment

   **Option B: Request More Information**
   - [ ] Click "Request Info" button
   - [ ] Enter details of required information
   - [ ] Click "Send Request"
   - [ ] Verify status changes to `NeedsInfo`
   - [ ] Verify HOA user receives notification
   - [ ] (HOA provides info, resubmits)
   - [ ] Re-review and approve

#### Verification:

```sql
-- Verify status transition
SELECT 
    rs.Id,
    rs.Status,
    sr.CurrentStatus,
    sr.StatusChangedBy,
    sr.StateChangedAt
FROM crs.ReserveStudies rs
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE rs.Id = 'YOUR_STUDY_ID';
```

### Test Case 2.2: Assign Specialist

**Actor**: TenantOwner  
**Status Transition**: `Approved` → `Assigned`

#### Steps:

1. **Open Approved Request**
   - [ ] Navigate to approved reserve study
   - [ ] Verify "Assign Specialist" button is visible

2. **Assign Specialist**
   - [ ] Click "Assign Specialist" button
   - [ ] Select specialist from dropdown (`specialist@testcompany.com`)
   - [ ] Click "Assign"
   - [ ] Verify success message

3. **Verify Assignment**
   - [ ] Specialist name appears on study details
   - [ ] Status updates to `Assigned`
   - [ ] Specialist receives notification email

#### Verification:

```sql
-- Verify specialist assignment
SELECT 
    rs.Id,
    rs.SpecialistUserId,
    u.Email AS SpecialistEmail,
    sr.CurrentStatus
FROM crs.ReserveStudies rs
JOIN crs.AspNetUsers u ON rs.SpecialistUserId = u.Id
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE rs.Id = 'YOUR_STUDY_ID';
```

**Expected**: `CurrentStatus` = `Assigned` (5), SpecialistUserId populated

---

## Phase 3: Proposal Generation & Acceptance

### Test Case 3.1: Draft Proposal

**Actor**: TenantSpecialist  
**Status Transition**: `Assigned` → `ProposalPendingESign`

#### Steps:

1. **Login as Specialist**
   - [ ] Login with `specialist@testcompany.com`
   - [ ] Verify assigned study appears in dashboard

2. **Create Proposal**
   - [ ] Navigate to assigned study
   - [ ] Click "Create Proposal" or navigate to Proposal section
   - [ ] Enter proposal details:
     - [ ] Scope of work
     - [ ] Estimated cost
     - [ ] Timeline
     - [ ] Terms and conditions
     - [ ] Comments/notes
   - [ ] Save draft

3. **Send Proposal to HOA**
   - [ ] Review proposal
   - [ ] Click "Send Proposal"
   - [ ] Verify status changes to `ProposalPendingESign`
   - [ ] Verify HOA receives proposal email

#### Verification:

```sql
-- Verify proposal creation
SELECT 
    p.Id,
    p.ReserveStudyId,
    p.ProposalScope,
    p.EstimatedCost,
    p.DateSent,
    sr.CurrentStatus
FROM crs.Proposals p
JOIN crs.ReserveStudies rs ON p.ReserveStudyId = rs.Id
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE p.ReserveStudyId = 'YOUR_STUDY_ID';
```

### Test Case 3.2: Accept Proposal (E-Sign)

**Actor**: HOA Board Member  
**Status Transition**: `ProposalPendingESign` → `Accepted`

#### Steps:

1. **Login as HOA User**
   - [ ] Login with `boardmember@testhoa.com`
   - [ ] Navigate to reserve study details

2. **Review Proposal**
   - [ ] Click "Acceptance" tab
   - [ ] Review proposal scope and cost
   - [ ] Review terms and conditions

3. **Accept Proposal**
   - [ ] Click "Accept Proposal" button
   - [ ] Enter electronic signature
   - [ ] Confirm acceptance
   - [ ] Verify success message

4. **Verify Acceptance**
   - [ ] Status changes to `Accepted`
   - [ ] Acceptance confirmation displayed
   - [ ] Tenant staff notified

#### Alternative: Reject Proposal

1. **Reject Proposal**
   - [ ] Click "Reject Proposal" button
   - [ ] Enter reason for rejection
   - [ ] Confirm rejection
   - [ ] Status changes to `Rejected`
   - [ ] Tenant staff notified for re-proposal

#### Verification:

```sql
-- Verify proposal acceptance
SELECT 
    p.Id,
    p.DateApproved,
    pa.AcceptedAt,
    pa.SignatureData,
    sr.CurrentStatus
FROM crs.Proposals p
LEFT JOIN crs.ProposalAcceptances pa ON p.Id = pa.ProposalId
JOIN crs.ReserveStudies rs ON p.ReserveStudyId = rs.Id
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE p.ReserveStudyId = 'YOUR_STUDY_ID';
```

---

## Phase 4: Financial Information Collection

### Test Case 4.1: Request Financial Information

**Actor**: TenantSpecialist  
**Status Transition**: `Accepted` → (request sent)

#### Steps:

1. **Login as Specialist**
   - [ ] Login with `specialist@testcompany.com`
   - [ ] Navigate to accepted study

2. **Request Financial Info**
   - [ ] Click "Request Financial Information" button
   - [ ] Verify request email sent to HOA contact
   - [ ] Status indicator shows "Awaiting Financial Info"

### Test Case 4.2: Submit Financial Information

**Actor**: HOA Board Member  
**Expected Platform Action**: Update status, notify specialist

#### Steps:

1. **Login as HOA User**
   - [ ] Login with `boardmember@testhoa.com`
   - [ ] Navigate to reserve study
   - [ ] Or click link in email

2. **Enter Financial Information**
   - [ ] Navigate to Financial Info section
   - [ ] Enter:
     - [ ] Current reserve fund balance
     - [ ] Annual contribution amount
     - [ ] Projected annual expenses
     - [ ] Fiscal year start month
     - [ ] Any additional comments
   - [ ] Click "Submit"

3. **Verify Submission**
   - [ ] Success message displayed
   - [ ] Status updates appropriately
   - [ ] Specialist notified

#### Verification:

```sql
-- Verify financial info submission
SELECT 
    fi.Id,
    fi.CurrentReserveFundBalance,
    fi.AnnualContribution,
    fi.DateSubmitted,
    fi.DateReviewed
FROM crs.FinancialInfos fi
WHERE fi.ReserveStudyId = 'YOUR_STUDY_ID';
```

---

## Phase 5: Site Visit & Inspection

### Test Case 5.1: Schedule Appointment

**Actor**: TenantSpecialist  
**Status Transition**: `Accepted` → `Scheduled`

#### Steps:

1. **Login as Specialist**
   - [ ] Navigate to study details

2. **Schedule Site Visit**
   - [ ] Click "Schedule Site Visit" button
   - [ ] Select date and time
   - [ ] Add notes/instructions
   - [ ] Click "Confirm"
   - [ ] Verify status changes to `Scheduled`
   - [ ] HOA user receives calendar invite/notification

### Test Case 5.2: Perform Inspection

**Actor**: TenantSpecialist  
**Status Transition**: `Scheduled` → `InProgress`

#### Steps:

1. **Start Inspection**
   - [ ] On inspection day, mark status as "In Progress"
   - [ ] Navigate to study on mobile/tablet device

2. **Document Findings**
   - [ ] For each element:
     - [ ] Assess current condition
     - [ ] Estimate remaining useful life
     - [ ] Document replacement cost estimate
   - [ ] Take photos of each element
   - [ ] Add notes and observations

### Test Case 5.3: Upload Photos & Notes

**Actor**: TenantSpecialist  
**Status Transition**: `InProgress` → `UnderReview`

#### Steps:

1. **Upload Site Visit Photos**
   - [ ] Navigate to "Site Photos" tab
   - [ ] Upload photos for each element
   - [ ] Add captions/descriptions
   - [ ] Tag elements appropriately

2. **Add Notes**
   - [ ] Navigate to "Notes" tab
   - [ ] Add inspection notes
   - [ ] Document any issues or concerns
   - [ ] Add recommendations

3. **Complete Inspection**
   - [ ] Mark inspection as complete
   - [ ] Status changes to `UnderReview`
   - [ ] Data uploaded for review

#### Verification:

```sql
-- Verify photos uploaded
SELECT 
    svp.Id,
    svp.FileName,
    svp.Description,
    svp.UploadedAt
FROM crs.SiteVisitPhotos svp
WHERE svp.ReserveStudyId = 'YOUR_STUDY_ID';

-- Verify notes added
SELECT 
    sn.Id,
    sn.Content,
    sn.CreatedAt,
    sn.CreatedBy
FROM crs.StudyNotes sn
WHERE sn.ReserveStudyId = 'YOUR_STUDY_ID';
```

---

## Phase 6: Report Generation & QA Review

### Test Case 6.1: Generate Draft Report

**Actor**: TenantSpecialist / SaaS Platform  
**Status Transition**: `UnderReview` → `ReportDrafted`

#### Steps:

1. **Review Inspection Data**
   - [ ] Verify all elements have been assessed
   - [ ] Verify photos are uploaded
   - [ ] Verify financial data is complete

2. **Generate Report**
   - [ ] Navigate to "Reports" tab
   - [ ] Click "Generate Draft Report"
   - [ ] Select report template
   - [ ] System compiles:
     - [ ] Component inventory
     - [ ] Condition assessments
     - [ ] Useful life estimates
     - [ ] Replacement costs
     - [ ] 30-year funding plan
   - [ ] Review generated report

3. **Submit for QA Review**
   - [ ] Click "Submit for QA"
   - [ ] Status changes to `ReportDrafted`
   - [ ] TenantOwner notified

### Test Case 6.2: QA Review

**Actor**: TenantOwner  
**Status Transition**: `ReportDrafted` → `ApprovedReport`

#### Steps:

1. **Login as Tenant Owner**
   - [ ] Navigate to study with draft report

2. **Review Report**
   - [ ] Review all sections:
     - [ ] Executive summary
     - [ ] Component inventory
     - [ ] Funding analysis
     - [ ] Recommendations
   - [ ] Check for accuracy and completeness
   - [ ] Review photos and documentation

3. **Decision: Approve or Request Revisions**

   **Option A: Approve Report**
   - [ ] Click "Approve Report"
   - [ ] Status changes to `ApprovedReport`
   - [ ] Proceed to delivery

   **Option B: Request Revisions**
   - [ ] Click "Request Revisions"
   - [ ] Enter revision notes
   - [ ] Status returns to `InProgress` or `UnderReview`
   - [ ] Specialist notified
   - [ ] Repeat until approved

#### Verification:

```sql
-- Verify report status
SELECT 
    gr.Id,
    gr.ReserveStudyId,
    gr.FileName,
    gr.GeneratedAt,
    gr.IsFinal,
    sr.CurrentStatus
FROM crs.GeneratedReports gr
JOIN crs.ReserveStudies rs ON gr.ReserveStudyId = rs.Id
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE gr.ReserveStudyId = 'YOUR_STUDY_ID';
```

---

## Phase 7: Report Delivery & Completion

### Test Case 7.1: Finalize and Deliver Report

**Actor**: TenantOwner  
**Status Transition**: `ApprovedReport` → `Complete`

#### Steps:

1. **Finalize Report**
   - [ ] Navigate to approved study
   - [ ] Click "Finalize Report"
   - [ ] System marks report as final
   - [ ] PDF version generated

2. **Deliver to HOA**
   - [ ] Click "Deliver Report"
   - [ ] Select delivery method:
     - [ ] Email with download link
     - [ ] Portal access
     - [ ] Both
   - [ ] HOA user receives notification

3. **Mark Complete**
   - [ ] Click "Mark as Complete"
   - [ ] Status changes to `Complete`
   - [ ] All parties notified

### Test Case 7.2: HOA Downloads Report

**Actor**: HOA Board Member

#### Steps:

1. **Login as HOA User**
   - [ ] Navigate to completed study

2. **Access Final Report**
   - [ ] Navigate to "Reports" tab
   - [ ] Verify final report is available
   - [ ] Download PDF
   - [ ] Verify report contents are complete

#### Verification:

```sql
-- Verify completion
SELECT 
    rs.Id,
    rs.IsComplete,
    rs.DateApproved,
    sr.CurrentStatus,
    sr.StateChangedAt
FROM crs.ReserveStudies rs
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE rs.Id = 'YOUR_STUDY_ID';
```

**Expected**: `IsComplete = 1`, `CurrentStatus = Complete` (14)

---

## Phase 8: Archival & Renewal

### Test Case 8.1: Archive Report

**Actor**: SaaS Platform (Automated) / TenantOwner  
**Status Transition**: `Complete` → `Archived`

#### Steps:

1. **Archive Study**
   - [ ] After completion period (or manual trigger)
   - [ ] Click "Archive" or automated archival
   - [ ] Status changes to `Archived`

2. **Schedule Renewal Reminder**
   - [ ] System schedules 3-year renewal reminder
   - [ ] Reminder date stored in database

3. **Verify Archival**
   - [ ] Study appears in "Archived" section
   - [ ] Report remains accessible
   - [ ] Renewal reminder scheduled

#### Verification:

```sql
-- Verify archival and renewal
SELECT 
    rs.Id,
    sr.CurrentStatus,
    sr.StateChangedAt,
    rs.DateModified
FROM crs.ReserveStudies rs
JOIN crs.StudyRequests sr ON rs.Id = sr.Id
WHERE rs.Id = 'YOUR_STUDY_ID';
```

**Expected**: `CurrentStatus = Archived` (15)

---

## Workflow Status Reference

### StudyStatus Enum (Engine)

| Value | Status | Description |
|-------|--------|-------------|
| 0 | NewRequest | Initial submission by HOA |
| 1 | PendingDetails | Awaiting complete information |
| 2 | ReadyForReview | All elements complete, ready for tenant review |
| 3 | NeedsInfo | Tenant requires additional information |
| 4 | Approved | Request approved by tenant |
| 5 | Assigned | Specialist assigned |
| 6 | ProposalPendingESign | Proposal awaiting HOA signature |
| 7 | Accepted | Proposal accepted by HOA |
| 8 | Rejected | Proposal rejected by HOA |
| 9 | Scheduled | Site visit scheduled |
| 10 | InProgress | Inspection in progress |
| 11 | UnderReview | Data uploaded, under review |
| 12 | ReportDrafted | Draft report created |
| 13 | ApprovedReport | Report passed QA |
| 14 | Complete | Report delivered |
| 15 | Archived | Study archived |

### Allowed State Transitions

```
NewRequest → PendingDetails
PendingDetails → ReadyForReview
ReadyForReview → Approved | NeedsInfo
NeedsInfo → ReadyForReview
Approved → Assigned
Assigned → ProposalPendingESign
ProposalPendingESign → Accepted | Rejected
Accepted → Scheduled
Scheduled → InProgress
InProgress → UnderReview
UnderReview → ReportDrafted | InProgress
ReportDrafted → ApprovedReport
ApprovedReport → Complete
Complete → Archived
```

---

## Verification Queries

### Complete Workflow Status Check

```sql
-- Full workflow status and history
SELECT 
    rs.Id AS StudyId,
    c.Name AS Community,
    ct.FullName AS Contact,
    sp.Email AS Specialist,
    rs.Status AS LegacyStatus,
    sr.CurrentStatus AS EngineStatus,
    rs.DateCreated,
    rs.IsComplete,
    rs.IsApproved,
    p.EstimatedCost AS ProposalCost,
    p.DateApproved AS ProposalApproved,
    fi.CurrentReserveFundBalance,
    fi.DateSubmitted AS FinancialInfoSubmitted
FROM crs.ReserveStudies rs
LEFT JOIN crs.Communities c ON rs.CommunityId = c.Id
LEFT JOIN crs.Contacts ct ON rs.ContactId = ct.Id
LEFT JOIN crs.AspNetUsers sp ON rs.SpecialistUserId = sp.Id
LEFT JOIN crs.StudyRequests sr ON rs.Id = sr.Id
LEFT JOIN crs.Proposals p ON rs.Id = p.ReserveStudyId
LEFT JOIN crs.FinancialInfos fi ON rs.Id = fi.ReserveStudyId
WHERE rs.Id = 'YOUR_STUDY_ID';
```

### Status History Audit

```sql
-- View all status changes for a study
SELECT 
    ssh.Id,
    ssh.FromStatus,
    ssh.ToStatus,
    ssh.ChangedAt,
    ssh.ChangedBy
FROM crs.StudyStatusHistories ssh
WHERE ssh.RequestId = 'YOUR_STUDY_ID'
ORDER BY ssh.ChangedAt;
```

### Notification Log Check

```sql
-- Check notifications sent for study
SELECT 
    n.Id,
    n.Type,
    n.Recipient,
    n.Subject,
    n.SentAt,
    n.DeliveryStatus
FROM crs.Notifications n
WHERE n.RelatedEntityId = 'YOUR_STUDY_ID'
ORDER BY n.SentAt;
```

---

## Failure Testing & Edge Cases

This section covers negative testing scenarios, input validation, boundary conditions, and security testing to ensure the application handles errors gracefully and prevents data corruption.

---

### Phase F1: Authentication & Authorization Failures

#### Test Case F1.1: Invalid Login Attempts

**Objective**: Verify the system handles authentication failures correctly

| Test ID | Test Scenario | Input | Expected Result | Status |
|---------|---------------|-------|-----------------|--------|
| F1.1.1 | Wrong password | Valid email, wrong password | Error: "Invalid credentials" - no user enumeration | ☐ |
| F1.1.2 | Non-existent user | fake@email.com | Error: "Invalid credentials" - same message as wrong password | ☐ |
| F1.1.3 | Empty email | "" (empty), any password | Validation error on email field | ☐ |
| F1.1.4 | Empty password | Valid email, "" (empty) | Validation error on password field | ☐ |
| F1.1.5 | SQL injection in email | `' OR '1'='1` | Input rejected, no SQL error exposed | ☐ |
| F1.1.6 | SQL injection in password | `'; DROP TABLE Users;--` | Input rejected, login fails safely | ☐ |
| F1.1.7 | XSS in email field | `<script>alert('xss')</script>` | Input sanitized/rejected | ☐ |
| F1.1.8 | Excessive login attempts | 10+ failed attempts | Account lockout or rate limiting | ☐ |
| F1.1.9 | Invalid tenant subdomain | `nonexistent.alxreservecloud.com` | Redirect to "Tenant not found" page | ☐ |
| F1.1.10 | Expired session | Session timeout | Redirect to login, no data loss | ☐ |

#### Test Case F1.2: Authorization Bypass Attempts

**Objective**: Verify users cannot access resources beyond their permissions

| Test ID | Test Scenario | Action | Expected Result | Status |
|---------|---------------|--------|-----------------|--------|
| F1.2.1 | HOA user accesses staff page | Navigate to `/ReserveStudies/Create` | 403 Forbidden or redirect | ☐ |
| F1.2.2 | Specialist accesses owner-only page | Navigate to admin settings | 403 Forbidden | ☐ |
| F1.2.3 | Cross-tenant data access | Modify URL with different TenantId | Access denied, no data returned | ☐ |
| F1.2.4 | HOA user views another's study | Direct URL to other user's study | Access denied or 404 | ☐ |
| F1.2.5 | Unauthenticated API access | Call API without auth token | 401 Unauthorized | ☐ |
| F1.2.6 | Expired token | Use expired JWT/session | 401 and redirect to login | ☐ |
| F1.2.7 | Tampered GUID in URL | `/ReserveStudies/{fake-guid}/Details` | 404 or proper error handling | ☐ |
| F1.2.8 | Specialist accesses unassigned study | Navigate to study not assigned to them | Access denied or limited view | ☐ |

---

### Phase F2: Input Validation Failures

#### Test Case F2.1: Community Creation - Invalid Input

**Location**: `/ReserveStudies/Request` - Step 1

| Test ID | Field | Invalid Input | Expected Result | Status |
|---------|-------|---------------|-----------------|--------|
| F2.1.1 | Community Name | Empty string | Required field validation error | ☐ |
| F2.1.2 | Community Name | Only spaces `"   "` | Validation error - name required | ☐ |
| F2.1.3 | Community Name | > 200 characters | Max length validation error | ☐ |
| F2.1.4 | Community Name | `<script>alert('xss')</script>` | XSS sanitized, saved as text | ☐ |
| F2.1.5 | Unit Count | Letters `"abc"` | Type validation - numbers only | ☐ |
| F2.1.6 | Unit Count | Negative number `-5` | Range validation - must be positive | ☐ |
| F2.1.7 | Unit Count | Decimal `"10.5"` | Integer validation or rounding | ☐ |
| F2.1.8 | Unit Count | Very large `999999999` | Reasonable max limit enforced | ☐ |
| F2.1.9 | Unit Count | Zero `0` | Validation - at least 1 unit | ☐ |
| F2.1.10 | Street Address | Empty when required | Required field validation | ☐ |
| F2.1.11 | City | Numbers only `"12345"` | Accept or validate format | ☐ |
| F2.1.12 | State | Invalid state code `"XX"` | Validation against valid states | ☐ |
| F2.1.13 | ZIP Code | Letters `"ABCDE"` | Format validation error | ☐ |
| F2.1.14 | ZIP Code | Too short `"123"` | Length validation (5 or 9 digits) | ☐ |
| F2.1.15 | ZIP Code | Special chars `"12-34"` | Format validation | ☐ |

#### Test Case F2.2: Contact Information - Invalid Input

**Location**: `/ReserveStudies/Request` - Step 2

| Test ID | Field | Invalid Input | Expected Result | Status |
|---------|-------|---------------|-----------------|--------|
| F2.2.1 | First Name | Empty | Required field error | ☐ |
| F2.2.2 | First Name | Numbers `"John123"` | Accept or alpha-only validation | ☐ |
| F2.2.3 | First Name | Special chars `"@#$%"` | Sanitized or rejected | ☐ |
| F2.2.4 | Last Name | Empty | Required field error | ☐ |
| F2.2.5 | Email | No @ symbol `"johnemail.com"` | Email format validation | ☐ |
| F2.2.6 | Email | No domain `"john@"` | Email format validation | ☐ |
| F2.2.7 | Email | Multiple @ `"john@@email.com"` | Email format validation | ☐ |
| F2.2.8 | Email | Spaces `"john doe@email.com"` | Email format validation | ☐ |
| F2.2.9 | Email | Empty | Required field error | ☐ |
| F2.2.10 | Phone | Letters `"abc-def-ghij"` | Phone format validation | ☐ |
| F2.2.11 | Phone | Too short `"123"` | Length validation | ☐ |
| F2.2.12 | Phone | Too long `"1234567890123456"` | Max length validation | ☐ |
| F2.2.13 | Phone | Special chars `"(123)@456-7890"` | Format validation | ☐ |
| F2.2.14 | Extension | Letters `"ext"` | Numbers only validation | ☐ |
| F2.2.15 | Extension | Negative `-123` | Positive numbers only | ☐ |

#### Test Case F2.3: Element Details - Invalid Input

**Location**: `/ReserveStudies/Request` - Step 3

| Test ID | Field | Invalid Input | Expected Result | Status |
|---------|-------|---------------|-----------------|--------|
| F2.3.1 | Quantity | Letters `"five"` | Type validation - numbers only | ☐ |
| F2.3.2 | Quantity | Negative `-10` | Must be positive | ☐ |
| F2.3.3 | Quantity | Zero `0` | Must be at least 1 | ☐ |
| F2.3.4 | Quantity | Decimal `5.5` | Integer validation or accept | ☐ |
| F2.3.5 | Last Replaced Date | Future date `2030-01-01` | Date must be in past | ☐ |
| F2.3.6 | Last Replaced Date | Invalid format `"13/45/2024"` | Date format validation | ☐ |
| F2.3.7 | Last Replaced Date | Text `"yesterday"` | Date format validation | ☐ |
| F2.3.8 | Last Replaced Date | Very old `1800-01-01` | Reasonable date range | ☐ |
| F2.3.9 | Useful Life Years | Letters `"twenty"` | Numbers only | ☐ |
| F2.3.10 | Useful Life Years | Negative `-5` | Must be positive | ☐ |
| F2.3.11 | Useful Life Years | Zero `0` | Must be at least 1 | ☐ |
| F2.3.12 | Useful Life Years | Unreasonable `500` | Max reasonable value | ☐ |
| F2.3.13 | Remaining Life | Greater than Useful Life | Remaining ≤ Useful Life | ☐ |
| F2.3.14 | Element without type | No element type selected | Required selection | ☐ |

#### Test Case F2.4: Financial Information - Invalid Input

**Location**: Financial Info Form

| Test ID | Field | Invalid Input | Expected Result | Status |
|---------|-------|---------------|-----------------|--------|
| F2.4.1 | Reserve Fund Balance | Letters `"fifty thousand"` | Currency/number validation | ☐ |
| F2.4.2 | Reserve Fund Balance | Negative `-50000` | Must be zero or positive | ☐ |
| F2.4.3 | Reserve Fund Balance | Special chars `$50,000` | Parse currency or reject | ☐ |
| F2.4.4 | Annual Contribution | Letters `"ten thousand"` | Number validation | ☐ |
| F2.4.5 | Annual Contribution | Negative `-1000` | Must be positive | ☐ |
| F2.4.6 | Annual Contribution | Zero `0` | Warn or allow | ☐ |
| F2.4.7 | Projected Expenses | Letters `"unknown"` | Number validation | ☐ |
| F2.4.8 | Fiscal Year Month | `0` or `13` | Valid 1-12 range | ☐ |
| F2.4.9 | Fiscal Year Month | Letters `"January"` | Number or accept month name | ☐ |
| F2.4.10 | All fields empty | Submit with no data | Required fields validation | ☐ |

#### Test Case F2.5: Proposal - Invalid Input

**Location**: Proposal Creation Form

| Test ID | Field | Invalid Input | Expected Result | Status |
|---------|-------|---------------|-----------------|--------|
| F2.5.1 | Estimated Cost | Letters `"five thousand"` | Currency validation | ☐ |
| F2.5.2 | Estimated Cost | Negative `-5000` | Must be positive | ☐ |
| F2.5.3 | Estimated Cost | Zero `0` | Warn or require positive | ☐ |
| F2.5.4 | Estimated Cost | Too large `999999999999` | Reasonable max | ☐ |
| F2.5.5 | Scope | Empty | Required field | ☐ |
| F2.5.6 | Scope | Only whitespace | Required field (after trim) | ☐ |
| F2.5.7 | Scope | > 10,000 characters | Max length validation | ☐ |
| F2.5.8 | Scope | Script tags | XSS sanitized | ☐ |
| F2.5.9 | Comments | > 5,000 characters | Max length validation | ☐ |

---

### Phase F3: Workflow State Transition Failures

#### Test Case F3.1: Invalid State Transitions

**Objective**: Verify state machine rejects invalid transitions

| Test ID | Current State | Attempted Transition | Expected Result | Status |
|---------|---------------|----------------------|-----------------|--------|
| F3.1.1 | NewRequest | → Complete | Transition denied | ☐ |
| F3.1.2 | NewRequest | → Archived | Transition denied | ☐ |
| F3.1.3 | PendingDetails | → InProgress | Transition denied | ☐ |
| F3.1.4 | Assigned | → Complete | Transition denied | ☐ |
| F3.1.5 | ProposalPendingESign | → Scheduled | Transition denied | ☐ |
| F3.1.6 | Rejected | → Accepted | Transition denied (terminal) | ☐ |
| F3.1.7 | Complete | → InProgress | Transition denied | ☐ |
| F3.1.8 | Archived | → Any state | Transition denied (terminal) | ☐ |
| F3.1.9 | InProgress | → NewRequest | Backward transition denied | ☐ |
| F3.1.10 | ReportDrafted | → Assigned | Backward transition denied | ☐ |

#### Test Case F3.2: State Transition Without Required Data

| Test ID | Transition | Missing Requirement | Expected Result | Status |
|---------|------------|---------------------|-----------------|--------|
| F3.2.1 | → Assigned | No specialist selected | Error: Specialist required | ☐ |
| F3.2.2 | → ProposalPendingESign | No proposal created | Error: Create proposal first | ☐ |
| F3.2.3 | → Accepted | No signature provided | Error: Signature required | ☐ |
| F3.2.4 | → Scheduled | No date selected | Error: Schedule date required | ☐ |
| F3.2.5 | → ReportDrafted | No report generated | Error: Generate report first | ☐ |
| F3.2.6 | → Complete | Report not approved | Error: QA approval required | ☐ |

#### Test Case F3.3: Concurrent State Modifications

| Test ID | Scenario | Expected Result | Status |
|---------|----------|-----------------|--------|
| F3.3.1 | Two users approve simultaneously | One succeeds, one gets concurrency error | ☐ |
| F3.3.2 | Edit while another saves | Concurrency conflict detected | ☐ |
| F3.3.3 | Delete while editing | Proper error handling | ☐ |
| F3.3.4 | Stale data submission | RowVersion check fails gracefully | ☐ |

---

### Phase F4: File Upload Failures

#### Test Case F4.1: Photo Upload Validation

**Location**: Site Visit Photos

| Test ID | Test Scenario | Input | Expected Result | Status |
|---------|---------------|-------|-----------------|--------|
| F4.1.1 | Wrong file type | .exe file | Rejected - image types only | ☐ |
| F4.1.2 | Wrong file type | .pdf file | Rejected or converted | ☐ |
| F4.1.3 | Wrong file type | .js file | Rejected - security | ☐ |
| F4.1.4 | File too large | 50MB image | Size limit error (e.g., max 10MB) | ☐ |
| F4.1.5 | File too small | 0 bytes | Invalid file error | ☐ |
| F4.1.6 | Corrupted image | Invalid JPEG data | Graceful error handling | ☐ |
| F4.1.7 | Renamed extension | .exe renamed to .jpg | Content-type validation | ☐ |
| F4.1.8 | Too many files | 100+ uploads at once | Rate limit or queue | ☐ |
| F4.1.9 | Special filename | `../../../etc/passwd.jpg` | Path traversal blocked | ☐ |
| F4.1.10 | Unicode filename | `图片.jpg` | Accepted or sanitized | ☐ |
| F4.1.11 | Empty filename | No name | Default name assigned | ☐ |
| F4.1.12 | Very long filename | 500+ characters | Truncated safely | ☐ |

#### Test Case F4.2: Report File Operations

| Test ID | Test Scenario | Expected Result | Status |
|---------|---------------|-----------------|--------|
| F4.2.1 | Download non-existent report | 404 or friendly error | ☐ |
| F4.2.2 | Download report from wrong tenant | Access denied | ☐ |
| F4.2.3 | Generate report with missing data | Clear error message | ☐ |
| F4.2.4 | Generate report during high load | Queued or timeout handling | ☐ |

---

### Phase F5: Tenant Isolation Failures

#### Test Case F5.1: Cross-Tenant Data Access Attempts

**Objective**: Verify complete data isolation between tenants

| Test ID | Attack Vector | Action | Expected Result | Status |
|---------|---------------|--------|-----------------|--------|
| F5.1.1 | URL manipulation | Change TenantId in URL query | Ignored - uses session tenant | ☐ |
| F5.1.2 | Form tampering | Submit form with different TenantId | Server overwrites with session tenant | ☐ |
| F5.1.3 | API request | Send TenantId in JSON body | Server ignores, uses context | ☐ |
| F5.1.4 | Community selection | Try to select other tenant's community | Community not visible in list | ☐ |
| F5.1.5 | Direct community ID | Submit CommunityId from other tenant | UnauthorizedAccessException | ☐ |
| F5.1.6 | User assignment | Assign user from different tenant | User not in selection list | ☐ |
| F5.1.7 | Search results | Search shows all tenants' data | Only current tenant data shown | ☐ |
| F5.1.8 | Report access | Guess report URL from other tenant | Access denied | ☐ |
| F5.1.9 | Subdomain switching | Login at tenant1, navigate to tenant2 | Forced logout | ☐ |
| F5.1.10 | Session hijacking | Use tenant1 session on tenant2 | Session invalid for tenant2 | ☐ |

---

### Phase F6: Database & System Failures

#### Test Case F6.1: Database Error Handling

| Test ID | Failure Scenario | Expected Behavior | Status |
|---------|------------------|-------------------|--------|
| F6.1.1 | Database connection timeout | User-friendly error, retry option | ☐ |
| F6.1.2 | Database unavailable | Maintenance page or cached read | ☐ |
| F6.1.3 | Constraint violation | Clear error, no partial save | ☐ |
| F6.1.4 | Duplicate key | Friendly message, suggest action | ☐ |
| F6.1.5 | Transaction rollback | All-or-nothing save confirmed | ☐ |
| F6.1.6 | Deadlock | Automatic retry or user notification | ☐ |

#### Test Case F6.2: External Service Failures

| Test ID | Service | Failure | Expected Behavior | Status |
|---------|---------|---------|-------------------|--------|
| F6.2.1 | Email service | SMTP unavailable | Queue for retry, notify admin | ☐ |
| F6.2.2 | Email service | Invalid recipient | Log error, continue workflow | ☐ |
| F6.2.3 | Blob storage | Upload fails | Retry or clear error message | ☐ |
| F6.2.4 | Blob storage | Download fails | Friendly error, retry option | ☐ |
| F6.2.5 | Stripe | Payment API down | Graceful degradation | ☐ |
| F6.2.6 | Stripe | Webhook timeout | Retry mechanism works | ☐ |

---

### Phase F7: Browser & Client-Side Failures

#### Test Case F7.1: Network Issues

| Test ID | Scenario | Expected Behavior | Status |
|---------|----------|-------------------|--------|
| F7.1.1 | Lost connection during save | Data preserved, retry on reconnect | ☐ |
| F7.1.2 | Slow connection (3G) | Loading indicators, no timeout | ☐ |
| F7.1.3 | SignalR disconnect | Auto-reconnect, state preserved | ☐ |
| F7.1.4 | Page refresh during save | Warning dialog, draft preserved | ☐ |
| F7.1.5 | Close browser during save | Auto-save or draft recovery | ☐ |

#### Test Case F7.2: Browser Compatibility

| Test ID | Browser/Scenario | Expected Behavior | Status |
|---------|------------------|-------------------|--------|
| F7.2.1 | JavaScript disabled | Graceful message, basic function | ☐ |
| F7.2.2 | Old browser (IE11) | Upgrade message or degraded mode | ☐ |
| F7.2.3 | Mobile Safari | Touch inputs work correctly | ☐ |
| F7.2.4 | Firefox private mode | Session/storage works | ☐ |
| F7.2.5 | Cookies disabled | Clear message about requirements | ☐ |

---

### Phase F8: Security Vulnerability Testing

#### Test Case F8.1: Injection Attacks

| Test ID | Attack Type | Test Input | Expected Result | Status |
|---------|-------------|------------|-----------------|--------|
| F8.1.1 | SQL Injection | `'; DROP TABLE ReserveStudies;--` | Input rejected or escaped | ☐ |
| F8.1.2 | SQL Injection | `1 OR 1=1` | No unauthorized data | ☐ |
| F8.1.3 | XSS Stored | `<script>document.cookie</script>` | Sanitized in display | ☐ |
| F8.1.4 | XSS Reflected | URL with `<script>` | Not executed | ☐ |
| F8.1.5 | LDAP Injection | `*)(uid=*))(|(uid=*` | Input rejected | ☐ |
| F8.1.6 | Command Injection | `; rm -rf /` | Not executed | ☐ |
| F8.1.7 | Path Traversal | `../../../etc/passwd` | Access denied | ☐ |
| F8.1.8 | XML Injection | XXE payload | Rejected | ☐ |

#### Test Case F8.2: CSRF & Request Forgery

| Test ID | Scenario | Expected Result | Status |
|---------|----------|-----------------|--------|
| F8.2.1 | CSRF token missing | Request rejected | ☐ |
| F8.2.2 | CSRF token invalid | Request rejected | ☐ |
| F8.2.3 | Replay old request | Token expired, rejected | ☐ |
| F8.2.4 | Cross-origin request | Blocked by CORS | ☐ |

#### Test Case F8.3: Data Exposure

| Test ID | Scenario | Expected Result | Status |
|---------|----------|-----------------|--------|
| F8.3.1 | Error messages | No stack traces in production | ☐ |
| F8.3.2 | API responses | No sensitive data leaked | ☐ |
| F8.3.3 | Browser dev tools | No secrets in HTML/JS | ☐ |
| F8.3.4 | URL parameters | No sensitive IDs exposed | ☐ |

---

### Phase F9: Boundary & Edge Case Testing

#### Test Case F9.1: Data Limits

| Test ID | Entity | Boundary | Test | Expected Result | Status |
|---------|--------|----------|------|-----------------|--------|
| F9.1.1 | Community Name | Max length | 200 chars exactly | Accepted | ☐ |
| F9.1.2 | Community Name | Over max | 201 chars | Rejected | ☐ |
| F9.1.3 | Elements | Max per study | 500 elements | Accepted or limited | ☐ |
| F9.1.4 | Elements | Zero | No elements | Validation required | ☐ |
| F9.1.5 | Photos | Max per study | 200 photos | Accepted or limited | ☐ |
| F9.1.6 | Notes | Max length | 10,000 chars | Accepted | ☐ |
| F9.1.7 | Cost | Max value | 999,999,999.99 | Accepted | ☐ |
| F9.1.8 | Cost | Min precision | 0.01 | Accepted | ☐ |
| F9.1.9 | Date | Min year | 1900-01-01 | Accepted or rejected | ☐ |
| F9.1.10 | Date | Max year | Current date | Accepted | ☐ |

#### Test Case F9.2: Empty State Handling

| Test ID | Scenario | Expected Display | Status |
|---------|----------|------------------|--------|
| F9.2.1 | No reserve studies | Empty state message | ☐ |
| F9.2.2 | No communities | Create first option | ☐ |
| F9.2.3 | No elements added | Warning before submit | ☐ |
| F9.2.4 | No photos uploaded | Allowed but noted | ☐ |
| F9.2.5 | No notifications | Empty list, not error | ☐ |
| F9.2.6 | No specialists available | Clear message | ☐ |

#### Test Case F9.3: Unicode & International

| Test ID | Scenario | Test Input | Expected Result | Status |
|---------|----------|------------|-----------------|--------|
| F9.3.1 | Unicode name | `José García` | Stored and displayed correctly | ☐ |
| F9.3.2 | Chinese chars | `王小明` | Stored and displayed correctly | ☐ |
| F9.3.3 | Emoji in notes | `Great condition! 👍` | Stored or filtered | ☐ |
| F9.3.4 | RTL text | Arabic text | Displayed correctly | ☐ |
| F9.3.5 | Mixed direction | `Hello مرحبا World` | Displayed correctly | ☐ |

---

### Failure Testing Checklist

#### Input Validation
- [ ] All required fields reject empty values
- [ ] Numeric fields reject letters
- [ ] Email fields validate format
- [ ] Phone fields validate format
- [ ] Date fields reject invalid dates
- [ ] Currency fields handle formatting
- [ ] Text fields have max length limits
- [ ] Dropdowns require selection

#### Security
- [ ] SQL injection attempts blocked
- [ ] XSS attempts sanitized
- [ ] CSRF tokens validated
- [ ] Path traversal blocked
- [ ] File upload types validated
- [ ] Authentication required on all protected pages
- [ ] Authorization checked for all actions

#### Tenant Isolation
- [ ] Users cannot see other tenants' data
- [ ] URL manipulation doesn't bypass isolation
- [ ] Form tampering doesn't bypass isolation
- [ ] API calls enforce tenant context

#### State Machine
- [ ] Invalid transitions rejected
- [ ] Required data checked before transitions
- [ ] Concurrent modifications handled
- [ ] Backward transitions blocked appropriately

#### Error Handling
- [ ] Database errors show friendly messages
- [ ] Network errors handled gracefully
- [ ] External service failures don't crash app
- [ ] Validation errors clearly displayed
- [ ] No stack traces in production

---

## Troubleshooting

### Common Issues

#### 1. Status Transition Fails
**Symptom**: "Transition not allowed" error

**Resolution**:
- Check current status in database
- Verify transition is in allowed transitions map
- Check user has appropriate role/permissions
- Review `StudyWorkflowService` logs

#### 2. Notifications Not Sending
**Symptom**: User doesn't receive expected email

**Resolution**:
- Check email service configuration
- Verify email address is correct
- Check notification queue/logs
- Verify SMTP settings

#### 3. Tenant Isolation Error
**Symptom**: "You cannot access this resource" error

**Resolution**:
- Verify user's TenantId matches resource TenantId
- Check subdomain resolution
- Verify role assignment includes correct TenantId

#### 4. Proposal Acceptance Fails
**Symptom**: Unable to accept proposal

**Resolution**:
- Verify proposal exists and is linked to study
- Check study status is `ProposalPendingESign`
- Verify user is HOA user for this study
- Check for valid signature data

#### 5. Report Generation Fails
**Symptom**: Error generating report

**Resolution**:
- Verify all required data is complete
- Check element assessments are filled
- Verify financial info is submitted
- Check report template exists

#### 6. Input Validation Not Triggering
**Symptom**: Invalid data saved to database

**Resolution**:
- Check model validation attributes ([Required], [Range], etc.)
- Verify Blazor form validation is enabled
- Check if server-side validation is bypassed
- Review FluentValidation rules if used

#### 7. Cross-Tenant Data Visible
**Symptom**: User sees data from another tenant

**Resolution**:
- Check global query filter in DbContext
- Verify ITenantContext is properly injected
- Check subdomain resolution middleware order
- Verify TenantId is set on all queries

#### 8. File Upload Fails
**Symptom**: Photos or documents don't upload

**Resolution**:
- Check file size limits in configuration
- Verify allowed file types list
- Check blob storage connection string
- Verify folder permissions (local storage)
- Check IIS/Kestrel request size limits

#### 9. Concurrent Edit Conflict
**Symptom**: "Data was modified by another user" error

**Resolution**:
- Expected behavior with optimistic concurrency
- Refresh and retry the operation
- Check RowVersion column is properly configured
- Implement merge or force-save if needed

#### 10. JavaScript/Blazor Errors
**Symptom**: UI becomes unresponsive or shows errors

**Resolution**:
- Check browser console for JS errors
- Verify SignalR connection is active
- Clear browser cache
- Check for component lifecycle issues
- Review for unhandled exceptions in Blazor code

---

## Test Completion Checklist

### Happy Path Testing

### Phase 1: Request Submission
- [ ] HOA user can login to tenant subdomain
- [ ] Request form loads with tenant branding
- [ ] Community can be created/selected
- [ ] Contact information saved
- [ ] Elements added successfully
- [ ] Request submitted successfully
- [ ] Confirmation email received
- [ ] Status = NewRequest

### Phase 2: Review & Assignment
- [ ] Tenant owner sees pending requests
- [ ] Request details viewable
- [ ] Can approve or request info
- [ ] Can assign specialist
- [ ] Specialist notified
- [ ] Status = Assigned

### Phase 3: Proposal
- [ ] Specialist can create proposal
- [ ] Proposal sent to HOA
- [ ] HOA can view proposal
- [ ] HOA can accept with e-signature
- [ ] Status = Accepted

### Phase 4: Financial Info
- [ ] Financial info request sent
- [ ] HOA can submit financial info
- [ ] Data saved correctly
- [ ] Specialist notified

### Phase 5: Site Visit
- [ ] Visit can be scheduled
- [ ] Photos can be uploaded
- [ ] Notes can be added
- [ ] Inspection marked complete
- [ ] Status = UnderReview

### Phase 6: Report & QA
- [ ] Draft report generated
- [ ] QA review completed
- [ ] Report approved
- [ ] Status = ApprovedReport

### Phase 7: Delivery
- [ ] Report finalized
- [ ] Report delivered to HOA
- [ ] HOA can download report
- [ ] Status = Complete

### Phase 8: Archival
- [ ] Study archived
- [ ] Renewal reminder scheduled
- [ ] Status = Archived

### Failure & Security Testing

### Phase F1: Authentication & Authorization
- [ ] Invalid logins handled correctly
- [ ] Account lockout works
- [ ] Authorization bypass prevented
- [ ] Cross-tenant access blocked

### Phase F2: Input Validation
- [ ] All numeric fields reject letters
- [ ] All required fields validated
- [ ] Email format validated
- [ ] Date fields reject invalid dates
- [ ] XSS attempts sanitized

### Phase F3: Workflow Transitions
- [ ] Invalid transitions rejected
- [ ] Missing data blocks transitions
- [ ] Concurrent modifications handled

### Phase F4: File Uploads
- [ ] Invalid file types rejected
- [ ] File size limits enforced
- [ ] Malicious filenames blocked

### Phase F5: Tenant Isolation
- [ ] URL manipulation blocked
- [ ] Form tampering blocked
- [ ] No cross-tenant data visible

### Phase F6-F7: System Resilience
- [ ] Database errors handled gracefully
- [ ] Network issues don't crash app
- [ ] External service failures managed

### Phase F8: Security
- [ ] SQL injection blocked
- [ ] XSS attacks prevented
- [ ] CSRF tokens validated

### Phase F9: Boundaries
- [ ] Max limits enforced
- [ ] Empty states displayed correctly
- [ ] Unicode characters handled

---

## Sign-Off

### Happy Path Testing

| Phase | Tested By | Date | Status |
|-------|-----------|------|--------|
| Phase 1 - Request Submission | | | ☐ Pass ☐ Fail |
| Phase 2 - Review & Assignment | | | ☐ Pass ☐ Fail |
| Phase 3 - Proposal | | | ☐ Pass ☐ Fail |
| Phase 4 - Financial Info | | | ☐ Pass ☐ Fail |
| Phase 5 - Site Visit | | | ☐ Pass ☐ Fail |
| Phase 6 - Report & QA | | | ☐ Pass ☐ Fail |
| Phase 7 - Delivery | | | ☐ Pass ☐ Fail |
| Phase 8 - Archival | | | ☐ Pass ☐ Fail |

### Failure & Security Testing

| Phase | Tested By | Date | Status |
|-------|-----------|------|--------|
| F1 - Auth Failures | | | ☐ Pass ☐ Fail |
| F2 - Input Validation | | | ☐ Pass ☐ Fail |
| F3 - Workflow Failures | | | ☐ Pass ☐ Fail |
| F4 - File Upload Failures | | | ☐ Pass ☐ Fail |
| F5 - Tenant Isolation | | | ☐ Pass ☐ Fail |
| F6 - Database Failures | | | ☐ Pass ☐ Fail |
| F7 - Browser Failures | | | ☐ Pass ☐ Fail |
| F8 - Security Testing | | | ☐ Pass ☐ Fail |
| F9 - Boundary Testing | | | ☐ Pass ☐ Fail |

**Overall Happy Path Result**: ☐ Pass ☐ Fail

**Overall Security/Failure Result**: ☐ Pass ☐ Fail

**Final Test Result**: ☐ Pass ☐ Fail

**Notes**:

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025 | ALX Reserve Cloud Team | Initial document - Happy path testing |
| 1.1 | 2025 | ALX Reserve Cloud Team | Added failure testing, security testing, input validation, edge cases |

---

**Document Version**: 1.1  
**Last Updated**: 2025  
**Author**: ALX Reserve Cloud Team
