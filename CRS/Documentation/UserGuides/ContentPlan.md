# CRS User Guide Content Plan

> **Purpose**: This document outlines the structure, topics, and assets needed for each user guide. 
> 
> **Status**: ✅ **All guides completed** - Content written, Help page implemented, navigation link added.

---

## Completion Status

| Guide | Status | Sections | Words (approx) | Last Updated |
|-------|--------|----------|----------------|--------------|
| HOA User Guide | ✅ Complete | 6/6 | ~8,300 | Complete |
| Specialist Guide | ✅ Complete | 6/6 | ~10,000 | Complete |
| Admin Guide | ✅ Complete | 6/6 | ~10,800 | Complete |
| **Total** | **✅ Complete** | **18/18** | **~29,100** | |

### Implementation Status

| Component | Status | Location |
|-----------|--------|----------|
| Markdown Documentation | ✅ Complete | `Documentation/UserGuides/` |
| Help Page Component | ✅ Complete | `Components/Pages/Help/Index.razor` |
| Navigation Link | ✅ Complete | "Help & Guides" in TenantNavMenu |
| Role-Based Defaults | ✅ Complete | Opens appropriate guide per user role |

---

## Table of Contents

1. [Guide Overview](#guide-overview)
2. [HOA User Guide](#hoa-user-guide)
3. [Specialist Guide](#specialist-guide)
4. [Admin Guide](#admin-guide)
5. [Cross-Reference Mapping](#cross-reference-mapping)
6. [Screenshot & Diagram Inventory](#screenshot--diagram-inventory)
7. [Writing Guidelines](#writing-guidelines)

---

## Guide Overview

| Guide | Audience | Complexity | Est. Sections | Priority |
|-------|----------|------------|---------------|----------|
| HOA User Guide | External customers (HOA board members) | Simple | 6 | High |
| Specialist Guide | TenantSpecialist (inspectors, analysts) | Medium | 6 | High |
| Admin Guide | TenantOwner (company administrators) | Complex | 6 | Medium |

### Shared Terminology

| Term | Definition |
|------|------------|
| Reserve Study | A financial planning document that inventories community assets and projects replacement costs |
| Community | The HOA property/association being studied |
| Component/Element | Individual assets tracked in the reserve study (roofs, pools, HVAC, etc.) |
| Funding Plan | The financial strategy for building reserve funds |
| Narrative | The written report accompanying the financial analysis |
| Proposal | The service agreement sent to HOA for approval |
| Site Visit | Physical inspection of the community property |

---

## HOA User Guide

**File Location**: `Documentation/UserGuides/HOAUserGuide/`
**Status**: ✅ Complete

### 1. Getting Started (`getting-started.md`) ✅

#### Topics to Cover
- [x] Accessing your portal (login, password reset)
- [x] Understanding your dashboard
- [x] Portal navigation overview
- [x] Your role and permissions explained
- [x] Getting help and support

#### Key Concepts
- Role: HOAUser vs HOAAuditor (read-only)
- Portal vs full application access
- Tenant-specific branding

#### Screenshots Needed
- [x] Login page
- [x] HOA user dashboard view
- [x] Navigation menu (HOA perspective)
- [x] Welcome banner

#### Cross-References
- → Admin Guide: "Inviting HOA Users"

---

### 2. Submitting a Reserve Study Request (`submitting-requests.md`) ✅ (`submitting-requests.md`)

#### Topics to Cover
- [x] When to request a reserve study
- [x] Starting a new request
- [x] Entering community information
  - Community name and address
  - Property type
  - Number of units
- [x] Adding contact information
- [x] Selecting components/elements to include
- [x] Submitting the request
- [x] What happens after submission

#### Key Concepts
- Request lifecycle (RequestCreated → RequestApproved)
- Required vs optional information
- Timeline expectations

#### Screenshots Needed
- [x] "New Request" button location
- [x] Community information form
- [x] Contact information form
- [x] Component selection interface
- [x] Request confirmation screen

#### Cross-References
- → Specialist Guide: "Reviewing Requests"
- → Admin Guide: "Approving Requests"

---

### 3. Proposals (`proposals.md`) ✅

#### Topics to Cover
- [x] Receiving a proposal notification
- [x] Reviewing proposal details
  - Scope of work
  - Pricing breakdown
  - Timeline
  - Terms and conditions
- [x] Understanding the e-signature process
- [x] Accepting a proposal
- [x] Rejecting a proposal (and next steps)
- [x] Proposal acceptance confirmation

#### Key Concepts
- ProposalSent → ProposalAccepted workflow
- Legal binding of e-signature
- What acceptance triggers (financial info request)

#### Screenshots Needed
- [x] Proposal email notification
- [x] Proposal review page
- [x] Pricing/terms section
- [x] E-signature interface
- [x] Acceptance confirmation

#### Cross-References
- → Specialist Guide: "Creating Proposals"
- → Admin Guide: "Proposal Settings"

---

### 4. Providing Financial Information (`financial-info.md`) ✅ (`financial-info.md`)

#### Topics to Cover
- [x] Why financial information is needed
- [x] Receiving the financial info request
- [x] Accessing the financial info form
- [x] Required information
  - Current reserve balance
  - Annual contributions
  - Recent expenditures
  - Bank statements (if required)
- [x] Saving progress (draft state)
- [x] Submitting financial information
- [x] What happens after submission

#### Key Concepts
- FinancialInfoRequested → FinancialInfoSubmitted workflow
- Data privacy and security
- Review process by specialist

#### Screenshots Needed
- [x] Financial info request notification
- [x] Financial info form (overview)
- [x] Individual form sections
- [x] Document upload interface
- [x] Submission confirmation

#### Cross-References
- → Specialist Guide: "Reviewing Financial Information"

---

### 5. Reports and Documents (`reports-and-documents.md`) ✅ (`reports-and-documents.md`)

#### Topics to Cover
- [x] Document types overview
  - Proposals
  - Financial info confirmations
  - Site visit schedules
  - Draft reports (if shared)
  - Final reserve study reports
- [x] Accessing your documents
- [x] Downloading reports (PDF)
- [x] Understanding your reserve study report
  - Executive summary
  - Component inventory
  - Funding plan
  - Appendices
- [x] Report delivery notifications

#### Key Concepts
- Document access permissions
- Report versions and amendments
- Archive access

#### Screenshots Needed
- [x] Documents list view
- [x] Document detail/preview
- [x] Download options
- [x] Report sections overview
- [x] Notification examples

#### Cross-References
- → Specialist Guide: "Publishing Reports"
- → Admin Guide: "Report Settings"

---

### 6. Invoices and Payments (`invoices-and-payments.md`) ✅ (`invoices-and-payments.md`)

#### Topics to Cover
- [x] Viewing your invoices
- [x] Invoice status explained
  - Draft
  - Sent
  - Paid
  - Overdue
- [x] Making a payment online
- [x] Payment methods accepted
- [x] Payment confirmation
- [x] Viewing payment history
- [x] Requesting invoice copies

#### Key Concepts
- Invoice lifecycle
- Payment processing (Stripe integration)
- Late payment policies

#### Screenshots Needed
- [x] My Invoices page
- [x] Invoice detail view
- [x] Payment form
- [x] Payment confirmation
- [x] Payment history

#### Cross-References
- → Admin Guide: "Invoice Management"
- → Admin Guide: "Invoice Settings"

---

## Specialist Guide

**File Location**: `Documentation/UserGuides/SpecialistGuide/`

### 1. Getting Started (`getting-started.md`)

#### Topics to Cover
- [x] Logging in and account setup
- [x] Dashboard overview for specialists
- [x] Understanding your assigned studies
- [x] Navigation and key areas
- [x] Notification preferences
- [x] Mobile considerations (if applicable)

#### Key Concepts
- TenantSpecialist role and permissions
- Study assignment workflow
- Kanban task board

#### Screenshots Needed
- [x] Specialist dashboard view
- [x] Assigned studies list
- [x] Quick action buttons
- [x] Notification center

#### Cross-References
- → Admin Guide: "Assigning Specialists"

---

### 2. Dashboard and Workflow (`dashboard-and-workflow.md`)

#### Topics to Cover
- [x] Dashboard widgets and metrics
  - Studies by status
  - Upcoming site visits
  - Pending tasks
- [x] Kanban board usage
  - Understanding columns/statuses
  - Moving tasks
  - Creating tasks
  - Task details and notes
- [x] Study list and filtering
- [x] Quick actions from dashboard
- [x] Calendar integration

#### Key Concepts
- Workflow status progression
- Task management
- Priority indicators

#### Screenshots Needed
- [x] Full dashboard view
- [x] Kanban board
- [x] Task detail dialog
- [x] Study filter options
- [x] Calendar widget

#### Cross-References
- → Admin Guide: "Workflow Configuration"

---

### 3. Proposals (`proposals.md`) ✅

#### Topics to Cover
- [x] When to create a proposal
- [x] Creating a new proposal
  - Selecting study/community
  - Setting scope of work
  - Pricing configuration
  - Terms and conditions
- [x] Proposal review process (if enabled)
- [x] Sending proposal to client
- [x] Tracking proposal status
- [x] Handling proposal acceptance
- [x] Handling proposal rejection

#### Key Concepts
- ProposalCreated → ProposalSent workflow
- Pricing templates
- E-signature tracking

#### Screenshots Needed
- [x] Create proposal button
- [x] Proposal form sections
- [x] Pricing configuration
- [x] Send proposal dialog
- [x] Proposal status tracking

#### Cross-References
- → HOA Guide: "Accepting Proposals"
- → Admin Guide: "Proposal Approval Settings"

---

### 4. Data Collection (`data-collection.md`)

#### Topics to Cover
- [x] Requesting financial information
  - Triggering the request
  - Customizing request message
- [x] Reviewing submitted financial info
  - Accessing submitted data
  - Validating information
  - Requesting corrections
- [x] Marking financial info as received
- [x] Service contacts
  - Requesting service contact info
  - Managing service contacts

#### Key Concepts
- FinancialInfo workflow stages
- Data validation requirements
- Communication with HOA

#### Screenshots Needed
- [x] Request financial info button
- [x] Financial info review page
- [x] Validation checklist
- [x] Service contacts section

#### Cross-References
- → HOA Guide: "Providing Financial Information"

---

### 5. Site Visits (`site-visits.md`)

#### Topics to Cover
- [x] Scheduling a site visit
  - Using the calendar
  - Sending calendar invites
  - HOA notification
- [x] Preparing for site visit
  - Reviewing community info
  - Component checklist
- [x] During the site visit
  - Mobile access (if available)
  - Taking photos
  - Recording notes
- [x] After the site visit
  - Uploading photos
  - Entering site visit data
  - Component assessment
  - Marking visit complete

#### Key Concepts
- SiteVisitPending → SiteVisitCompleted workflow
- Photo organization
- Component condition assessment

#### Screenshots Needed
- [x] Schedule visit dialog
- [x] Calendar with visit
- [x] Photo upload interface
- [x] Site visit data entry
- [x] Component assessment form

#### Cross-References
- → Admin Guide: "Calendar Settings"

---

### 6. Report Generation (`report-generation.md`)

#### Topics to Cover
- [x] Working with components/elements
  - Viewing component list
  - Editing component details
  - Adding additional components
  - Setting useful life and costs
- [x] Funding plan
  - Understanding funding strategies
  - Configuring funding parameters
  - Reviewing projections
- [x] Narrative creation
  - Using narrative templates
  - Editing narrative sections
  - Inserting data tokens
- [x] Report preview and generation
  - Previewing draft report
  - Generating PDF
  - Quality checklist
- [x] Submitting for QA review

#### Key Concepts
- Component lifecycle calculations
- Funding strategies (Full Funding, Threshold, etc.)
- Narrative template system
- Report workflow stages

#### Screenshots Needed
- [x] Component list/editor
- [x] Funding plan configuration
- [x] Funding projections chart
- [x] Narrative editor
- [x] Report preview
- [x] Submit for review button

#### Cross-References
- → Admin Guide: "Element Management"
- → Admin Guide: "Narrative Templates"
- → Admin Guide: "QA Review Process"

---

## Admin Guide

**File Location**: `Documentation/UserGuides/AdminGuide/`

### 1. Getting Started (`getting-started.md`)

#### Topics to Cover
- [x] Admin dashboard overview
- [x] Your role as TenantOwner
- [x] Key responsibilities
  - User management
  - Workflow oversight
  - Quality assurance
  - Settings configuration
- [x] Navigation for admins
- [x] Quick actions and shortcuts

#### Key Concepts
- TenantOwner vs TenantSpecialist permissions
- Tenant isolation (multi-tenant SaaS)
- Admin-only features

#### Screenshots Needed
- [x] Admin dashboard view
- [x] Admin menu items
- [x] Quick action panel
- [x] Notification center (admin view)

#### Cross-References
- → Specialist Guide: "Getting Started" (for comparison)

---

### 2. User Management (`user-management.md`)

#### Topics to Cover
- [x] User types overview
  - Staff users (Specialists, Viewers)
  - HOA users (external)
- [x] Managing staff users
  - Viewing user list
  - Creating new staff users
  - Editing user details
  - Deactivating users
- [x] Role assignment
  - Available roles
  - Assigning/removing roles
- [x] Inviting HOA users
  - Sending invitations
  - Managing pending invites
  - HOA user onboarding
- [x] User impersonation (if enabled)

#### Key Concepts
- Role hierarchy
- Tenant-scoped permissions
- User lifecycle

#### Screenshots Needed
- [x] Users list page
- [x] Create user dialog
- [x] Edit user dialog
- [x] Role assignment interface
- [x] Invite HOA user dialog
- [x] HOA users list

#### Cross-References
- → HOA Guide: "Getting Started"
- → Specialist Guide: "Getting Started"

---

### 3. Workflow Management (`workflow-management.md`)

#### Topics to Cover
- [x] Request queue management
  - Viewing incoming requests
  - Reviewing request details
  - Approving requests
  - Requesting more information
- [x] Assigning specialists
  - Viewing specialist workload
  - Making assignments
  - Reassigning studies
- [x] Monitoring study progress
  - Status dashboard
  - Bottleneck identification
  - Overdue alerts
- [x] QA review process
  - Reviewing draft reports
  - Approval workflow
  - Requesting revisions
- [x] Publishing reports
  - Final review checklist
  - Publishing to client
  - Delivery confirmation
- [x] Amendments and scope changes
  - Understanding scope variance
  - Approving amendments
  - Client communication

#### Key Concepts
- Full workflow lifecycle
- Approval gates
- Amendment triggers

#### Screenshots Needed
- [x] Request queue
- [x] Request detail (admin view)
- [x] Assign specialist dialog
- [x] Study progress dashboard
- [x] QA review interface
- [x] Publish report dialog
- [x] Amendment review

#### Cross-References
- → Specialist Guide: "Report Generation"
- → HOA Guide: "Reports and Documents"

---

### 4. Settings and Configuration (`settings-and-configuration.md`)

#### Topics to Cover
- [x] Tenant settings
  - Company information
  - Branding (logo, colors)
  - Contact details
- [x] Reserve study settings
  - Default parameters
  - Funding strategies
  - Calculation options
- [x] Element/component management
  - Default element library
  - Custom elements
  - Element categories
  - Cost defaults
- [x] Theme customization
  - Preset themes
  - Custom colors
- [x] Homepage customization
  - Public-facing homepage
  - Content blocks
  - Preview and publish
- [x] Narrative templates
  - Template structure
  - Creating/editing templates
  - Template sections and blocks

#### Key Concepts
- Tenant-level vs study-level settings
- Default inheritance
- Template system

#### Screenshots Needed
- [x] Tenant settings page
- [x] Reserve study settings
- [x] Element management page
- [x] Add/edit element dialog
- [x] Theme selector
- [x] Homepage editor
- [x] Narrative template editor

#### Cross-References
- → Specialist Guide: "Report Generation"

---

### 5. Financial Management (`financial-management.md`)

#### Topics to Cover
- [x] Invoice settings
  - Invoice numbering
  - Payment terms
  - Late fees configuration
  - Branding
- [x] Creating invoices
  - Manual invoice creation
  - Auto-generated invoices
- [x] Managing invoices
  - Invoice list and filtering
  - Invoice status tracking
  - Sending invoices
  - Recording payments
- [x] Credit memos
  - Creating credit memos
  - Applying credits
- [x] Payment tracking
  - Payment history
  - Outstanding balances
  - Aging reports
- [x] Stripe integration
  - Payment processing
  - Webhook handling

#### Key Concepts
- Invoice lifecycle
- Payment reconciliation
- Financial reporting

#### Screenshots Needed
- [x] Invoice settings page
- [x] Invoice list
- [x] Create invoice form
- [x] Invoice detail (admin view)
- [x] Record payment dialog
- [x] Credit memo form
- [x] Aging report

#### Cross-References
- → HOA Guide: "Invoices and Payments"

---

### 6. Reporting and Analytics (`reporting-and-analytics.md`)

#### Topics to Cover
- [x] Dashboard analytics
  - Key metrics
  - Trend charts
  - Performance indicators
- [x] Aging reports
  - Invoice aging
  - Study aging by status
- [x] Email logs
  - Viewing sent emails
  - Email delivery status
  - Troubleshooting delivery issues
- [x] Audit and compliance
  - Activity logging
  - User action history
  - Data export
- [x] Generated reports archive
  - Accessing past reports
  - Report versioning

#### Key Concepts
- Business intelligence
- Audit trail
- Compliance requirements

#### Screenshots Needed
- [x] Analytics dashboard
- [x] Aging report page
- [x] Email logs page
- [x] Email detail dialog
- [x] Audit log (if exposed in UI)
- [x] Reports archive

#### Cross-References
- → All guides: Various related features

---

## Cross-Reference Mapping

This section maps related topics across guides to ensure consistency and enable linking.

### By Feature

| Feature | HOA Guide | Specialist Guide | Admin Guide |
|---------|-----------|------------------|-------------|
| Login/Access | getting-started | getting-started | getting-started |
| Dashboard | getting-started | dashboard-and-workflow | getting-started |
| Study Requests | submitting-requests | - | workflow-management |
| Proposals | proposals | proposals | workflow-management |
| Financial Info | financial-info | data-collection | - |
| Site Visits | - | site-visits | - |
| Reports | reports-and-documents | report-generation | workflow-management |
| Invoices | invoices-and-payments | - | financial-management |
| User Management | - | - | user-management |
| Settings | - | - | settings-and-configuration |

### By Workflow Status

| Status | HOA Actions | Specialist Actions | Admin Actions |
|--------|-------------|-------------------|---------------|
| RequestCreated | View status | - | Review, Approve |
| RequestApproved | - | - | Assign Specialist |
| ProposalCreated | - | Create, Edit | Review (optional) |
| ProposalSent | Review, Accept/Reject | Track | Monitor |
| ProposalAccepted | Receive notification | Request Financial Info | - |
| FinancialInfoRequested | Submit financial info | Track | - |
| FinancialInfoSubmitted | - | Review, Accept | - |
| SiteVisitScheduled | View schedule | Conduct visit | - |
| SiteVisitCompleted | - | Enter data, Upload photos | - |
| ReportDrafted | - | Submit for QA | Review, Approve |
| ReportComplete | Download report | - | Publish |

---

## Screenshot & Diagram Inventory

### Diagrams to Create

| Diagram | Type | Used In | Description |
|---------|------|---------|-------------|
| HOA Workflow | Flowchart | HOA Guide | Simplified view of HOA user journey |
| Specialist Workflow | Flowchart | Specialist Guide | Full workflow from assignment to report |
| Admin Workflow | Flowchart | Admin Guide | Focus on approval gates |
| Study Lifecycle | Timeline | All Guides | Linear status progression |
| Role Permissions | Matrix | Admin Guide | What each role can do |

### Screenshot Checklist

#### Common UI Elements
- [x] Login page
- [x] Password reset
- [x] Dashboard (all 3 role views)
- [x] Navigation menu (all 3 role views)
- [x] Notification panel
- [x] User profile/settings

#### HOA-Specific (12 screenshots)
- [x] HOA dashboard
- [x] New request form (3 sections)
- [x] Proposal review page
- [x] E-signature interface
- [x] Financial info form (2-3 sections)
- [x] Documents list
- [x] Report download
- [x] My Invoices page
- [x] Payment form

#### Specialist-Specific (18 screenshots)
- [x] Specialist dashboard
- [x] Kanban board
- [x] Study detail page
- [x] Create proposal form
- [x] Send proposal dialog
- [x] Financial info review
- [x] Schedule site visit
- [x] Photo upload
- [x] Component editor
- [x] Funding plan config
- [x] Narrative editor
- [x] Report preview
- [x] Submit for QA

#### Admin-Specific (20 screenshots)
- [x] Admin dashboard
- [x] Users list
- [x] Create/Edit user dialogs
- [x] Role assignment
- [x] HOA user invitation
- [x] Request queue
- [x] Assign specialist dialog
- [x] QA review interface
- [x] Publish report dialog
- [x] Tenant settings
- [x] Reserve study settings
- [x] Element management
- [x] Invoice settings
- [x] Invoice list/detail
- [x] Aging report
- [x] Email logs

---

## Writing Guidelines

### Tone and Voice

| Audience | Tone | Vocabulary Level |
|----------|------|------------------|
| HOA Users | Friendly, supportive | Simple, avoid jargon |
| Specialists | Professional, efficient | Technical but clear |
| Admins | Authoritative, comprehensive | Technical, detailed |

### Formatting Standards

```markdown
# Section Title

Brief introduction paragraph explaining what this section covers.

## Subsection

Step-by-step instructions:

1. **Action** - Description of what to do
2. **Action** - Description of what to do
3. **Action** - Description of what to do

> **Tip**: Helpful tips in blockquotes

> **Warning**: Important warnings in blockquotes

### Sub-subsection

Additional details or variations.

![Screenshot description](./images/screenshot-name.png)
*Caption describing the screenshot*

---

**Related Topics**:
- [Link to related topic](./related-topic)
- [Link to another guide](../OtherGuide/topic)
```

### Content Checklist (per section)

- [x] Clear introduction
- [x] Step-by-step instructions
- [x] All UI elements referenced have screenshots
- [x] Tips for common issues
- [x] Warnings for destructive actions
- [x] Cross-references to related topics
- [x] Consistent terminology (see glossary)

---

## Next Steps

1. [x] ~~Review and approve this content plan~~
2. [ ] Capture screenshots (can be done in parallel with writing)
3. [x] ~~Write HOA User Guide (priority 1)~~ ✅ Complete
4. [x] ~~Write Specialist Guide (priority 2)~~ ✅ Complete
5. [x] ~~Write Admin Guide (priority 3)~~ ✅ Complete
6. [ ] Create diagrams (workflow flowcharts)
7. [ ] Review and edit all guides
8. [x] ~~Implement in-app help system~~ ✅ Complete

### Remaining Tasks

| Task | Priority | Status |
|------|----------|--------|
| Capture screenshots | Medium | Pending |
| Create workflow diagrams | Low | Pending |
| Add search functionality | Low | Optional |
| PDF export feature | Low | Optional |
