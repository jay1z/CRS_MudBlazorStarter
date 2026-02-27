# Role Definitions & Workflow

## Role Hierarchy

| Scope | Role | Policy | Description |
|-------|------|--------|-------------|
| **Platform** | PlatformAdmin | `RequirePlatformAdmin` | SaaS owner, can access all tenants |
| **Platform** | PlatformSupport | - | Limited global read/support (optional) |
| **Tenant** | TenantOwner | `RequireTenantOwner` | Company admin, reviews requests, assigns specialists, publishes reports |
| **Tenant** | TenantSpecialist | `RequireTenantStaff` | Performs inspections, creates proposals, drafts reports |
| **Tenant** | TenantViewer | `RequireTenantViewer` | Read-only internal staff |
| **External** | HOAUser | `RequireHOAUser` | HOA board member, submits requests, accepts proposals |
| **External** | HOAAuditor | `RequireHOAUser` | Read-only access for compliance review |

### Who is "Staff"?

**"Staff" = TenantOwner + TenantSpecialist**

These are internal employees of the reserve study company (tenant). The `RequireTenantStaff` policy allows access to both roles.

---

## Reserve Study Workflow - Who Does What

```mermaid
flowchart TD
    subgraph HOA["HOA User (External Customer)"]
        A[Submit Request Form]
        G[Accept Proposal via E-Sign]
        I[Submit Financial Info]
    end

    subgraph OWNER["TenantOwner (Company Admin)"]
        C[Review Request]
        D{Approve?}
        E[Assign Specialist]
        N[QA Review Report]
        P[Publish Final Report]
    end

    subgraph SPECIALIST["TenantSpecialist (Inspector)"]
        F[Create & Send Proposal]
        H[Request Financial Info]
        J[Schedule Site Visit]
        K[Perform Inspection]
        L[Upload Photos & Notes]
        M[Generate Draft Report]
    end

    A -->|NewRequest| B[Request Created]
    B -->|Auto| C
    C --> D
    D -->|Yes| E
    D -->|No, NeedsInfo| A
    E -->|Assigned| F
    F -->|ProposalPendingESign| G
    G -->|Accepted| H
    H --> I
    I -->|UnderReview| J
    J -->|Scheduled| K
    K -->|InProgress| L
    L --> M
    M -->|ReportDrafted| N
    N -->|ApprovedReport| P
    P -->|Complete| Q[Study Complete]
```

---

## Workflow Status Transitions by Role

| From Status | To Status | Who Can Do This | Action |
|-------------|-----------|-----------------|--------|
| NewRequest | PendingDetails | System | Auto when elements added |
| PendingDetails | ReadyForReview | System | Auto when all data complete |
| ReadyForReview | **Approved** | **TenantOwner** | Reviews & approves request |
| ReadyForReview | NeedsInfo | **TenantOwner** | Requests more info from HOA |
| NeedsInfo | ReadyForReview | HOAUser | Provides requested info |
| Approved | **Assigned** | **TenantOwner** | Assigns a specialist |
| Assigned | ProposalPendingESign | **TenantSpecialist** | Creates & sends proposal |
| ProposalPendingESign | **Accepted** | **HOAUser** | E-signs the proposal |
| ProposalPendingESign | Rejected | HOAUser | Rejects proposal |
| Accepted | Scheduled | TenantSpecialist | Schedules site visit |
| Scheduled | InProgress | TenantSpecialist | Starts inspection |
| InProgress | UnderReview | TenantSpecialist | Uploads data |
| UnderReview | ReportDrafted | TenantSpecialist | Generates report |
| ReportDrafted | **ApprovedReport** | **TenantOwner** | QA approves report |
| ApprovedReport | **Complete** | **TenantOwner** | Publishes to HOA |
| Complete | Archived | System | Auto after time period |

---

## Data Model

```mermaid
erDiagram
    TENANT {
        int TenantId PK
        string Name
        string Subdomain
        string SubscriptionTierId
    }

    APPLICATIONUSER {
        guid UserId PK
        string Email
        string FirstName
        string LastName
    }

    ROLE {
        guid RoleId PK
        string Name
        string Scope "Platform|Tenant|External"
    }

    USERROLEASSIGNMENT {
        guid UserRoleAssignmentId PK
        guid UserId FK
        guid RoleId FK
        int TenantId FK "null for Platform roles"
    }

    COMMUNITY {
        guid CommunityId PK
        int TenantId FK
        string Name
        string Address
    }

    RESERVESTUDY {
        guid ReserveStudyId PK
        int TenantId FK
        guid CommunityId FK
        guid ApplicationUserId FK "HOA user who submitted"
        guid SpecialistUserId FK "Assigned specialist"
        int Status
    }

    STUDYREQUEST {
        guid Id PK "Same as ReserveStudyId"
        int TenantId FK
        int CurrentStatus "Engine status enum"
    }

    PROPOSAL {
        guid ProposalId PK
        guid ReserveStudyId FK
        decimal EstimatedCost
        datetime DateSent
        datetime DateApproved
    }

    REPORT {
        guid ReportId PK
        guid ReserveStudyId FK
        bool IsPublishedToClient
        datetime PublishedAt
    }

    TENANT ||--o{ COMMUNITY : has
    TENANT ||--o{ USERROLEASSIGNMENT : scopes
    COMMUNITY ||--o{ RESERVESTUDY : has
    RESERVESTUDY ||--|| STUDYREQUEST : "1:1 workflow"
    RESERVESTUDY ||--o| PROPOSAL : has
    RESERVESTUDY ||--o{ REPORT : has

    APPLICATIONUSER ||--o{ USERROLEASSIGNMENT : has
    APPLICATIONUSER ||--o{ RESERVESTUDY : submits
    APPLICATIONUSER ||--o{ RESERVESTUDY : assigned_to

    ROLE ||--o{ USERROLEASSIGNMENT : defines
```

---

## Page Access by Role

| Page/Feature | PlatformAdmin | TenantOwner | TenantSpecialist | TenantViewer | HOAUser |
|--------------|:-------------:|:-----------:|:----------------:|:------------:|:-------:|
| `/Admin/*` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `/ReserveStudies/Create` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `/ReserveStudies/Request` | ✅ | ✅ | ✅ | ❌ | ✅ |
| `/ReserveStudies/{id}/Details` | ✅ | ✅ | ✅ | ✅ | ✅* |
| Assign Specialist | ✅ | ✅ | ❌ | ❌ | ❌ |
| Send Proposal | ✅ | ✅ | ✅ | ❌ | ❌ |
| Accept Proposal | ❌ | ❌ | ❌ | ❌ | ✅ |
| Upload Photos/Notes | ✅ | ✅ | ✅ | ❌ | ❌ |
| Approve Report (QA) | ✅ | ✅ | ❌ | ❌ | ❌ |
| Publish Report | ✅ | ✅ | ❌ | ❌ | ❌ |
| Download Final Report | ✅ | ✅ | ✅ | ✅ | ✅ |

\* HOAUser can only see their own studies

---

## Summary: The Three User Types

### 1. **HOA Users** (External Customers)
- Submit reserve study requests
- Accept proposals (e-signature)
- Provide financial information
- Download final reports
- **Cannot** assign specialists, create proposals, or approve reports

### 2. **Tenant Staff** (Reserve Study Company Employees)
- **TenantOwner**: Reviews requests, assigns specialists, QA approves reports, publishes
- **TenantSpecialist**: Creates proposals, performs inspections, drafts reports
- **TenantViewer**: Read-only access to studies

### 3. **Platform Admin** (SaaS Owner - You)
- Full access to all tenants
- Manages subscriptions, tenants, global settings
