# Invoice System Test Plan

This document provides a comprehensive test plan for the CRS Invoice System.

**Last Updated:** 2025-01-16  
**Version:** 1.0

---

## Table of Contents

1. [Overview](#overview)
2. [Test Environment](#test-environment)
3. [Test Categories](#test-categories)
4. [Test Cases](#test-cases)
   - [Invoice Creation](#1-invoice-creation)
   - [Invoice Line Items](#2-invoice-line-items)
   - [Invoice Status Workflow](#3-invoice-status-workflow)
   - [Payment Processing](#4-payment-processing)
   - [Stripe Integration](#5-stripe-integration)
   - [Credit Memos](#6-credit-memos)
   - [Invoice PDF Generation](#7-invoice-pdf-generation)
   - [Invoice Reminders](#8-invoice-reminders)
   - [Automated Jobs](#9-automated-jobs)
   - [Invoice List Page](#10-invoice-list-page)
   - [Invoice Detail Page](#11-invoice-detail-page)
   - [Batch Operations](#12-batch-operations)
   - [Client Portal](#13-client-portal)
   - [Reports](#14-reports)
   - [Security & Authorization](#15-security--authorization)
5. [Edge Cases](#edge-cases)
6. [Performance Testing](#performance-testing)
7. [Regression Testing](#regression-testing)

---

## Overview

### Scope
This test plan covers all invoice-related functionality in the CRS application, including:
- Invoice CRUD operations
- Payment processing (manual and Stripe)
- Credit memos and refunds
- PDF generation
- Automated reminders and late interest
- Reporting and dashboards
- Client portal access
- Invoice number customization
- Auto-generate next milestone
- Tenant invoice settings

### Out of Scope
- Unit tests for individual methods (covered separately)
- Load testing
- Penetration testing

---

## Test Environment

### Prerequisites
- [ ] Database seeded with test tenant
- [ ] Test user with Specialist role
- [ ] Stripe test mode enabled
- [ ] Email service configured (or using mock)
- [ ] Test reserve study with accepted proposal

### Test Data Requirements
- At least 1 tenant with active subscription
- At least 1 user with "Specialist" role
- At least 3 reserve studies with accepted proposals
- Stripe test API keys configured

---

## Test Categories

| Category | Priority | Test Count |
|----------|----------|------------|
| Invoice Creation | High | 12 |
| Invoice Line Items | Medium | 8 |
| Invoice Status Workflow | High | 15 |
| Payment Processing | Critical | 18 |
| Stripe Integration | Critical | 10 |
| Credit Memos | High | 12 |
| Invoice PDF Generation | Medium | 8 |
| Invoice Reminders | Medium | 10 |
| Automated Jobs | High | 6 |
| Invoice List Page | Medium | 12 |
| Invoice Detail Page | High | 14 |
| Batch Operations | Medium | 8 |
| Client Portal | High | 10 |
| Reports | Medium | 8 |
| Security & Authorization | Critical | 10 |

**Total: 151 test cases**

---

## Test Cases

### 1. Invoice Creation

#### TC-INV-001: Create Invoice from Proposal
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to accepted proposal | Proposal detail page displays |
| 2 | Click "Create Invoice" button | Invoice creation dialog opens |
| 3 | Select payment schedule (Full Payment) | Schedule selected |
| 4 | Click "Create" | Invoice created with correct amount |
| 5 | Verify invoice number | Format: INV-YYYY-##### |
| 6 | Verify line items | Items copied from proposal |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-INV-002: Create Invoice with 50/50 Split
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice from proposal | Dialog opens |
| 2 | Select "50/50 Split" schedule | Schedule selected |
| 3 | Click "Create" | Two invoices created (50% each) |
| 4 | Verify milestone labels | "Deposit" and "Final Payment" |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-INV-003: Create Invoice with Custom Percentage
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "Custom" schedule | Custom percentage input appears |
| 2 | Enter 30% deposit | Value accepted |
| 3 | Click "Create" | Two invoices (30% and 70%) |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-004: Invoice Number Sequence
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create first invoice of year | Number: INV-2025-00001 |
| 2 | Create second invoice | Number: INV-2025-00002 |
| 3 | Create invoice in new year | Number: INV-2026-00001 |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-INV-005: Create Invoice with Tax
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice with 8% tax rate | Tax calculated |
| 2 | Verify subtotal | Correct sum of line items |
| 3 | Verify tax amount | 8% of subtotal |
| 4 | Verify total | Subtotal + Tax |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-006: Create Invoice with Discount
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice with $500 discount | Discount applied |
| 2 | Enter discount description | "Early signup bonus" |
| 3 | Verify total | Subtotal - Discount + Tax |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-007: Create Invoice with Payment Terms
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Set Net Days to 30 | Due date = Invoice date + 30 |
| 2 | Set early payment discount (2/10) | Discount fields populated |
| 3 | Set late interest rate (1.5%) | Rate stored |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-INV-008: Duplicate Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open existing invoice | Invoice detail displays |
| 2 | Click "Duplicate" | New invoice created |
| 3 | Verify new invoice is Draft | Status = Draft |
| 4 | Verify new invoice number | New sequential number |
| 5 | Verify line items copied | All items present |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-009: Bill To Information
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice | Bill To auto-populated |
| 2 | Verify BillToName | Matches contact name |
| 3 | Verify BillToEmail | Matches contact email |
| 4 | Verify BillToAddress | Matches community address |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-010: Create Draft Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create new invoice | Status = Draft |
| 2 | Verify editable | Can modify all fields |
| 3 | Verify not sent | SentAt is null |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-INV-011: Create Invoice - Validation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Try creating invoice without reserve study | Error displayed |
| 2 | Try creating with 0 amount | Error displayed |
| 3 | Try creating with invalid email | Validation error |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-INV-012: Invoice Date Defaults
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create new invoice | Invoice date = today |
| 2 | With Net 30 terms | Due date = today + 30 days |

**Priority:** Low  
**Status:** ⬜ Not Started

---

### 2. Invoice Line Items

#### TC-LI-001: Add Line Item
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open draft invoice | Line items section visible |
| 2 | Click "Add Line Item" | Add form appears |
| 3 | Enter description, quantity, price | Fields accepted |
| 4 | Save line item | Item added to invoice |
| 5 | Verify line total | Quantity × Unit Price |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-LI-002: Edit Line Item
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click edit on line item | Edit form opens |
| 2 | Modify quantity | Value updated |
| 3 | Save changes | Totals recalculated |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LI-003: Remove Line Item
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click delete on line item | Confirmation prompt |
| 2 | Confirm deletion | Item removed |
| 3 | Verify totals updated | Subtotal recalculated |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LI-004: Line Item Sort Order
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Add multiple line items | Items displayed in order |
| 2 | Reorder items | Sort order updated |
| 3 | Verify on PDF | Order preserved |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-LI-005: Line Item Validation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Try adding item with empty description | Error displayed |
| 2 | Try adding with quantity = 0 | Error displayed |
| 3 | Try adding with negative price | Error displayed |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LI-006: Line Items on Finalized Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Finalize invoice | Status = Finalized |
| 2 | Try editing line item | Edit disabled |
| 3 | Try adding line item | Add button disabled |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-LI-007: Subtotal Calculation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Add 3 line items | Subtotal = sum of all |
| 2 | Verify precision | 2 decimal places |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LI-008: Line Item Soft Delete
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Delete line item | DateDeleted set |
| 2 | Verify not in UI | Item not displayed |
| 3 | Verify in database | Record still exists |

**Priority:** Low  
**Status:** ⬜ Not Started

---

### 3. Invoice Status Workflow

#### TC-ST-001: Draft to Finalized
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open draft invoice | Finalize button visible |
| 2 | Click "Finalize" | Status changes to Finalized |
| 3 | Verify edit locked | Cannot modify line items |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-002: Finalized to Sent
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open finalized invoice | Send button visible |
| 2 | Click "Send Invoice" | Email sent to client |
| 3 | Verify status | Status = Sent |
| 4 | Verify SentAt | Timestamp recorded |
| 5 | Verify SentByUserId | Current user ID |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-003: Sent to Viewed
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Client opens invoice (public portal) | Invoice displays |
| 2 | Verify status | Status = Viewed |
| 3 | Verify ViewedAt | Timestamp recorded |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-ST-004: Sent to Partially Paid
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Record partial payment | Payment recorded |
| 2 | Verify status | Status = PartiallyPaid |
| 3 | Verify AmountPaid | Correct amount |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-005: Partial to Paid
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Record remaining payment | Balance = 0 |
| 2 | Verify status | Status = Paid |
| 3 | Verify PaidAt | Timestamp recorded |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-006: Sent to Overdue
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice past due date | LateInterestJob runs |
| 2 | Verify status | Status = Overdue |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-007: Void Draft Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open draft invoice | Void button not visible |
| 2 | Delete invoice instead | Invoice soft deleted |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-ST-008: Void Sent Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open sent invoice | Void button visible |
| 2 | Click "Void" | Confirmation dialog |
| 3 | Enter reason and confirm | Status = Voided |
| 4 | Verify VoidReason | Reason stored |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-009: Cannot Void Paid Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open paid invoice | Void button not visible |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-010: Cannot Edit Voided Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open voided invoice | All edit buttons disabled |
| 2 | Try to record payment | Action rejected |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-ST-011: Overdue with Grace Period
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice 3 days past due | Grace period = 5 days |
| 2 | Verify no interest | Interest not applied |
| 3 | Invoice 6 days past due | Interest applied |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-ST-012: Status Badge Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View invoice list | Status badges colored |
| 2 | Draft = gray | Correct color |
| 3 | Paid = green | Correct color |
| 4 | Overdue = red | Correct color |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-ST-013: Delete Draft Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open draft invoice | Delete button visible |
| 2 | Click "Delete" | Confirmation dialog |
| 3 | Confirm | Invoice soft deleted |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-ST-014: Cannot Delete Sent Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open sent invoice | Delete button not visible |
| 2 | Must void instead | Void button available |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-ST-015: Audit Trail
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice | DateCreated set |
| 2 | Modify invoice | DateModified updated |
| 3 | Send invoice | SentAt, SentByUserId set |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 4. Payment Processing

#### TC-PAY-001: Record Manual Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open invoice detail | Record Payment button visible |
| 2 | Click "Record Payment" | Payment dialog opens |
| 3 | Enter amount | Field accepts value |
| 4 | Select method (Check) | Method selected |
| 5 | Enter reference number | Reference stored |
| 6 | Click "Save" | Payment recorded |
| 7 | Verify AmountPaid | Updated correctly |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-PAY-002: Payment Record Created
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Record payment | PaymentRecord created |
| 2 | Verify Amount | Matches payment |
| 3 | Verify PaymentDate | Timestamp recorded |
| 4 | Verify PaymentMethod | "Check" |
| 5 | Verify ReferenceNumber | Stored |
| 6 | Verify IsAutomatic | false |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-003: Multiple Payments
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice total = $10,000 | Balance = $10,000 |
| 2 | Record $3,000 payment | Balance = $7,000 |
| 3 | Record $5,000 payment | Balance = $2,000 |
| 4 | Record $2,000 payment | Balance = $0, Status = Paid |
| 5 | Verify 3 PaymentRecords | All payments tracked |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-004: Payment Exceeds Balance
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Balance = $1,000 | |
| 2 | Try recording $1,500 | Validation error |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-005: Zero Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Try recording $0 payment | Validation error |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-006: Negative Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Try recording -$500 | Validation error |
| 2 | Use Credit Memo instead | Guidance shown |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-007: Payment on Voided Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open voided invoice | Record Payment disabled |
| 2 | Try via API | Error returned |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-008: Payment on Paid Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open paid invoice | Record Payment disabled |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-009: Payment Methods
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Record with "Check" | Method stored |
| 2 | Record with "ACH" | Method stored |
| 3 | Record with "Credit Card" | Method stored |
| 4 | Record with "Cash" | Method stored |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-PAY-010: Payment Timeline Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View invoice with payments | Timeline displays |
| 2 | Most recent first | Correct order |
| 3 | Amount, date, method shown | All details visible |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-011: Early Payment Discount
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with 2/10 Net 30 | Discount available |
| 2 | Pay within 10 days | Discount applied |
| 3 | Verify discount amount | 2% of total |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-012: Early Discount Expired
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice past discount date | Discount not shown |
| 2 | Full amount due | No discount applied |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-013: Late Interest Calculation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice overdue 30 days | Interest applied |
| 2 | Rate = 1.5% monthly | Calculated correctly |
| 3 | Verify AccruedInterest | Displayed on invoice |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-014: Payment Clears Status
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Overdue invoice | Status = Overdue |
| 2 | Pay in full | Status = Paid |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-015: Balance Due Calculation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Total = $10,000 | |
| 2 | Paid = $3,000 | |
| 3 | Credits = $500 | |
| 4 | Verify NetBalanceDue | $6,500 |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PAY-016: RecordedBy User
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Record payment as User A | RecordedByUserId = A |
| 2 | View payment history | User name displayed |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PAY-017: Payment Summary Card
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View invoice detail | Payment summary visible |
| 2 | Shows total paid | Correct amount |
| 3 | Shows balance due | Correct amount |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-PAY-018: Overpayment Handling
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Try to overpay invoice | Validation prevents |
| 2 | Use Credit Memo for refund | Alternative path |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 5. Stripe Integration

#### TC-STRIPE-001: Create Checkout Session
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Pay Now" on invoice | Redirect to Stripe |
| 2 | Verify amount | Correct total |
| 3 | Verify metadata | invoice_id present |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-STRIPE-002: Payment Success
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Complete Stripe payment | Webhook received |
| 2 | Verify invoice updated | AmountPaid increased |
| 3 | Verify status | Paid (if full) |
| 4 | Verify PaymentRecord | IsAutomatic = true |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-STRIPE-003: Payment Success Page
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | After Stripe payment | Redirect to success page |
| 2 | Invoice number displayed | Correct invoice |
| 3 | Amount shown | Correct amount |
| 4 | "Return to Invoices" button | Works correctly |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-STRIPE-004: Payment URL Expiration
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create payment session | URL generated |
| 2 | Wait for expiration | URL expires |
| 3 | Click expired link | New session created |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-STRIPE-005: Webhook Signature Verification
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Send webhook without signature | Request rejected |
| 2 | Send with valid signature | Request processed |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-STRIPE-006: Duplicate Webhook
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Process webhook | Payment recorded |
| 2 | Replay same webhook | Idempotent - no duplicate |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-STRIPE-007: Partial Stripe Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Configure for balance only | Correct amount |
| 2 | Complete payment | Status = PartiallyPaid or Paid |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-STRIPE-008: StripePaymentIntentId Storage
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Complete payment | PaymentIntentId stored |
| 2 | Verify on invoice | Field populated |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-STRIPE-009: Stripe Test Mode
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Use test card 4242... | Payment succeeds |
| 2 | Use test card 4000... (decline) | Payment fails |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-STRIPE-010: Client Portal Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open public invoice link | Pay Now visible |
| 2 | Click Pay Now | Redirects to Stripe |
| 3 | Complete payment | Invoice marked paid |

**Priority:** Critical  
**Status:** ⬜ Not Started

---

### 6. Credit Memos

#### TC-CM-001: Create Credit Memo
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open invoice detail | "Issue Credit Memo" button |
| 2 | Click button | Dialog opens |
| 3 | Enter amount ($500) | Value accepted |
| 4 | Select reason | Reason selected |
| 5 | Click "Issue" | Credit memo created |
| 6 | Verify CM number | CM-YYYY-##### |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-CM-002: Apply Credit Immediately
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create credit memo | "Apply immediately" checked |
| 2 | Submit | Status = Applied |
| 3 | Verify invoice balance | Reduced by credit |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-CM-003: Draft Credit Memo
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create credit memo | Uncheck "Apply immediately" |
| 2 | Submit | Status = Draft |
| 3 | Verify invoice balance | Not changed yet |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-CM-004: Apply Draft Credit
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open draft credit memo | Apply button visible |
| 2 | Click "Apply" | Status = Applied |
| 3 | Verify invoice balance | Now reduced |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-CM-005: Void Credit Memo
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open applied credit memo | Void button visible |
| 2 | Click "Void" | Enter reason |
| 3 | Confirm | Status = Voided |
| 4 | Verify invoice balance | Credit reversed |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-CM-006: Credit Exceeds Balance
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Balance = $500 | |
| 2 | Try creating $600 credit | Validation error |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-CM-007: Credit Memo PDF
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open credit memo | Download PDF button |
| 2 | Click download | PDF downloads |
| 3 | Verify content | Amount, reason, dates |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-CM-008: Credit Memo Reasons
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "Refund" | Option available |
| 2 | Select "Pricing Adjustment" | Option available |
| 3 | Select "Service Credit" | Option available |
| 4 | Select "Duplicate Payment" | Option available |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-CM-009: Credit Brings Balance to Zero
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Balance = $500 | |
| 2 | Apply $500 credit | Balance = $0 |
| 3 | Verify invoice status | Status = Paid |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-CM-010: Multiple Credits on Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Apply $200 credit | Balance reduced |
| 2 | Apply $100 credit | Balance reduced more |
| 3 | Verify TotalCredits | $300 |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-CM-011: Credit Display on Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View invoice with credits | Credit section visible |
| 2 | Shows all credit memos | Listed with amounts |
| 3 | Shows total credits | Sum displayed |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-CM-012: Void Credit on Paid Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice paid via credit | Status = Paid |
| 2 | Void the credit | Status reverts |
| 3 | Balance recalculated | Amount due again |

**Priority:** High  
**Status:** ⬜ Not Started

---

### 7. Invoice PDF Generation

#### TC-PDF-001: Generate Invoice PDF
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open invoice detail | Download PDF button |
| 2 | Click "Download PDF" | File downloads |
| 3 | Verify filename | Invoice-INV-YYYY-#####.pdf |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PDF-002: PDF Content - Header
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open PDF | Company info displayed |
| 2 | Invoice number | Correct |
| 3 | Invoice date | Correct |
| 4 | Due date | Correct |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PDF-003: PDF Content - Bill To
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open PDF | Bill To section |
| 2 | Client name | Correct |
| 3 | Client address | Correct |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PDF-004: PDF Content - Line Items
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open PDF | Line items table |
| 2 | All items present | Correct count |
| 3 | Quantities and prices | Correct values |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PDF-005: PDF Content - Totals
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open PDF | Totals section |
| 2 | Subtotal | Correct |
| 3 | Tax | Correct |
| 4 | Total | Correct |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PDF-006: VOID Watermark
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Generate PDF for voided invoice | VOID watermark visible |
| 2 | Watermark is prominent | Red, diagonal |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PDF-007: Early Discount Notice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with early discount | Notice on PDF |
| 2 | Shows discount amount | Correct value |
| 3 | Shows deadline | Correct date |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-PDF-008: PDF via API
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call GET /api/Invoices/{id}/pdf | PDF returned |
| 2 | Content-Type | application/pdf |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 8. Invoice Reminders

#### TC-REM-001: Send Manual Reminder
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open overdue invoice | Send Reminder button |
| 2 | Click button | Confirmation dialog |
| 3 | Confirm | Email sent |
| 4 | Verify ReminderCount | Incremented |
| 5 | Verify LastReminderSent | Timestamp set |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REM-002: Reminder Email Content
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Send reminder | Email received |
| 2 | Subject includes "Reminder" | Correct |
| 3 | Shows invoice number | Correct |
| 4 | Shows amount due | Correct |
| 5 | Shows days overdue | Correct |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REM-003: Reminder Button State
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Draft invoice | Reminder button hidden |
| 2 | Paid invoice | Reminder button hidden |
| 3 | Voided invoice | Reminder button hidden |
| 4 | Sent invoice | Reminder button visible |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REM-004: Reminder Count Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View invoice | Shows "(X sent)" |
| 2 | Send reminder | Count increases |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-REM-005: Automated Reminder - Before Due
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice due in 3 days | Job runs |
| 2 | Verify reminder sent | Email sent |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REM-006: Automated Reminder - After Due
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice 1 day overdue | Job runs |
| 2 | Verify reminder sent | Email sent |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REM-007: Reminder Schedule
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Track reminders over time | |
| 2 | -3 days | First reminder |
| 3 | +1 day | Second reminder |
| 4 | +7 days | Third reminder |
| 5 | +14 days | Fourth reminder |
| 6 | +30 days | Fifth reminder |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REM-008: Max Reminders
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with 5 reminders | Max reached |
| 2 | Job runs | No more reminders sent |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REM-009: No Reminder for Paid
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice paid | |
| 2 | Job runs | No reminder sent |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REM-010: No Reminder for Voided
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice voided | |
| 2 | Job runs | No reminder sent |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 9. Automated Jobs

#### TC-JOB-001: Late Interest Job Runs
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Overdue invoice exists | |
| 2 | Job runs at 3 AM | Interest calculated |
| 3 | Verify AccruedInterest | Value set |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-JOB-002: Status Update to Overdue
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice past due date | Status = Sent |
| 2 | Job runs | Status = Overdue |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-JOB-003: Grace Period Respected
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Grace period = 5 days | |
| 2 | Invoice 3 days late | No interest |
| 3 | Invoice 6 days late | Interest applied |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-JOB-004: Interest Accumulation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice overdue 30 days | Interest for 1 month |
| 2 | Job runs again at 60 days | Additional interest |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-JOB-005: Reminder Job Runs
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Eligible invoices exist | |
| 2 | Job runs at 8 AM | Reminders sent |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-JOB-006: Job Tenant Isolation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Multiple tenants | |
| 2 | Job runs | Processes all tenants |
| 3 | Each tenant isolated | No cross-contamination |

**Priority:** Critical  
**Status:** ⬜ Not Started

---

### 10. Invoice List Page

#### TC-LIST-001: Navigate to Invoice List
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Invoices" in menu | Page loads |
| 2 | URL is /Invoices | Correct route |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-LIST-002: Summary Cards Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | 4 summary cards |
| 2 | Total count | Correct |
| 3 | Total paid | Correct |
| 4 | Outstanding | Correct |
| 5 | Overdue count | Correct |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LIST-003: Invoice Table
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View table | Columns visible |
| 2 | Invoice #, Client, Date, Due, Amount, Status | All present |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-LIST-004: Filter by Status
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "Paid" filter | Only paid invoices |
| 2 | Select "Overdue" filter | Only overdue |
| 3 | Clear filter | All invoices |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LIST-005: Filter by Date Range
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "This Month" | Current month only |
| 2 | Select "Last 30 Days" | Last 30 days |
| 3 | Select "This Year" | Current year |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LIST-006: Search
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter invoice number | Matching invoices |
| 2 | Enter client name | Matching invoices |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LIST-007: Sort by Column
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Amount" header | Sorts ascending |
| 2 | Click again | Sorts descending |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-LIST-008: Pagination
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | More than 10 invoices | Pagination shown |
| 2 | Click page 2 | Second page loads |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-LIST-009: Click Invoice Link
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click invoice number | Navigates to detail |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-LIST-010: CSV Export
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Export CSV" | File downloads |
| 2 | Verify content | All columns present |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-LIST-011: Empty State
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | No invoices exist | Empty message shown |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-LIST-012: Loading State
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Page loads | Loading indicator |
| 2 | Data loads | Table appears |

**Priority:** Low  
**Status:** ⬜ Not Started

---

### 11. Invoice Detail Page

#### TC-DET-001: Navigate to Detail
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to /Invoices/{id} | Page loads |
| 2 | Invoice displays | All details visible |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-002: Invoice Header
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | Header section |
| 2 | Invoice number | Displayed |
| 3 | Status badge | Correct color |
| 4 | Total amount | Displayed |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-003: Bill To Section
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | Bill To visible |
| 2 | Client name, address | Correct |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-004: Line Items Table
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | Line items table |
| 2 | All items shown | Correct count |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-005: Action Buttons
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Draft invoice | Edit, Send, Delete visible |
| 2 | Sent invoice | Download, Reminder, Void visible |
| 3 | Paid invoice | Download visible |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-006: Download PDF
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Download PDF" | PDF downloads |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-007: Send Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Send Invoice" | Confirmation |
| 2 | Confirm | Email sent, status updated |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-008: Payment History Section
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with payments | Timeline visible |
| 2 | Payment details | All shown |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-009: Credit Memo Section
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with credits | Credit section visible |
| 2 | Credit details | All shown |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-010: Pay Now Button
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Unpaid invoice | Pay Now visible |
| 2 | Click button | Redirects to Stripe |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-DET-011: Copy Client Link
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Copy Client Link" | Link copied |
| 2 | Access token generated | If not exists |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-012: Early Discount Banner
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Invoice with discount | Banner visible |
| 2 | Shows discount amount | Correct value |
| 3 | Shows deadline | Correct date |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-DET-013: Overdue Banner
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Overdue invoice | Banner visible |
| 2 | Shows days overdue | Correct count |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-DET-014: Back Navigation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click back button | Returns to list |

**Priority:** Low  
**Status:** ⬜ Not Started

---

### 12. Batch Operations

#### TC-BATCH-001: Select Single Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click checkbox | Invoice selected |
| 2 | Counter shows "1 selected" | Correct count |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-BATCH-002: Select Multiple Invoices
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select 3 invoices | All selected |
| 2 | Counter shows "3 selected" | Correct count |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-BATCH-003: Select All
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click header checkbox | All on page selected |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-BATCH-004: Clear Selection
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Clear" button | All deselected |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-BATCH-005: Bulk Send Reminders
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select 3 eligible invoices | Selected |
| 2 | Click "Send Reminders" | Confirmation |
| 3 | Confirm | Reminders sent to all |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-BATCH-006: Bulk Export PDF
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select 3 invoices | Selected |
| 2 | Click "Export PDFs" | 3 PDFs download |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-BATCH-007: Bulk Void
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select 2 eligible invoices | Selected |
| 2 | Click "Void" | Confirmation |
| 3 | Confirm | Both voided |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-BATCH-008: Ineligible Invoices Skipped
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select paid + sent invoices | Mixed selection |
| 2 | Click "Void" | Only sent voided |
| 3 | Paid skipped | Message shown |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 13. Client Portal

#### TC-PORT-001: Access Public Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to /invoice/{token} | Page loads |
| 2 | No login required | Public access |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-PORT-002: Invoice Displays
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View public page | Invoice details |
| 2 | Amount, due date | Visible |
| 3 | Line items | Listed |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PORT-003: Invalid Token
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Access with bad token | Error page |
| 2 | "Invoice Not Found" | Message shown |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-PORT-004: Mark as Viewed
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open public invoice | Status = Sent |
| 2 | Page loads | Status = Viewed |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PORT-005: Pay Now Button
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Unpaid invoice | Pay Now visible |
| 2 | Click button | Redirects to Stripe |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-PORT-006: Download PDF
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Download PDF" | PDF downloads |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PORT-007: Paid Invoice Display
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View paid invoice | Status = Paid |
| 2 | Pay Now hidden | Button not shown |
| 3 | Thank you message | Displayed |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PORT-008: Voided Invoice
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Access voided invoice | VOID shown |
| 2 | Pay Now hidden | Cannot pay |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-PORT-009: Public Layout
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | Clean layout |
| 2 | No navigation menu | Minimal UI |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-PORT-010: Mobile Responsive
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View on mobile | Layout adapts |
| 2 | All content visible | No horizontal scroll |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 14. Reports

#### TC-REP-001: Navigate to Aging Report
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Aging Report" | Page loads |
| 2 | URL is /Reports/Aging | Correct route |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REP-002: Aging Buckets
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View report | 5 buckets shown |
| 2 | Current, 1-30, 31-60, 61-90, 90+ | All present |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REP-003: Aging Summary Cards
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View cards | Totals per bucket |
| 2 | Values correct | Sum verified |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REP-004: Aging CSV Export
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Export CSV" | File downloads |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-REP-005: Navigate to Revenue Dashboard
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Revenue" | Page loads |
| 2 | URL is /Reports/Revenue | Correct route |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REP-006: Revenue Metrics
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View cards | 4 metrics shown |
| 2 | Invoiced, Collected, Outstanding, Overdue | All present |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-REP-007: Revenue Period Filters
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select "This Month" | Data filters |
| 2 | Select "This Year" | Data filters |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-REP-008: Revenue Charts
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | View page | 2 charts visible |
| 2 | Donut chart | Status breakdown |
| 3 | Bar chart | Monthly trend |

**Priority:** Medium  
**Status:** ⬜ Not Started

---

### 15. Security & Authorization

#### TC-SEC-001: Tenant Isolation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | User A in Tenant 1 | |
| 2 | Try to access Tenant 2 invoice | Access denied |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-SEC-002: Authorization Required
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Access /Invoices without login | Redirect to login |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-SEC-003: Role-Based Access
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Specialist role | Full access |
| 2 | Viewer role | View only |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SEC-004: Public Portal No Auth
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Access /invoice/{token} | No login required |
| 2 | Token acts as auth | Access granted |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SEC-005: Token Security
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Token is 32+ characters | Secure length |
| 2 | Random generation | Unpredictable |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SEC-006: API Authentication
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Call API without auth | 401 Unauthorized |
| 2 | Call with valid token | 200 OK |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-SEC-007: Stripe Webhook Security
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Webhook without signature | Rejected |
| 2 | Webhook with valid signature | Processed |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-SEC-008: Soft Delete
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Delete invoice | DateDeleted set |
| 2 | Query invoices | Deleted not returned |
| 3 | Data still in DB | Audit preserved |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-SEC-009: Audit Trail
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create invoice | DateCreated, CreatedBy |
| 2 | Modify invoice | DateModified |
| 3 | Send invoice | SentAt, SentBy |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-SEC-010: HTTPS Required
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Access via HTTP | Redirect to HTTPS |

**Priority:** High  
**Status:** ⬜ Not Started

---

### 16. Invoice Settings

#### TC-SET-001: Navigate to Settings Page
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Click "Invoice Settings" in admin menu | Page loads |
| 2 | URL is /Settings/Invoices | Correct route |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SET-002: Default Settings Created
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | First visit to settings page | Default settings created |
| 2 | Verify prefix = "INV" | Default value |
| 3 | Verify format = "{PREFIX}-{YEAR}-{NUMBER}" | Default format |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-SET-003: Change Invoice Prefix
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Change prefix to "INVOICE" | Preview updates |
| 2 | Save settings | Success message |
| 3 | Create new invoice | Uses new prefix |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SET-004: Change Number Format
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select different format | Preview updates |
| 2 | Save settings | Success |
| 3 | Create new invoice | Uses new format |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-SET-005: Number Reset Frequency
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Set to "Yearly" | Setting saved |
| 2 | Simulate year change | Number resets to 1 |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-SET-006: Number Padding
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Set padding to 3 | Preview shows 001 |
| 2 | Set padding to 7 | Preview shows 0000001 |

**Priority:** Low  
**Status:** ⬜ Not Started

#### TC-SET-007: Default Payment Terms
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Set Net Days to 45 | Setting saved |
| 2 | Create new invoice | Due date = +45 days |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-SET-008: Existing Invoices Unchanged
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Note existing invoice numbers | Record numbers |
| 2 | Change settings | Settings saved |
| 3 | View existing invoices | Numbers unchanged |

**Priority:** High  
**Status:** ⬜ Not Started

---

### 17. Auto-Generate Next Milestone

#### TC-AUTO-001: Enable Auto-Generate
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open Invoice Settings | Page loads |
| 2 | Enable "Auto-generate next milestone" | Toggle on |
| 3 | Save settings | Setting saved |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-AUTO-002: Auto-Generate on Deposit Payment
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create Deposit invoice | Milestone = Deposit |
| 2 | Pay invoice in full | Status = Paid |
| 3 | Verify new invoice | Site Visit Complete invoice created |

**Priority:** Critical  
**Status:** ⬜ Not Started

#### TC-AUTO-003: Milestone Sequence
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Pay Deposit invoice | Creates Site Visit Complete |
| 2 | Pay Site Visit invoice | Creates Draft Report Delivery |
| 3 | Pay Draft Report invoice | Creates Final Delivery |
| 4 | Pay Final Delivery invoice | No new invoice created |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-AUTO-004: Prevent Duplicate Creation
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Site Visit invoice exists | |
| 2 | Pay Deposit invoice | No duplicate created |

**Priority:** High  
**Status:** ⬜ Not Started

#### TC-AUTO-005: Staff Notification
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enable notifications | Email configured |
| 2 | Invoice auto-generated | Email sent to configured address |
| 3 | Email contains correct info | Invoice number, amount, link |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-AUTO-006: Disabled Auto-Generate
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Disable auto-generate | Toggle off |
| 2 | Pay milestone invoice | No new invoice created |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-AUTO-007: Full Payment - No Auto-Generate
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Create Full Payment invoice | Milestone = FullPayment |
| 2 | Pay in full | No new invoice created |

**Priority:** Medium  
**Status:** ⬜ Not Started

#### TC-AUTO-008: Stripe Payment Triggers Auto-Generate
| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Deposit invoice with Stripe | |
| 2 | Complete Stripe payment | Webhook processed |
| 3 | Next milestone created | Auto-generated correctly |

**Priority:** Critical  
**Status:** ⬜ Not Started

---

## Edge Cases

### EC-001: Very Long Line Item Description
Test with 1000+ character description

### EC-002: Unicode in Invoice Data
Test with special characters, emojis, non-Latin scripts

### EC-003: Zero Tax Rate
Verify calculations with 0% tax

### EC-004: 100% Discount
Verify handling of free invoices

### EC-005: Concurrent Payments
Two users recording payment simultaneously

### EC-006: Timezone Handling
Due dates across timezone boundaries

### EC-007: Currency Precision
Verify 2 decimal place handling throughout

### EC-008: Very Large Amounts
Test with $1,000,000+ invoices

### EC-009: Rapid Click Protection
Double-clicking action buttons

### EC-010: Session Timeout
Actions during expired session

---

## Performance Testing

### PERF-001: Invoice List with 1000+ Records
| Metric | Target |
|--------|--------|
| Page Load | < 2 seconds |
| Filter Apply | < 500ms |

### PERF-002: PDF Generation
| Metric | Target |
|--------|--------|
| Single Invoice | < 3 seconds |
| 10 Invoices | < 15 seconds |

### PERF-003: Bulk Operations
| Metric | Target |
|--------|--------|
| 50 Reminders | < 30 seconds |
| 50 PDFs | < 60 seconds |

---

## Regression Testing

After any invoice-related code change, run:

1. TC-INV-001 (Create Invoice)
2. TC-ST-002 (Send Invoice)
3. TC-PAY-001 (Record Payment)
4. TC-STRIPE-002 (Stripe Payment)
5. TC-PDF-001 (Generate PDF)
6. TC-SEC-001 (Tenant Isolation)
7. TC-SET-003 (Invoice Number Prefix)
8. TC-AUTO-002 (Auto-Generate on Payment)

---

## Test Execution Tracking

| Category | Total | Passed | Failed | Blocked | Not Run |
|----------|-------|--------|--------|---------|---------|
| Invoice Creation | 12 | 0 | 0 | 0 | 12 |
| Line Items | 8 | 0 | 0 | 0 | 8 |
| Status Workflow | 15 | 0 | 0 | 0 | 15 |
| Payment Processing | 18 | 0 | 0 | 0 | 18 |
| Stripe Integration | 10 | 0 | 0 | 0 | 10 |
| Credit Memos | 12 | 0 | 0 | 0 | 12 |
| PDF Generation | 8 | 0 | 0 | 0 | 8 |
| Reminders | 10 | 0 | 0 | 0 | 10 |
| Automated Jobs | 6 | 0 | 0 | 0 | 6 |
| Invoice List | 12 | 0 | 0 | 0 | 12 |
| Invoice Detail | 14 | 0 | 0 | 0 | 14 |
| Batch Operations | 8 | 0 | 0 | 0 | 8 |
| Client Portal | 10 | 0 | 0 | 0 | 10 |
| Reports | 8 | 0 | 0 | 0 | 8 |
| Security | 10 | 0 | 0 | 0 | 10 |
| Invoice Settings | 8 | 0 | 0 | 0 | 8 |
| Auto-Generate | 8 | 0 | 0 | 0 | 8 |
| **TOTAL** | **167** | **0** | **0** | **0** | **167** |

---

*Test Plan Version 1.1 - Updated 2025-01-16*
