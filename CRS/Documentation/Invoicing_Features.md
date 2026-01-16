# Invoicing Features

This document tracks the implementation status of invoicing features for the CRS application.

**Last Updated:** 2025-01-16  
**Status:** All Top 10 Priority Features Complete ✅

---

## Table of Contents

1. [Feature Summary](#feature-summary)
2. [Implemented Features](#implemented-features)
3. [Future Enhancements](#future-enhancements)
4. [Technical Architecture](#technical-architecture)
5. [File Reference](#file-reference)
6. [Changelog](#changelog)

---

## Feature Summary

| Category | Features | Status |
|----------|----------|--------|
| Core Invoice Management | 5 features | ✅ Complete |
| Payment Schedules | 6 features | ✅ Complete |
| Payment Terms | 5 features | ✅ Complete |
| Invoice Actions | 6 features | ✅ Complete |
| Invoice Display | 4 features | ✅ Complete |
| Tax & Discounts | 4 features | ✅ Complete |
| Invoice PDF | 5 features | ✅ Complete |
| Invoice Reminders | 7 features | ✅ Complete |
| Automated Jobs | 3 features | ✅ Complete |
| Invoice Pages | 6 features | ✅ Complete |
| Online Payment (Stripe) | 5 features | ✅ Complete |
| A/R Aging Report | 4 features | ✅ Complete |
| Payment History | 4 features | ✅ Complete |
| Revenue Dashboard | 7 features | ✅ Complete |
| Client Invoice Portal | 6 features | ✅ Complete |
| Credit Memos | 9 features | ✅ Complete |
| Batch Operations | 6 features | ✅ Complete |
| Invoice Number Customization | 7 features | ✅ Complete |
| Auto-Generate Next Milestone | 6 features | ✅ Complete |
| Default Payment Terms | 6 features | ✅ Complete |
| Invoice Branding / Templates | 13 features | ✅ Complete |

**Total: 124 features implemented**

---

## Implemented Features

### Core Invoice Management

| Feature | Description | Status |
|---------|-------------|--------|
| Invoice Creation | Create invoices manually or from proposals | ✅ |
| Milestone Invoicing | Create invoices for specific payment milestones | ✅ |
| Line Items | Add, edit, remove line items | ✅ |
| Invoice Status Tracking | Draft, Finalized, Sent, Viewed, PartiallyPaid, Paid, Overdue, Voided | ✅ |
| Invoice Numbering | Auto-generated invoice numbers (INV-YYYY-#####) | ✅ |

### Payment Schedules

| Feature | Description | Status |
|---------|-------------|--------|
| Full Payment | Single payment option | ✅ |
| 50/50 Split | Deposit + Final payment | ✅ |
| Thirds | 3 equal payments | ✅ |
| Quarters | 4 equal payments | ✅ |
| Custom Schedule | Custom deposit percentage | ✅ |
| Prepayment Discount | Discount for paying upfront | ✅ |

### Payment Terms

| Feature | Description | Status |
|---------|-------------|--------|
| Net Days | Configurable due dates (Net 30, Net 45, etc.) | ✅ |
| Early Payment Discount | 2/10 Net 30 style discounts | ✅ |
| Late Payment Interest | Monthly interest rate for overdue invoices | ✅ |
| Grace Period | Days after due date before interest accrues | ✅ |
| Minimum Deposit | Minimum deposit amount requirement | ✅ |

### Invoice Actions

| Feature | Description | Status |
|---------|-------------|--------|
| Edit Invoice | Modify draft invoices | ✅ |
| Send Invoice | Email invoice to client | ✅ |
| Record Payment | Track payments received (manual + automatic) | ✅ |
| Delete Invoice | Remove draft invoices | ✅ |
| Void Invoice | Cancel sent invoices (audit trail preserved) | ✅ |
| Duplicate Invoice | Create copy as new draft | ✅ |

### Invoice Display

| Feature | Description | Status |
|---------|-------------|--------|
| Invoice View Dialog | View invoice details in modal | ✅ |
| Early Payment Banner | Shows discount availability and amount | ✅ |
| Late Interest Display | Shows accrued interest on overdue invoices | ✅ |
| Overdue Indicator | Visual indicator for overdue invoices | ✅ |

### Tax & Discounts

| Feature | Description | Status |
|---------|-------------|--------|
| Tax Rate | Configurable tax percentage | ✅ |
| Tax Amount | Calculated tax on subtotal | ✅ |
| Discount Amount | Fixed or calculated discounts | ✅ |
| Discount Description | Reason for discount | ✅ |

### Invoice PDF Generation

| Feature | Description | Status |
|---------|-------------|--------|
| PDF Generation | Generate PDF using QuestPDF | ✅ |
| Professional Template | Header, line items, totals, terms | ✅ |
| Download Button | Download PDF from InvoicePanel and Detail pages | ✅ |
| Early Discount Notice | Shows discount availability on PDF | ✅ |
| VOID Watermark | Shows watermark on voided invoices | ✅ |

### Invoice Reminders

| Feature | Description | Status |
|---------|-------------|--------|
| Manual Send Reminder | Button to send reminder from InvoicePanel | ✅ |
| Reminder Tracking | ReminderCount, LastReminderSent fields | ✅ |
| Reminder Email | Uses InvoiceMailable with IsReminder flag | ✅ |
| Days Past Due | Shows overdue days in reminder | ✅ |
| Automated Reminders | InvoiceReminderInvocable runs daily at 8 AM | ✅ |
| Reminder Schedule | -3, 1, 7, 14, 30 days from due date | ✅ |
| Max Reminders | Stops after 5 reminders per invoice | ✅ |

### Automated Jobs

| Feature | Description | Status |
|---------|-------------|--------|
| Late Interest Job | LateInterestInvocable runs daily at 3 AM | ✅ |
| Overdue Status Update | Auto-marks invoices as overdue | ✅ |
| Interest Calculation | Applies late interest to overdue invoices | ✅ |

### Invoice Pages

| Feature | Description | Route |
|---------|-------------|-------|
| Invoices List Page | View all invoices with filters | `/Invoices` |
| Invoice Detail Page | View single invoice details | `/Invoices/{id}` |
| QuickBooks CSV Export | Export invoices to CSV format | Button on list |
| Summary Dashboard | Total/Paid/Outstanding/Overdue cards | On list page |
| Filters | Status, date range, search filters | On list page |
| Navigation Link | Added to Specialist Tools menu | ✅ |

### Online Payment via Stripe

| Feature | Description | Status |
|---------|-------------|--------|
| Pay Now Button | Stripe Checkout integration on invoice detail | ✅ |
| Stripe Checkout Session | Creates one-time payment session | ✅ |
| Payment Webhook | Handles payment_intent.succeeded events | ✅ |
| Payment Success Page | Confirmation page after payment | `/Invoices/PaymentSuccess` |
| Invoice Fields | StripePaymentIntentId, PaymentUrl, PaymentUrlExpires | ✅ |

### A/R Aging Report

| Feature | Description | Route |
|---------|-------------|-------|
| Aging Report Page | View invoices by aging bucket | `/Reports/Aging` |
| Aging Buckets | Current, 1-30, 31-60, 61-90, 90+ days | ✅ |
| Summary Cards | Totals per aging bucket | ✅ |
| CSV Export | Export aging report to CSV | Button on page |

### Payment History / Audit Trail

| Feature | Description | Status |
|---------|-------------|--------|
| PaymentRecord Model | Tracks individual payments with audit fields | ✅ |
| Automatic vs Manual | IsAutomatic flag for Stripe vs manual payments | ✅ |
| Payment Timeline | Visual timeline on invoice detail page | ✅ |
| Recorded By User | Tracks who recorded each payment | ✅ |

### Revenue Dashboard

| Feature | Description | Route |
|---------|-------------|-------|
| Revenue Page | Key metrics and charts | `/Reports/Revenue` |
| Period Filters | Month, Quarter, Year, All Time | ✅ |
| Key Metrics | Invoiced, Collected, Outstanding, Overdue | ✅ |
| Status Chart | Donut chart of revenue by status | ✅ |
| Trend Chart | Bar chart of monthly invoiced vs collected | ✅ |
| Top Clients | Table of clients by revenue | ✅ |
| Recent Payments | Timeline of recent payments | ✅ |

### Client Invoice Portal

| Feature | Description | Route |
|---------|-------------|-------|
| Public Invoice Page | Token-based access for clients | `/invoice/{token}` |
| Access Token | Unique secure token per invoice | ✅ |
| Copy Client Link | Button to copy share URL | ✅ |
| Pay Online | Clients can pay via Stripe | ✅ |
| Download PDF | Clients can download invoice PDF | ✅ |
| Public Layout | Clean, minimal layout for clients | ✅ |

### Credit Memos / Refunds

| Feature | Description | Status |
|---------|-------------|--------|
| CreditMemo Model | Tracks credits, refunds, adjustments | ✅ |
| Credit Memo Status | Draft, Applied, Voided | ✅ |
| Credit Memo Reasons | Refund, Pricing Adjustment, Service Credit, etc. | ✅ |
| Apply Credit | Reduces invoice balance | ✅ |
| Void Credit | Reverses applied credit | ✅ |
| Credit Memo PDF | Professional PDF generation | ✅ |
| Credit Memo Dialog | UI for issuing credits | ✅ |
| Credit Display | Shows credits on invoice detail | ✅ |
| Net Balance | Computed property includes credits | ✅ |

### Batch Operations

| Feature | Description | Status |
|---------|-------------|--------|
| Multi-Select Table | Checkbox selection on invoice list | ✅ |
| Bulk Send Reminders | Send reminders to selected invoices | ✅ |
| Bulk Export PDF | Download PDFs for selected invoices | ✅ |
| Bulk Void | Void selected invoices with confirmation | ✅ |
| Selection Counter | Shows count of selected items | ✅ |
| Clear Selection | Button to clear all selections | ✅ |

### Invoice Number Customization (Recently Added)

| Feature | Description | Status |
|---------|-------------|--------|
| TenantInvoiceSettings Model | Per-tenant invoice configuration | ✅ |
| Custom Prefix | Configurable invoice prefix (INV, INVOICE, etc.) | ✅ |
| Number Format | Multiple format patterns available | ✅ |
| Number Padding | Configurable digit count (3-10) | ✅ |
| Reset Frequency | Never, Yearly, or Monthly reset | ✅ |
| Number Preview | Live preview of next invoice number | ✅ |
| Settings Page | Full configuration UI at /Settings/Invoices | ✅ |

### Auto-Generate Next Milestone (Recently Added)

| Feature | Description | Status |
|---------|-------------|--------|
| Auto-Generate Setting | Toggle per tenant | ✅ |
| Milestone Sequence | Deposit → Site Visit → Draft → Final | ✅ |
| On Payment Trigger | Creates next invoice when current is paid | ✅ |
| Staff Notification | Email notification when auto-generated | ✅ |
| Duplicate Prevention | Checks if next milestone already exists | ✅ |
| Settings Integration | Uses tenant defaults for new invoice | ✅ |

### Default Payment Terms (Recently Added)

| Feature | Description | Status |
|---------|-------------|--------|
| Default Net Days | Configurable default due date offset | ✅ |
| Default Early Discount | Pre-configured early payment discount | ✅ |
| Default Late Interest | Pre-configured late interest rate | ✅ |
| Default Grace Period | Pre-configured grace period | ✅ |
| Default Tax Rate | Pre-configured tax rate | ✅ |
| Tax Label | Customizable tax label (Tax, VAT, GST) | ✅ |

### Invoice Branding / Templates (Recently Added)

| Feature | Description | Status |
|---------|-------------|--------|
| UseTenantBranding Toggle | Enable/disable custom branding | ✅ |
| Primary Color | Customizable primary brand color | ✅ |
| Secondary Color | Customizable secondary brand color | ✅ |
| Logo URL | Company logo on invoices | ✅ |
| Company Info | Name, address, phone, email, website | ✅ |
| Tagline | Custom tagline under company name | ✅ |
| Footer Text | Customizable footer message | ✅ |
| Default Terms | Pre-configured terms & conditions | ✅ |
| Payment Instructions | Displayed on PDF | ✅ |
| PAID Watermark | Toggle for paid invoice watermark | ✅ |
| Credit Memo Branding | Same branding on credit memos | ✅ |
| Color Picker UI | Visual color selection in settings | ✅ |
| Live Color Preview | Preview gradient in settings | ✅ |

---

## Future Enhancements

### Lower Priority - Nice to Have

#### 1. Recurring Invoices
- Create recurring invoice template
- Set frequency (monthly, quarterly, annual)
- Auto-generate invoices on schedule
- Track recurring series

**Estimated Effort:** High  
**Dependencies:** Scheduler, new models

~~#### 2. Invoice Templates / Branding~~ ✅ COMPLETE

#### 3. QuickBooks API Integration
- Export invoices to QBO format
- Export payments
- Two-way sync with QuickBooks

**Estimated Effort:** High  
**Dependencies:** QuickBooks API

~~#### 4. Invoice Number Customization~~ ✅ COMPLETE

~~#### 5. Auto-Generate Next Milestone Invoice~~ ✅ COMPLETE

---

## Technical Architecture

### Services

| Service | Interface | Description |
|---------|-----------|-------------|
| InvoiceService | IInvoiceService | Core invoice CRUD and business logic |
| InvoicePdfService | IInvoicePdfService | PDF generation using QuestPDF |
| InvoicePaymentService | IInvoicePaymentService | Stripe payment integration |
| CreditMemoService | ICreditMemoService | Credit memo management |
| CreditMemoPdfService | ICreditMemoPdfService | Credit memo PDF generation |

### Models

| Model | Description |
|-------|-------------|
| Invoice | Main invoice entity |
| InvoiceLineItem | Line items for invoices |
| PaymentRecord | Payment audit trail |
| CreditMemo | Credit memos and refunds |

### Jobs (Coravel)

| Job | Schedule | Description |
|-----|----------|-------------|
| LateInterestInvocable | Daily 3 AM | Apply late interest to overdue invoices |
| InvoiceReminderInvocable | Daily 8 AM | Send automated payment reminders |

### Controllers

| Controller | Description |
|------------|-------------|
| InvoicesController | API for PDF download, payment URLs, share links |
| StripeWebhookController | Handles Stripe payment webhooks |

### Pages (Blazor)

| Page | Route | Description |
|------|-------|-------------|
| Index.razor | /Invoices | Invoice list with filters and batch operations |
| Details.razor | /Invoices/{id} | Invoice detail with actions |
| PaymentSuccess.razor | /Invoices/PaymentSuccess | Stripe payment confirmation |
| PublicView.razor | /invoice/{token} | Public client invoice view |
| Aging.razor | /Reports/Aging | A/R aging report |
| Revenue.razor | /Reports/Revenue | Revenue dashboard |
| InvoiceSettings.razor | /Settings/Invoices | Invoice configuration settings |

### Components

| Component | Description |
|-----------|-------------|
| InvoicePanel.razor | Compact invoice display panel |
| CreditMemoDialog.razor | Dialog for issuing credit memos |
| InvoicePaymentDialog.razor | Dialog for recording payments |

---

## File Reference

### Models
- `CRS/Models/Invoice.cs`
- `CRS/Models/InvoiceLineItem.cs`
- `CRS/Models/PaymentRecord.cs`
- `CRS/Models/CreditMemo.cs`
- `CRS/Models/TenantInvoiceSettings.cs`
- `CRS/Models/InvoiceBranding.cs`

### Services
- `CRS/Services/InvoiceService.cs`
- `CRS/Services/InvoicePdfService.cs`
- `CRS/Services/InvoicePaymentService.cs`
- `CRS/Services/CreditMemoService.cs`
- `CRS/Services/CreditMemoPdfService.cs`

### Interfaces
- `CRS/Services/Interfaces/IInvoiceService.cs`
- `CRS/Services/Interfaces/IInvoicePdfService.cs`
- `CRS/Services/Interfaces/IInvoicePaymentService.cs`
- `CRS/Services/Interfaces/ICreditMemoService.cs`

### Jobs
- `CRS/Jobs/LateInterestInvocable.cs`
- `CRS/Jobs/InvoiceReminderInvocable.cs`

### Controllers
- `CRS/Controllers/InvoicesController.cs`
- `CRS/Controllers/StripeWebhookController.cs`

### Pages
- `CRS/Components/Pages/Invoices/Index.razor`
- `CRS/Components/Pages/Invoices/Details.razor`
- `CRS/Components/Pages/Invoices/PaymentSuccess.razor`
- `CRS/Components/Pages/Invoices/PublicView.razor`
- `CRS/Components/Pages/Reports/Aging.razor`
- `CRS/Components/Pages/Reports/Revenue.razor`

### Components
- `CRS/Components/Shared/Invoices/InvoicePanel.razor`
- `CRS/Components/Shared/Invoices/CreditMemoDialog.razor`
- `CRS/Components/Shared/Invoices/InvoicePaymentDialog.razor`

### Layouts
- `CRS/Components/Layout/PublicLayout.razor`

---

## Changelog

| Date | Version | Change |
|------|---------|--------|
| 2025-01-16 | 1.0 | Initial invoice system implementation |
| 2025-01-16 | 1.1 | Added payment terms (Net days, early discount, late interest) |
| 2025-01-16 | 1.2 | Added Void and Duplicate invoice actions |
| 2025-01-16 | 2.0 | Implemented Invoice PDF Generation with QuestPDF |
| 2025-01-16 | 2.1 | Implemented Late Interest Scheduled Job (daily at 3 AM) |
| 2025-01-16 | 2.2 | Implemented Manual Send Reminder button with tracking |
| 2025-01-16 | 3.0 | Created Invoices List Page (/Invoices) with filters and summary |
| 2025-01-16 | 3.1 | Created Invoice Detail Page (/Invoices/{id}) |
| 2025-01-16 | 3.2 | Added QuickBooks-compatible CSV export |
| 2025-01-16 | 4.0 | Implemented Online Payment via Stripe |
| 2025-01-16 | 4.1 | Implemented Automated Invoice Reminders (daily at 8 AM) |
| 2025-01-16 | 4.2 | Created A/R Aging Report page (/Reports/Aging) |
| 2025-01-16 | 5.0 | Added PaymentRecord model and payment history tracking |
| 2025-01-16 | 5.1 | Created Revenue Dashboard page (/Reports/Revenue) |
| 2025-01-16 | 5.2 | Created Client Invoice Portal (/invoice/{token}) |
| 2025-01-16 | 6.0 | Added CreditMemo model with service and PDF generation |
| 2025-01-16 | 6.1 | Implemented Batch Operations (reminders, PDF export, void) |
| 2025-01-16 | 6.2 | ALL TOP 10 PRIORITY FEATURES COMPLETE 🎉 |
| 2025-01-16 | 7.0 | Added TenantInvoiceSettings model for invoice customization |
| 2025-01-16 | 7.1 | Created Invoice Settings page (/Settings/Invoices) |
| 2025-01-16 | 7.2 | Implemented customizable invoice numbering (prefix, format, padding) |
| 2025-01-16 | 7.3 | Implemented auto-generate next milestone invoice on payment |
| 2025-01-16 | 7.4 | Added staff notification email for auto-generated invoices |
| 2025-01-16 | 7.5 | Added default payment terms settings per tenant |
| 2025-01-16 | 8.0 | Added invoice branding with custom colors (InvoiceBranding model) |
| 2025-01-16 | 8.1 | Updated InvoicePdfService to use tenant branding |
| 2025-01-16 | 8.2 | Updated CreditMemoPdfService to use tenant branding |
| 2025-01-16 | 8.3 | Added branding section to Invoice Settings page |
| 2025-01-16 | 8.4 | Added color pickers, logo URL, company info fields |
| 2025-01-16 | 8.5 | **124 TOTAL FEATURES IMPLEMENTED** |

---

*Document maintained by the development team. For questions, contact the project lead.*
