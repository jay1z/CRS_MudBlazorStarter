# Stripe Connect Test Plan

## Overview
This test plan covers the Stripe Connect functionality in the CRS application, which allows tenants (Reserve Study companies) to receive payments from their HOA clients through Direct Charges.

## Prerequisites

### Environment Setup
- [ ] Stripe Test Mode enabled (use test API keys)
- [ ] Stripe CLI installed for webhook testing: `stripe listen --forward-to localhost:5xxx/api/webhooks/stripe`
- [ ] Test tenant account created in the application
- [ ] User with `TenantOwner` or `TenantAdmin` role

### Stripe Test Data
- **Test Card (Success):** `4242 4242 4242 4242`
- **Test Card (Decline):** `4000 0000 0000 0002`
- **Test ACH (Success):** Use Stripe test bank `000123456789` routing `110000000`
- **Test SSN (last 4):** `0000`
- **Test Business Tax ID:** `000000000`

---

## Test Scenarios

### 1. Connect Account Creation & Onboarding

#### TC-1.1: Start New Stripe Connect Onboarding
**Preconditions:** Tenant has no existing Stripe Connect account

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Log in as TenantOwner | Dashboard loads successfully |
| 2 | Navigate to `/account/payment-settings` | Payment Settings page loads |
| 3 | Verify "No Account" state displayed | Shows "Connect Stripe Account" button, platform fee table |
| 4 | Click "Connect Stripe Account" | Loading indicator appears |
| 5 | Wait for redirect | User redirected to Stripe Express onboarding |
| 6 | Complete Stripe onboarding form | Use test data above |
| 7 | Submit onboarding | Redirected back to `/account/payment-settings?onboarding=complete` |
| 8 | Verify success message | Snackbar shows "Stripe Connect setup completed!" |
| 9 | Verify status indicators | Card Payments: Active, Payouts: Enabled |

**API Endpoint:** `POST /api/stripe/connect/onboard`

#### TC-1.2: Resume Incomplete Onboarding
**Preconditions:** Tenant has Connect account but onboarding is incomplete

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Payment Settings | Warning alert shows "setup is incomplete" |
| 2 | Click "Continue Setup" | Redirected to Stripe onboarding |
| 3 | Complete remaining fields | Successfully completes onboarding |
| 4 | Return to app | Status updates to fully connected |

**API Endpoint:** `POST /api/stripe/connect/onboard/resume`

#### TC-1.3: Onboarding for Existing Account
**Preconditions:** Tenant already has `StripeConnectAccountId` stored

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call `CreateAccountAndGetOnboardingUrlAsync` | Returns existing account ID, new onboarding URL |
| 2 | Verify no duplicate account created | Account ID remains the same |

---

### 2. Account Status Management

#### TC-2.1: Get Account Status (Cached)
**Preconditions:** Account synced within last 5 minutes

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call `GET /api/stripe/connect/status` | Returns cached status |
| 2 | Verify no Stripe API call | Status returned from database |

#### TC-2.2: Get Account Status (Stale Cache)
**Preconditions:** Last sync > 5 minutes ago

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call `GET /api/stripe/connect/status` | Triggers `SyncAccountStatusAsync` |
| 2 | Verify fresh data | `LastSynced` timestamp updated |

#### TC-2.3: Manual Status Refresh
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Refresh Status" on UI | Loading indicator appears |
| 2 | Wait for completion | Status indicators update |
| 3 | Verify `LastSynced` updated | Timestamp reflects current time |

**API Endpoint:** `POST /api/stripe/connect/sync`

#### TC-2.4: Status When No Account Exists
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call status for tenant without account | Returns `HasConnectAccount: false` |
| 2 | Verify all flags are false | Onboarding, Payouts, CardPayments all false |

---

### 3. Stripe Express Dashboard Access

#### TC-3.1: Open Stripe Dashboard
**Preconditions:** Fully onboarded Connect account

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Open Stripe Dashboard" | New tab opens |
| 2 | Verify Stripe Express Dashboard loads | Tenant can view transactions, payouts |

**API Endpoint:** `GET /api/stripe/connect/dashboard`

#### TC-3.2: Dashboard Link Without Account
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call dashboard endpoint without account | Returns 400 error |
| 2 | Verify error message | "Could not create dashboard link..." |

---

### 4. Direct Charge Payment Flow

#### TC-4.1: Invoice Payment with Connected Account
**Preconditions:** Tenant has active Stripe Connect, invoice created

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice for client | Invoice created with payment link |
| 2 | Verify checkout session uses `StripeAccount` header | Direct charge to connected account |
| 3 | Verify `ApplicationFeeAmount` set | Platform fee calculated per tier |
| 4 | Client pays via card | Payment succeeds |
| 5 | Verify funds routing | Tenant receives payment minus fees |
| 6 | Verify platform fee collected | Application fee in platform account |

#### TC-4.2: Invoice Payment Without Connected Account
**Preconditions:** Tenant has no Stripe Connect account

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice for client | Invoice created |
| 2 | Generate payment link | Standard checkout (no Direct Charge) |
| 3 | Verify `ApplicationFeeAmount` is null | No platform fee applies |
| 4 | Client pays | Payment processed through platform account |

#### TC-4.3: ACH Bank Transfer Payment
**Preconditions:** Connected account with ACH enabled

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Client selects "Bank Account" at checkout | ACH flow begins |
| 2 | Complete test bank verification | `us_bank_account` payment method |
| 3 | Payment completes | Invoice marked paid, lower fees |

---

### 5. Application Fee Calculation

#### TC-5.1: Startup Tier Fee (2.0%)
| Input | Expected Fee |
|-------|-------------|
| $1,000 | $20.00 (2000 cents) |
| $500.50 | $10.01 (1001 cents, rounded up) |

#### TC-5.2: Pro Tier Fee (1.5%)
| Input | Expected Fee |
|-------|-------------|
| $1,000 | $15.00 (1500 cents) |
| $2,500 | $37.50 (3750 cents) |

#### TC-5.3: Enterprise Tier Fee (1.0%)
| Input | Expected Fee |
|-------|-------------|
| $1,000 | $10.00 (1000 cents) |
| $10,000 | $100.00 (10000 cents) |

#### TC-5.4: Unknown Tenant Defaults to Pro Rate
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Calculate fee for non-existent tenant | Uses Pro rate (1.5%) |

---

### 6. Webhook Handling

#### TC-6.1: account.updated Webhook
**Trigger:** Account status changes in Stripe

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Simulate `account.updated` event | Webhook received |
| 2 | Verify tenant flags updated | `PayoutsEnabled`, `CardPaymentsEnabled` |
| 3 | Verify `StripeConnectLastSyncedAt` updated | Timestamp refreshed |

**Testing via Stripe CLI:**
```bash
stripe trigger account.updated --override account:id=acct_xxx
```

#### TC-6.2: account.application.deauthorized Webhook
**Trigger:** Tenant disconnects from platform in Stripe

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Simulate deauthorization event | Webhook received |
| 2 | Verify `OnboardingComplete` = false | Account marked inactive |
| 3 | Verify `PayoutsEnabled` = false | Payouts disabled |
| 4 | Verify `StripeConnectAccountId` preserved | Can reconnect later |
| 5 | Check logs | Warning logged about deauthorization |

#### TC-6.3: checkout.session.completed Webhook (Direct Charge)
**Trigger:** Payment completed on connected account

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Complete payment on invoice | Webhook fires |
| 2 | Verify invoice status updated | Status = Paid |
| 3 | Verify payment record created | Amount and reference saved |
| 4 | Verify application fee recorded | Fee amount tracked |

---

### 7. Security & Authorization

#### TC-7.1: Tenant Isolation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | User from Tenant A calls status endpoint | Only sees Tenant A's account |
| 2 | Attempt to access Tenant B's account | Denied / returns own tenant data |

#### TC-7.2: Role-Based Access
| Role | Access to `/account/payment-settings` |
|------|--------------------------------------|
| TenantOwner | ✅ Allowed |
| TenantAdmin | ✅ Allowed |
| TenantSpecialist | ❌ Denied |
| User (HOA) | ❌ Denied |
| Admin | ❌ Denied (unless tenant context) |

#### TC-7.3: Missing Tenant Context
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call API without tenant context | Returns 400 "Tenant context not available" |

---

### 8. Error Handling

#### TC-8.1: Stripe API Failure During Onboarding
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Simulate Stripe API error | Returns `Success: false` |
| 2 | Verify error logged | Error message in logs |
| 3 | Verify UI shows error | User sees error message |

#### TC-8.2: Invalid Tenant ID
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call with non-existent tenant | "Tenant not found" error |

#### TC-8.3: Account Link Creation Failure
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Simulate AccountLink API failure | Returns null URL |
| 2 | Verify graceful degradation | No crash, error logged |

---

### 9. UI/UX Testing

#### TC-9.1: Payment Settings Page States
| State | UI Elements |
|-------|-------------|
| No Account | Setup button, fee table, info alert |
| Incomplete Onboarding | Warning alert, "Continue Setup" button |
| Fully Connected | Status cards, dashboard button, refresh button |
| Loading | Progress indicator |

#### TC-9.2: Query Parameter Handling
| URL Parameter | Expected Behavior |
|---------------|-------------------|
| `?onboarding=complete` | Success snackbar, status refresh |
| `?onboarding=refresh` | Warning snackbar about completing setup |

#### TC-9.3: Platform Fee Display by Tier
| Tier | Displayed Fee |
|------|---------------|
| Startup | 2.0% |
| Pro | 1.5% |
| Enterprise | 1.0% |

---

## Integration Test Checklist

### End-to-End Flow
- [ ] New tenant signs up
- [ ] Owner navigates to Payment Settings
- [ ] Completes Stripe Connect onboarding
- [ ] Creates reserve study with invoice
- [ ] HOA client receives invoice email
- [ ] Client pays invoice via Stripe Checkout
- [ ] Webhook updates invoice status
- [ ] Tenant sees payment in Stripe Dashboard
- [ ] Platform collects application fee

### Regression Scenarios
- [ ] Existing invoices still payable after Connect setup
- [ ] Tenant without Connect can still create invoices
- [ ] Multiple tenants can have separate Connect accounts
- [ ] Disconnecting Stripe doesn't break existing payments

---

## Test Environment Commands

### Start Webhook Listener
```bash
stripe listen --forward-to https://localhost:7xxx/api/webhooks/stripe
```

### Trigger Test Webhooks
```bash
# Account updated
stripe trigger account.updated

# Checkout completed
stripe trigger checkout.session.completed
```

### Check Account Status in Stripe
```bash
stripe accounts retrieve acct_xxxxx
```

---

## Notes

- Always use **Stripe Test Mode** for testing
- **Never** use production API keys in test environments
- Clear browser cookies/cache if tenant context issues occur
- Check Stripe Dashboard → Developers → Webhooks for event logs
- Monitor application logs for detailed error information
