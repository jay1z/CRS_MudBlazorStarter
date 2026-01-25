# Financial Management

Manage your organization's invoicing, payments, and financial tracking. This guide covers invoice settings, creating invoices, payment processing, credit memos, and aging reports.

---

## Financial Overview

Financial management in the system includes:

| Area | Description |
|------|-------------|
| **Invoice Settings** | Configure billing defaults |
| **Invoice Creation** | Generate bills for services |
| **Invoice Management** | Track and send invoices |
| **Payment Processing** | Record and track payments |
| **Credit Memos** | Handle credits and adjustments |
| **Aging Reports** | Monitor accounts receivable |

---

## Invoice Settings

### Accessing Invoice Settings

1. Go to **Tenant Admin** → **Invoice Settings**
2. Or navigate to `/Settings/Invoices`

### Invoice Numbering

Configure how invoice numbers are generated:

| Setting | Description | Example |
|---------|-------------|---------|
| **Prefix** | Text before number | "INV-" |
| **Starting Number** | First invoice number | 1001 |
| **Date Format** | Include date in number | "INV-2024-0001" |
| **Auto-Increment** | Automatic numbering | Yes |

### Payment Terms

Set default payment terms:

| Setting | Description |
|---------|-------------|
| **Due Days** | Days until due (e.g., 30) |
| **Payment Methods** | Credit card, check, ACH |
| **Accepted Cards** | Visa, MC, Amex, Discover |

### Late Fee Configuration

Configure late payment penalties:

| Setting | Options |
|---------|---------|
| **Late Fee Type** | Percentage or flat fee |
| **Late Fee Amount** | e.g., 1.5% or $25 |
| **Grace Period** | Days after due before fee |
| **Apply Automatically** | Auto-calculate late fees |

### Invoice Branding

Customize invoice appearance:

| Element | Description |
|---------|-------------|
| **Logo** | Company logo on invoices |
| **Company Info** | Name, address, phone |
| **Footer** | Payment instructions, thank you |
| **Colors** | Match your branding |

---

## Creating Invoices

### When Invoices Are Created

Invoices can be created:

| Method | When |
|--------|------|
| **Manual** | You create when ready |
| **Automatic** | System creates at milestones |
| **Milestone-based** | At proposal acceptance, site visit, delivery |

### Manual Invoice Creation

1. Go to **Invoices** → **Create Invoice**
2. Or from a study, click **"Create Invoice"**

### Invoice Form

| Field | Description |
|-------|-------------|
| **Study** | Link to reserve study |
| **Invoice Number** | Auto-generated or custom |
| **Invoice Date** | Date of invoice |
| **Due Date** | Payment deadline |
| **Bill To** | Client name and address |
| **Line Items** | Services and amounts |
| **Notes** | Additional information |

### Line Items

Add line items for services:

| Field | Description |
|-------|-------------|
| **Description** | Service description |
| **Quantity** | Number of units |
| **Unit Price** | Price per unit |
| **Amount** | Total (auto-calculated) |

### Automatic Invoicing

If configured, invoices are auto-created at:

| Milestone | Typical Amount |
|-----------|----------------|
| **Proposal Acceptance** | Deposit (25-50%) |
| **Site Visit Complete** | Progress payment |
| **Report Delivery** | Final payment |

---

## Managing Invoices

### Invoice List

View all invoices:

1. Go to **Invoices** in the main menu
2. See list of all invoices

### List Columns

| Column | Information |
|--------|-------------|
| **Invoice #** | Unique identifier |
| **Client** | Community/HOA name |
| **Date** | Invoice date |
| **Due Date** | Payment deadline |
| **Amount** | Total amount |
| **Paid** | Amount paid |
| **Balance** | Outstanding amount |
| **Status** | Current status |

### Invoice Statuses

| Status | Meaning |
|--------|---------|
| **Draft** | Not yet sent |
| **Sent** | Delivered to client |
| **Viewed** | Client has opened |
| **Paid** | Payment received |
| **Partially Paid** | Some payment received |
| **Overdue** | Past due date |
| **Voided** | Cancelled |

### Filtering Invoices

Filter by:

- **Status** - Unpaid, Paid, Overdue
- **Date Range** - Specific period
- **Client** - Specific HOA
- **Study** - Specific reserve study

### Invoice Actions

| Action | Description |
|--------|-------------|
| **View** | See invoice details |
| **Edit** | Modify (if draft) |
| **Send** | Email to client |
| **Download** | Get PDF |
| **Record Payment** | Enter payment |
| **Void** | Cancel invoice |
| **Duplicate** | Create copy |

---

## Sending Invoices

### Sending to Client

1. Open the invoice
2. Click **"Send"** or **"Send Invoice"**
3. Review recipient email
4. Add optional message
5. Confirm and send

### What Client Receives

- 📧 **Email** with invoice summary
- 📎 **PDF attachment** (if configured)
- 🔗 **Pay Now link** to portal
- 💳 **Payment instructions**

### Resending Invoices

To resend:

1. Open the invoice
2. Click **"Resend"**
3. Confirm sending
4. New email delivered

### Send Reminders

For unpaid invoices:

1. Select unpaid invoices
2. Click **"Send Reminder"**
3. Reminder email sent
4. Reminder logged

---

## Recording Payments

### When Payment is Received

Whether online or offline:

1. Open the invoice
2. Click **"Record Payment"**
3. Enter payment details

### Payment Form

| Field | Description |
|-------|-------------|
| **Amount** | Payment amount |
| **Date** | Payment date |
| **Method** | Credit card, check, etc. |
| **Reference** | Check number, transaction ID |
| **Notes** | Additional info |

### Online Payments

When clients pay online via Stripe:

1. Payment processed automatically
2. Invoice updated to **"Paid"**
3. Receipt generated
4. You're notified

### Partial Payments

If payment is less than total:

1. Record the partial amount
2. Status becomes **"Partially Paid"**
3. Balance updates
4. Record additional payments as received

### Payment History

View all payments on an invoice:

- Payment date
- Amount
- Method
- Who recorded it
- Reference number

---

## Credit Memos

### When to Use

Create credit memos for:

| Situation | Example |
|-----------|---------|
| **Overpayment** | Client paid too much |
| **Discount** | Agreed-upon reduction |
| **Error** | Invoice was incorrect |
| **Cancellation** | Partial service cancelled |

### Creating a Credit Memo

1. Go to **Invoices** → **Credit Memos**
2. Or click **"Create Credit Memo"** from invoice
3. Fill in details:

| Field | Description |
|-------|-------------|
| **Client** | Who receives credit |
| **Amount** | Credit amount |
| **Reason** | Why issued |
| **Related Invoice** | Original invoice |

4. Save credit memo

### Applying Credits

Credits can be:

- **Applied** to future invoices
- **Refunded** to client
- **Carried** as credit balance

### Credit Memo Tracking

View credit memos in:

- Credit memo list
- Client account balance
- Invoice adjustments

---

## Payment Tracking

### Outstanding Balances

View who owes money:

1. Go to **Invoices** → Filter by **Unpaid**
2. Or view **Aging Report**

### Client Balances

See total owed by each client:

1. Go to **Customers**
2. View **Balance** column
3. Click for details

### Payment History

View all payments received:

1. Go to **Reports** → **Payments**
2. Filter by date range
3. See all transactions

---

## Aging Reports

### Accessing Aging Report

1. Go to **Reports** → **A/R Aging**
2. Or **Specialist Tools** → **A/R Aging**

### Aging Buckets

Invoices grouped by age:

| Bucket | Age |
|--------|-----|
| **Current** | Not yet due |
| **1-30 Days** | 1-30 days past due |
| **31-60 Days** | 31-60 days past due |
| **61-90 Days** | 61-90 days past due |
| **90+ Days** | Over 90 days past due |

### Using the Aging Report

| Column | Information |
|--------|-------------|
| **Client** | Customer name |
| **Current** | Not yet due amount |
| **1-30** | Amount in bucket |
| **31-60** | Amount in bucket |
| **61-90** | Amount in bucket |
| **90+** | Amount in bucket |
| **Total** | Total outstanding |

### Actions from Aging Report

- **Click client** - View their invoices
- **Send reminders** - Email overdue notices
- **Export** - Download for follow-up

---

## Stripe Integration

### Payment Processing

Online payments are processed through Stripe:

| Feature | Description |
|---------|-------------|
| **Card payments** | Visa, MC, Amex, Discover |
| **Secure processing** | PCI compliant |
| **Automatic receipts** | Sent to clients |
| **Dashboard sync** | Updates automatically |

### Stripe Dashboard

For detailed transaction info:

1. Log into Stripe dashboard
2. View payments and payouts
3. Handle disputes if any
4. Download reports

### Webhooks

Stripe webhooks automatically:

- Update invoice status
- Record payments
- Trigger notifications
- Update customer records

### Refunds

To refund a payment:

1. Locate the payment
2. Click **"Refund"**
3. Enter refund amount
4. Confirm refund
5. Funds returned to client

---

## Revenue Reporting

### Revenue Dashboard

View revenue metrics:

| Metric | Description |
|--------|-------------|
| **MTD Revenue** | Month-to-date |
| **YTD Revenue** | Year-to-date |
| **Outstanding** | Unpaid invoices |
| **Collected** | Payments received |

### Revenue by Period

View revenue trends:

1. Go to **Reports** → **Revenue**
2. Select date range
3. View by month, quarter, year

### Revenue by Client

See which clients generate revenue:

1. Filter by client
2. View total billed and paid
3. Export for analysis

---

## Best Practices

### Invoicing

| Practice | Benefit |
|----------|---------|
| Invoice promptly | Faster payment |
| Clear descriptions | No confusion |
| Consistent numbering | Easy tracking |
| Send reminders | Reduce overdue |

### Collections

| Stage | Action |
|-------|--------|
| **Due date approaching** | Send reminder |
| **1-7 days overdue** | Friendly reminder |
| **14+ days overdue** | Phone follow-up |
| **30+ days overdue** | Escalate |
| **60+ days overdue** | Consider collections |

### Reconciliation

| Task | Frequency |
|------|-----------|
| Match payments to invoices | Daily/weekly |
| Review aging report | Weekly |
| Reconcile with bank | Monthly |
| Write off bad debt | As needed |

---

## Troubleshooting

### Common Issues

**Q: Client says they paid but invoice shows unpaid.**
A: Check if payment was processed. Look in Stripe. May need manual recording.

**Q: Invoice won't send.**
A: Verify client email. Check email logs. Resend if needed.

**Q: Late fees aren't calculating.**
A: Check late fee settings. Verify due date. Run fee calculation.

**Q: Can't void an invoice with payments.**
A: Refund or adjust payments first, then void.

**Q: Payment went to wrong invoice.**
A: Record a negative payment to reverse, then apply to correct invoice.

---

## Next Steps

Monitor your business performance:

- **[Reporting and Analytics →](./reporting-and-analytics.md)** - Business intelligence

---

[← Settings and Configuration](./settings-and-configuration.md) | [Reporting and Analytics →](./reporting-and-analytics.md)
