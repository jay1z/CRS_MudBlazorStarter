# User Management

Managing users is a core admin responsibility. This guide covers creating staff users, inviting HOA clients, assigning roles, and maintaining user accounts.

---

## User Types Overview

Your organization has two main user categories:

### Staff Users (Internal)

| Role | Description | Access Level |
|------|-------------|--------------|
| **TenantOwner** | Admin/owner | Full access |
| **TenantSpecialist** | Inspector/analyst | Work on assigned studies |
| **TenantViewer** | Read-only staff | View studies only |

### External Users

| Role | Description | Access Level |
|------|-------------|--------------|
| **HOAUser** | HOA board member/manager | Their studies only |
| **HOAAuditor** | Compliance reviewer | Read-only access |

---

## Accessing User Management

### Navigate to Users

1. Click **"Manage Users"** in the Tenant Admin menu
2. Or go to `/Admin/Users`

### User List View

The user list displays:

| Column | Information |
|--------|-------------|
| **Name** | User's full name |
| **Email** | Login email |
| **Role(s)** | Assigned roles |
| **Status** | Active/Inactive |
| **Last Login** | Most recent access |
| **Actions** | Edit, Manage roles |

### Filtering Users

Filter the list by:

- **Role** - Show only specialists, admins, etc.
- **Status** - Active or inactive
- **Search** - Find by name or email

---

## Managing Staff Users

### Creating a Staff User

1. Click **"Add User"** or **"Create User"**
2. Fill in user details:

| Field | Description | Required |
|-------|-------------|----------|
| **First Name** | User's first name | ✅ |
| **Last Name** | User's last name | ✅ |
| **Email** | Login email (must be unique) | ✅ |
| **Phone** | Contact phone | Optional |
| **Role** | Initial role assignment | ✅ |

3. Click **"Create"** or **"Save"**

### What Happens After Creation

1. User account is **created**
2. **Welcome email** sent with temporary password
3. User can **log in** and set new password
4. User appears in **user list**

### Editing a User

1. Find the user in the list
2. Click **"Edit"** or the user's name
3. Modify details:
   - Name, phone, email
   - Profile information
   - Notification preferences
4. Click **"Save"**

> **Note**: Changing email may require re-verification.

### Deactivating a User

To remove access without deleting:

1. Open the user's profile
2. Click **"Deactivate"** or toggle **"Active"** off
3. Confirm the action

**Effects of deactivation:**

- User **cannot log in**
- User **retains** their data
- Assigned studies **remain** assigned
- Can be **reactivated** later

### Reactivating a User

1. Filter by **"Inactive"** status
2. Find the user
3. Click **"Activate"** or toggle **"Active"** on
4. User can log in again

### Deleting a User

> **Warning**: Deletion is permanent. Consider deactivating instead.

If you must delete:

1. Deactivate the user first
2. **Reassign** their studies to others
3. Delete (if option available)
4. Or contact support for deletion

---

## Role Assignment

### Understanding Roles

| Role | Scope | Key Permissions |
|------|-------|-----------------|
| **TenantOwner** | Tenant | Full admin access |
| **TenantSpecialist** | Tenant | Create proposals, conduct visits, write reports |
| **TenantViewer** | Tenant | View-only access |
| **HOAUser** | External | Submit requests, accept proposals, view own studies |
| **HOAAuditor** | External | Read-only compliance access |

### Assigning Roles

1. Open the user's profile
2. Go to **"Roles"** section
3. Click **"Add Role"**
4. Select the role from dropdown
5. Save

### Multiple Roles

Users can have multiple roles:

- A user could be both **TenantOwner** and **TenantSpecialist**
- Permissions are **additive** (combined)
- Most restrictive policies may apply for some actions

### Removing Roles

1. Open user's profile
2. Go to **"Roles"** section
3. Click **"Remove"** next to the role
4. Confirm removal

> **Warning**: Removing TenantOwner from yourself could lock you out. Ensure another admin exists.

---

## Inviting HOA Users

### When to Invite

Invite HOA users when:

- A new study is starting
- A contact needs portal access
- A property manager requests access

### Sending an Invitation

1. Click **"Invite HOA User"** or go to HOA Users section
2. Fill in invitation details:

| Field | Description | Required |
|-------|-------------|----------|
| **Email** | Recipient's email | ✅ |
| **First Name** | Their first name | ✅ |
| **Last Name** | Their last name | ✅ |
| **Community** | Associated property | Optional |
| **Role** | HOAUser or HOAAuditor | ✅ |
| **Message** | Personal note | Optional |

3. Click **"Send Invitation"**

### Invitation Process

1. **Email sent** to the recipient
2. Email contains **registration link**
3. Recipient **creates password**
4. Account is **activated**
5. User can **access portal**

### Managing Pending Invites

View pending invitations:

1. Go to **"HOA Users"** or **"Pending Invites"**
2. See list of sent but unaccepted invites
3. Options:
   - **Resend** invitation email
   - **Cancel** the invitation
   - **View** details

### Invitation Expiration

- Invitations typically expire after **7-30 days**
- Expired invites can be **resent**
- Consider following up if not accepted

---

## HOA User Management

### Viewing HOA Users

1. Navigate to **"HOA Users"** section
2. Or filter user list by **"HOAUser"** role

### HOA User Information

For each HOA user:

| Field | Description |
|-------|-------------|
| **Name** | User's name |
| **Email** | Login email |
| **Communities** | Associated properties |
| **Status** | Active/Inactive |
| **Last Login** | Recent access |

### Linking to Communities

Associate HOA users with communities:

1. Open the HOA user's profile
2. Go to **"Communities"** section
3. Add or remove community associations
4. User sees only linked studies

### HOA User Access Control

HOA users can only access:

- Studies for **their communities**
- Documents for **their studies**
- Invoices addressed to **them**

---

## User Impersonation

If enabled, admins can impersonate users for support purposes.

### When to Use

- **Troubleshooting** user-reported issues
- **Training** demonstrations
- **Testing** user experience

### How to Impersonate

1. Find the user in the list
2. Click **"Impersonate"** (if available)
3. You now see the portal **as that user**
4. Your actions are **logged**

### Stopping Impersonation

1. Click **"Stop Impersonation"** banner
2. Or navigate to `/Admin/StopImpersonation`
3. You return to your admin view

### Impersonation Logging

All impersonation is logged:

- **Who** impersonated
- **When** it occurred
- **What actions** were taken
- Visible in **audit logs**

---

## Password Management

### User Password Reset

If a user is locked out:

1. They can use **"Forgot Password"** on login page
2. Or you can **send a reset link**:
   - Open user profile
   - Click **"Send Password Reset"**
   - User receives email with reset link

### Password Policies

Your system may enforce:

| Policy | Requirement |
|--------|-------------|
| **Minimum length** | 8+ characters |
| **Complexity** | Mix of letters, numbers, symbols |
| **Expiration** | Periodic password changes |
| **History** | Can't reuse recent passwords |

---

## Best Practices

### User Lifecycle

| Event | Action |
|-------|--------|
| **New hire** | Create account, assign role |
| **Role change** | Update roles appropriately |
| **Departure** | Deactivate, reassign work |
| **Return** | Reactivate account |

### Security

| Practice | Reason |
|----------|--------|
| Use unique emails | One account per person |
| Remove unused accounts | Reduce security risk |
| Review access regularly | Ensure appropriate access |
| Don't share credentials | Individual accountability |

### HOA User Tips

| Practice | Reason |
|----------|--------|
| Invite early | Gives client portal access |
| Link to correct community | Proper access scoping |
| Follow up on invites | Ensure registration |
| Communicate clearly | Explain what they'll access |

---

## Troubleshooting

### Common Issues

**Q: User says they can't log in.**
A: Check: Is account active? Password correct? Account locked? Send reset if needed.

**Q: User can't see a study.**
A: Check role and community association. HOA users need linked community.

**Q: I accidentally deactivated myself.**
A: Contact another admin or platform support.

**Q: Invitation email not received.**
A: Check spam folder. Verify email address. Resend invitation.

**Q: User has wrong role.**
A: Edit user → Roles → Add/remove as needed.

---

## Next Steps

With users set up, manage the workflow:

- **[Workflow Management →](./workflow-management.md)** - Handle approvals and assignments

---

[← Getting Started](./getting-started.md) | [Workflow Management →](./workflow-management.md)
