# CRS User Guide Content Plan

> **Purpose**: This document outlines the structure, topics, and assets needed for each user guide before content creation begins.

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

### 1. Getting Started (`getting-started.md`)

#### Topics to Cover
- [ ] Accessing your portal (login, password reset)
- [ ] Understanding your dashboard
- [ ] Portal navigation overview
- [ ] Your role and permissions explained
- [ ] Getting help and support

#### Key Concepts
- Role: HOAUser vs HOAAuditor (read-only)
- Portal vs full application access
- Tenant-specific branding

#### Screenshots Needed
- [ ] Login page
- [ ] HOA user dashboard view
- [ ] Navigation menu (HOA perspective)
- [ ] Welcome banner

#### Cross-References
- → Admin Guide: "Inviting HOA Users"

---

### 2. Submitting a Reserve Study Request (`submitting-requests.md`)

#### Topics to Cover
- [ ] When to request a reserve study
- [ ] Starting a new request
- [ ] Entering community information
  - Community name and address
  - Property type
  - Number of units
- [ ] Adding contact information
- [ ] Selecting components/elements to include
- [ ] Submitting the request
- [ ] What happens after submission

#### Key Concepts
- Request lifecycle (RequestCreated → RequestApproved)
- Required vs optional information
- Timeline expectations

#### Screenshots Needed
- [ ] "New Request" button location
- [ ] Community information form
- [ ] Contact information form
- [ ] Component selection interface
- [ ] Request confirmation screen

#### Cross-References
- → Specialist Guide: "Reviewing Requests"
- → Admin Guide: "Approving Requests"

---

### 3. Proposals (`proposals.md`)

#### Topics to Cover
- [ ] Receiving a proposal notification
- [ ] Reviewing proposal details
  - Scope of work
  - Pricing breakdown
  - Timeline
  - Terms and conditions
- [ ] Understanding the e-signature process
- [ ] Accepting a proposal
- [ ] Rejecting a proposal (and next steps)
- [ ] Proposal acceptance confirmation

#### Key Concepts
- ProposalSent → ProposalAccepted workflow
- Legal binding of e-signature
- What acceptance triggers (financial info request)

#### Screenshots Needed
- [ ] Proposal email notification
- [ ] Proposal review page
- [ ] Pricing/terms section
- [ ] E-signature interface
- [ ] Acceptance confirmation

#### Cross-References
- → Specialist Guide: "Creating Proposals"
- → Admin Guide: "Proposal Settings"

---

### 4. Providing Financial Information (`financial-info.md`)

#### Topics to Cover
- [ ] Why financial information is needed
- [ ] Receiving the financial info request
- [ ] Accessing the financial info form
- [ ] Required information
  - Current reserve balance
  - Annual contributions
  - Recent expenditures
  - Bank statements (if required)
- [ ] Saving progress (draft state)
- [ ] Submitting financial information
- [ ] What happens after submission

#### Key Concepts
- FinancialInfoRequested → FinancialInfoSubmitted workflow
- Data privacy and security
- Review process by specialist

#### Screenshots Needed
- [ ] Financial info request notification
- [ ] Financial info form (overview)
- [ ] Individual form sections
- [ ] Document upload interface
- [ ] Submission confirmation

#### Cross-References
- → Specialist Guide: "Reviewing Financial Information"

---

### 5. Reports and Documents (`reports-and-documents.md`)

#### Topics to Cover
- [ ] Document types overview
  - Proposals
  - Financial info confirmations
  - Site visit schedules
  - Draft reports (if shared)
  - Final reserve study reports
- [ ] Accessing your documents
- [ ] Downloading reports (PDF)
- [ ] Understanding your reserve study report
  - Executive summary
  - Component inventory
  - Funding plan
  - Appendices
- [ ] Report delivery notifications

#### Key Concepts
- Document access permissions
- Report versions and amendments
- Archive access

#### Screenshots Needed
- [ ] Documents list view
- [ ] Document detail/preview
- [ ] Download options
- [ ] Report sections overview
- [ ] Notification examples

#### Cross-References
- → Specialist Guide: "Publishing Reports"
- → Admin Guide: "Report Settings"

---

### 6. Invoices and Payments (`invoices-and-payments.md`)

#### Topics to Cover
- [ ] Viewing your invoices
- [ ] Invoice status explained
  - Draft
  - Sent
  - Paid
  - Overdue
- [ ] Making a payment online
- [ ] Payment methods accepted
- [ ] Payment confirmation
- [ ] Viewing payment history
- [ ] Requesting invoice copies

#### Key Concepts
- Invoice lifecycle
- Payment processing (Stripe integration)
- Late payment policies

#### Screenshots Needed
- [ ] My Invoices page
- [ ] Invoice detail view
- [ ] Payment form
- [ ] Payment confirmation
- [ ] Payment history

#### Cross-References
- → Admin Guide: "Invoice Management"
- → Admin Guide: "Invoice Settings"

---

## Specialist Guide

**File Location**: `Documentation/UserGuides/SpecialistGuide/`

### 1. Getting Started (`getting-started.md`)

#### Topics to Cover
- [ ] Logging in and account setup
- [ ] Dashboard overview for specialists
- [ ] Understanding your assigned studies
- [ ] Navigation and key areas
- [ ] Notification preferences
- [ ] Mobile considerations (if applicable)

#### Key Concepts
- TenantSpecialist role and permissions
- Study assignment workflow
- Kanban task board

#### Screenshots Needed
- [ ] Specialist dashboard view
- [ ] Assigned studies list
- [ ] Quick action buttons
- [ ] Notification center

#### Cross-References
- → Admin Guide: "Assigning Specialists"

---

### 2. Dashboard and Workflow (`dashboard-and-workflow.md`)

#### Topics to Cover
- [ ] Dashboard widgets and metrics
  - Studies by status
  - Upcoming site visits
  - Pending tasks
- [ ] Kanban board usage
  - Understanding columns/statuses
  - Moving tasks
  - Creating tasks
  - Task details and notes
- [ ] Study list and filtering
- [ ] Quick actions from dashboard
- [ ] Calendar integration

#### Key Concepts
- Workflow status progression
- Task management
- Priority indicators

#### Screenshots Needed
- [ ] Full dashboard view
- [ ] Kanban board
- [ ] Task detail dialog
- [ ] Study filter options
- [ ] Calendar widget

#### Cross-References
- → Admin Guide: "Workflow Configuration"

---

### 3. Proposals (`proposals.md`)

#### Topics to Cover
- [ ] When to create a proposal
- [ ] Creating a new proposal
  - Selecting study/community
  - Setting scope of work
  - Pricing configuration
  - Terms and conditions
- [ ] Proposal review process (if enabled)
- [ ] Sending proposal to client
- [ ] Tracking proposal status
- [ ] Handling proposal acceptance
- [ ] Handling proposal rejection

#### Key Concepts
- ProposalCreated → ProposalSent workflow
- Pricing templates
- E-signature tracking

#### Screenshots Needed
- [ ] Create proposal button
- [ ] Proposal form sections
- [ ] Pricing configuration
- [ ] Send proposal dialog
- [ ] Proposal status tracking

#### Cross-References
- → HOA Guide: "Accepting Proposals"
- → Admin Guide: "Proposal Approval Settings"

---

### 4. Data Collection (`data-collection.md`)

#### Topics to Cover
- [ ] Requesting financial information
  - Triggering the request
  - Customizing request message
- [ ] Reviewing submitted financial info
  - Accessing submitted data
  - Validating information
  - Requesting corrections
- [ ] Marking financial info as received
- [ ] Service contacts
  - Requesting service contact info
  - Managing service contacts

#### Key Concepts
- FinancialInfo workflow stages
- Data validation requirements
- Communication with HOA

#### Screenshots Needed
- [ ] Request financial info button
- [ ] Financial info review page
- [ ] Validation checklist
- [ ] Service contacts section

#### Cross-References
- → HOA Guide: "Providing Financial Information"

---

### 5. Site Visits (`site-visits.md`)

#### Topics to Cover
- [ ] Scheduling a site visit
  - Using the calendar
  - Sending calendar invites
  - HOA notification
- [ ] Preparing for site visit
  - Reviewing community info
  - Component checklist
- [ ] During the site visit
  - Mobile access (if available)
  - Taking photos
  - Recording notes
- [ ] After the site visit
  - Uploading photos
  - Entering site visit data
  - Component assessment
  - Marking visit complete

#### Key Concepts
- SiteVisitPending → SiteVisitCompleted workflow
- Photo organization
- Component condition assessment

#### Screenshots Needed
- [ ] Schedule visit dialog
- [ ] Calendar with visit
- [ ] Photo upload interface
- [ ] Site visit data entry
- [ ] Component assessment form

#### Cross-References
- → Admin Guide: "Calendar Settings"

---

### 6. Report Generation (`report-generation.md`)

#### Topics to Cover
- [ ] Working with components/elements
  - Viewing component list
  - Editing component details
  - Adding additional components
  - Setting useful life and costs
- [ ] Funding plan
  - Understanding funding strategies
  - Configuring funding parameters
  - Reviewing projections
- [ ] Narrative creation
  - Using narrative templates
  - Editing narrative sections
  - Inserting data tokens
- [ ] Report preview and generation
  - Previewing draft report
  - Generating PDF
  - Quality checklist
- [ ] Submitting for QA review

#### Key Concepts
- Component lifecycle calculations
- Funding strategies (Full Funding, Threshold, etc.)
- Narrative template system
- Report workflow stages

#### Screenshots Needed
- [ ] Component list/editor
- [ ] Funding plan configuration
- [ ] Funding projections chart
- [ ] Narrative editor
- [ ] Report preview
- [ ] Submit for review button

#### Cross-References
- → Admin Guide: "Element Management"
- → Admin Guide: "Narrative Templates"
- → Admin Guide: "QA Review Process"

---

## Admin Guide

**File Location**: `Documentation/UserGuides/AdminGuide/`

### 1. Getting Started (`getting-started.md`)

#### Topics to Cover
- [ ] Admin dashboard overview
- [ ] Your role as TenantOwner
- [ ] Key responsibilities
  - User management
  - Workflow oversight
  - Quality assurance
  - Settings configuration
- [ ] Navigation for admins
- [ ] Quick actions and shortcuts

#### Key Concepts
- TenantOwner vs TenantSpecialist permissions
- Tenant isolation (multi-tenant SaaS)
- Admin-only features

#### Screenshots Needed
- [ ] Admin dashboard view
- [ ] Admin menu items
- [ ] Quick action panel
- [ ] Notification center (admin view)

#### Cross-References
- → Specialist Guide: "Getting Started" (for comparison)

---

### 2. User Management (`user-management.md`)

#### Topics to Cover
- [ ] User types overview
  - Staff users (Specialists, Viewers)
  - HOA users (external)
- [ ] Managing staff users
  - Viewing user list
  - Creating new staff users
  - Editing user details
  - Deactivating users
- [ ] Role assignment
  - Available roles
  - Assigning/removing roles
- [ ] Inviting HOA users
  - Sending invitations
  - Managing pending invites
  - HOA user onboarding
- [ ] User impersonation (if enabled)

#### Key Concepts
- Role hierarchy
- Tenant-scoped permissions
- User lifecycle

#### Screenshots Needed
- [ ] Users list page
- [ ] Create user dialog
- [ ] Edit user dialog
- [ ] Role assignment interface
- [ ] Invite HOA user dialog
- [ ] HOA users list

#### Cross-References
- → HOA Guide: "Getting Started"
- → Specialist Guide: "Getting Started"

---

### 3. Workflow Management (`workflow-management.md`)

#### Topics to Cover
- [ ] Request queue management
  - Viewing incoming requests
  - Reviewing request details
  - Approving requests
  - Requesting more information
- [ ] Assigning specialists
  - Viewing specialist workload
  - Making assignments
  - Reassigning studies
- [ ] Monitoring study progress
  - Status dashboard
  - Bottleneck identification
  - Overdue alerts
- [ ] QA review process
  - Reviewing draft reports
  - Approval workflow
  - Requesting revisions
- [ ] Publishing reports
  - Final review checklist
  - Publishing to client
  - Delivery confirmation
- [ ] Amendments and scope changes
  - Understanding scope variance
  - Approving amendments
  - Client communication

#### Key Concepts
- Full workflow lifecycle
- Approval gates
- Amendment triggers

#### Screenshots Needed
- [ ] Request queue
- [ ] Request detail (admin view)
- [ ] Assign specialist dialog
- [ ] Study progress dashboard
- [ ] QA review interface
- [ ] Publish report dialog
- [ ] Amendment review

#### Cross-References
- → Specialist Guide: "Report Generation"
- → HOA Guide: "Reports and Documents"

---

### 4. Settings and Configuration (`settings-and-configuration.md`)

#### Topics to Cover
- [ ] Tenant settings
  - Company information
  - Branding (logo, colors)
  - Contact details
- [ ] Reserve study settings
  - Default parameters
  - Funding strategies
  - Calculation options
- [ ] Element/component management
  - Default element library
  - Custom elements
  - Element categories
  - Cost defaults
- [ ] Theme customization
  - Preset themes
  - Custom colors
- [ ] Homepage customization
  - Public-facing homepage
  - Content blocks
  - Preview and publish
- [ ] Narrative templates
  - Template structure
  - Creating/editing templates
  - Template sections and blocks

#### Key Concepts
- Tenant-level vs study-level settings
- Default inheritance
- Template system

#### Screenshots Needed
- [ ] Tenant settings page
- [ ] Reserve study settings
- [ ] Element management page
- [ ] Add/edit element dialog
- [ ] Theme selector
- [ ] Homepage editor
- [ ] Narrative template editor

#### Cross-References
- → Specialist Guide: "Report Generation"

---

### 5. Financial Management (`financial-management.md`)

#### Topics to Cover
- [ ] Invoice settings
  - Invoice numbering
  - Payment terms
  - Late fees configuration
  - Branding
- [ ] Creating invoices
  - Manual invoice creation
  - Auto-generated invoices
- [ ] Managing invoices
  - Invoice list and filtering
  - Invoice status tracking
  - Sending invoices
  - Recording payments
- [ ] Credit memos
  - Creating credit memos
  - Applying credits
- [ ] Payment tracking
  - Payment history
  - Outstanding balances
  - Aging reports
- [ ] Stripe integration
  - Payment processing
  - Webhook handling

#### Key Concepts
- Invoice lifecycle
- Payment reconciliation
- Financial reporting

#### Screenshots Needed
- [ ] Invoice settings page
- [ ] Invoice list
- [ ] Create invoice form
- [ ] Invoice detail (admin view)
- [ ] Record payment dialog
- [ ] Credit memo form
- [ ] Aging report

#### Cross-References
- → HOA Guide: "Invoices and Payments"

---

### 6. Reporting and Analytics (`reporting-and-analytics.md`)

#### Topics to Cover
- [ ] Dashboard analytics
  - Key metrics
  - Trend charts
  - Performance indicators
- [ ] Aging reports
  - Invoice aging
  - Study aging by status
- [ ] Email logs
  - Viewing sent emails
  - Email delivery status
  - Troubleshooting delivery issues
- [ ] Audit and compliance
  - Activity logging
  - User action history
  - Data export
- [ ] Generated reports archive
  - Accessing past reports
  - Report versioning

#### Key Concepts
- Business intelligence
- Audit trail
- Compliance requirements

#### Screenshots Needed
- [ ] Analytics dashboard
- [ ] Aging report page
- [ ] Email logs page
- [ ] Email detail dialog
- [ ] Audit log (if exposed in UI)
- [ ] Reports archive

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
- [ ] Login page
- [ ] Password reset
- [ ] Dashboard (all 3 role views)
- [ ] Navigation menu (all 3 role views)
- [ ] Notification panel
- [ ] User profile/settings

#### HOA-Specific (12 screenshots)
- [ ] HOA dashboard
- [ ] New request form (3 sections)
- [ ] Proposal review page
- [ ] E-signature interface
- [ ] Financial info form (2-3 sections)
- [ ] Documents list
- [ ] Report download
- [ ] My Invoices page
- [ ] Payment form

#### Specialist-Specific (18 screenshots)
- [ ] Specialist dashboard
- [ ] Kanban board
- [ ] Study detail page
- [ ] Create proposal form
- [ ] Send proposal dialog
- [ ] Financial info review
- [ ] Schedule site visit
- [ ] Photo upload
- [ ] Component editor
- [ ] Funding plan config
- [ ] Narrative editor
- [ ] Report preview
- [ ] Submit for QA

#### Admin-Specific (20 screenshots)
- [ ] Admin dashboard
- [ ] Users list
- [ ] Create/Edit user dialogs
- [ ] Role assignment
- [ ] HOA user invitation
- [ ] Request queue
- [ ] Assign specialist dialog
- [ ] QA review interface
- [ ] Publish report dialog
- [ ] Tenant settings
- [ ] Reserve study settings
- [ ] Element management
- [ ] Invoice settings
- [ ] Invoice list/detail
- [ ] Aging report
- [ ] Email logs

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
- [Link to related topic](./related-topic.md)
- [Link to another guide](../OtherGuide/topic.md)
```

### Content Checklist (per section)

- [ ] Clear introduction
- [ ] Step-by-step instructions
- [ ] All UI elements referenced have screenshots
- [ ] Tips for common issues
- [ ] Warnings for destructive actions
- [ ] Cross-references to related topics
- [ ] Consistent terminology (see glossary)

---

## Next Steps

1. [ ] Review and approve this content plan
2. [ ] Capture screenshots (can be done in parallel with writing)
3. [ ] Write HOA User Guide (priority 1)
4. [ ] Write Specialist Guide (priority 2)
5. [ ] Write Admin Guide (priority 3)
6. [ ] Create diagrams
7. [ ] Review and edit all guides
8. [ ] Implement in-app help system
