# Reporting and Analytics

Monitor your business performance with dashboards, reports, and analytics. This guide covers metrics, aging reports, email logs, audit trails, and report archives.

---

## Analytics Overview

The system provides insights into:

| Area | What It Shows |
|------|---------------|
| **Dashboard Metrics** | Key performance indicators |
| **Aging Reports** | Outstanding invoices and studies |
| **Email Logs** | Sent email tracking |
| **Audit Logs** | User activity history |
| **Report Archives** | Generated documents |

---

## Dashboard Analytics

### Key Metrics

Your dashboard displays real-time metrics:

#### Study Metrics

| Metric | Description |
|--------|-------------|
| **Total Studies** | All-time count |
| **Active Studies** | Currently in progress |
| **Completed This Month** | Monthly completions |
| **Average Time to Complete** | Days from request to delivery |
| **Studies by Status** | Distribution across stages |

#### Financial Metrics

| Metric | Description |
|--------|-------------|
| **Revenue MTD** | Month-to-date revenue |
| **Revenue YTD** | Year-to-date revenue |
| **Outstanding Balance** | Unpaid invoice total |
| **Overdue Amount** | Past-due total |
| **Average Invoice Value** | Typical invoice amount |

#### Operational Metrics

| Metric | Description |
|--------|-------------|
| **Pending Requests** | Awaiting your approval |
| **Pending Assignments** | Need specialist assignment |
| **Pending QA** | Reports awaiting review |
| **Overdue Items** | Past target dates |

### Trend Charts

Visual representations of:

- **Studies completed** over time
- **Revenue** by month
- **Average completion time** trend
- **Status distribution** pie chart

### Refreshing Data

Dashboard data refreshes:

- **Automatically** at regular intervals
- **On page load** when you navigate
- **Manually** with refresh button

---

## Aging Reports

### Invoice Aging

Track outstanding accounts receivable:

#### Accessing Invoice Aging

1. Go to **Reports** → **A/R Aging**
2. Or **Specialist Tools** → **A/R Aging**

#### Aging Buckets

| Bucket | Description |
|--------|-------------|
| **Current** | Due in the future |
| **1-30 Days** | 1-30 days past due |
| **31-60 Days** | 31-60 days past due |
| **61-90 Days** | 61-90 days past due |
| **90+ Days** | Over 90 days overdue |

#### Report Columns

| Column | Information |
|--------|-------------|
| **Client** | Customer name |
| **Contact** | Primary contact |
| **Current** | Amount not yet due |
| **1-30** | Amount 1-30 days old |
| **31-60** | Amount 31-60 days old |
| **61-90** | Amount 61-90 days old |
| **90+** | Amount over 90 days |
| **Total** | Total outstanding |

#### Using the Report

| Action | Purpose |
|--------|---------|
| **Sort by total** | Find biggest balances |
| **Sort by oldest** | Prioritize collections |
| **Click client** | View their invoices |
| **Export** | Download for follow-up |

### Study Aging

Track how long studies stay in each status:

#### Accessing Study Aging

1. Go to **Reports** → **Study Aging**
2. Or filter studies by **Days in Stage**

#### What It Shows

| Column | Information |
|--------|-------------|
| **Study** | Community name |
| **Status** | Current workflow status |
| **Days in Stage** | Time in current status |
| **Assigned To** | Responsible specialist |
| **Target Date** | Expected completion |

#### Identifying Issues

Look for studies with:

- **Long time** in one status
- **Past target date**
- **No recent activity**

---

## Email Logs

### Accessing Email Logs

1. Go to **Tenant Admin** → **Email History**
2. Or navigate to `/Admin/EmailLogs`

### Email List

View all sent emails:

| Column | Information |
|--------|-------------|
| **Date** | When sent |
| **To** | Recipient email |
| **Subject** | Email subject |
| **Type** | Email category |
| **Status** | Delivery status |

### Email Statuses

| Status | Meaning |
|--------|---------|
| **Sent** | Successfully sent |
| **Delivered** | Confirmed delivered |
| **Opened** | Recipient opened |
| **Clicked** | Recipient clicked link |
| **Bounced** | Failed to deliver |
| **Failed** | Error sending |

### Email Details

Click an email to see:

- Full **recipient** information
- Complete **subject** and **preview**
- **Timestamps** (sent, delivered, opened)
- **Related study** or invoice
- Full **email content**

### Filtering Emails

Filter by:

| Filter | Options |
|--------|---------|
| **Date Range** | Specific period |
| **Recipient** | Specific email address |
| **Type** | Proposal, Invoice, etc. |
| **Status** | Delivered, Bounced, etc. |
| **Study** | Related study |

### Troubleshooting Delivery

If an email wasn't received:

1. **Check status** - Bounced? Failed?
2. **Verify address** - Typo?
3. **Check spam** - Have recipient check
4. **Resend** - Try again if needed

---

## Audit and Compliance

### Activity Logging

The system logs user actions:

| Logged Action | Examples |
|---------------|----------|
| **Authentication** | Login, logout, failed attempts |
| **Data Changes** | Create, update, delete records |
| **Workflow Actions** | Approve, assign, publish |
| **Document Access** | View, download documents |
| **Settings Changes** | Configuration updates |

### Accessing Audit Logs

If exposed in the UI:

1. Go to **Admin** → **Audit Logs**
2. Or check system settings

### Audit Log Contents

| Field | Information |
|-------|-------------|
| **Timestamp** | When action occurred |
| **User** | Who performed action |
| **Action** | What was done |
| **Entity** | What was affected |
| **Details** | Additional context |
| **IP Address** | Where from |

### Filtering Audit Logs

Filter by:

- **User** - Specific person
- **Action Type** - Login, update, etc.
- **Date Range** - Specific period
- **Entity** - Studies, invoices, etc.

### Audit Use Cases

| Purpose | What to Look For |
|---------|------------------|
| **Security review** | Failed logins, unusual access |
| **Compliance** | Who accessed what data |
| **Troubleshooting** | What changed and when |
| **Training** | User activity patterns |

### Data Retention

Audit logs are typically retained for:

- **Active data** - Indefinitely
- **Archived data** - Per retention policy
- **Compliance** - As required by law

---

## Generated Reports Archive

### Accessing Report Archive

1. Go to **Documents** → **Generated Reports**
2. Or filter documents by type

### What's Archived

| Document Type | Retention |
|---------------|-----------|
| **Final Reports** | Permanent |
| **Proposals** | Long-term |
| **Invoices** | 7+ years |
| **Signed Documents** | Permanent |

### Report Versions

Multiple versions may exist:

| Version | Description |
|---------|-------------|
| **Draft** | Working version |
| **Submitted** | Sent for QA |
| **Final** | Published version |
| **Amended** | If changes were made |

### Accessing Old Reports

1. Navigate to the **study**
2. Go to **Documents** tab
3. See all generated versions
4. Download any version

### Report History

For each report, see:

- **Generation date** - When created
- **Generated by** - Who created it
- **Version** - Draft, final, etc.
- **File size** - Document size

---

## Custom Reports

### Available Reports

| Report | Description |
|--------|-------------|
| **Study Summary** | Overview of all studies |
| **Revenue Report** | Financial performance |
| **Specialist Workload** | Work distribution |
| **Client Report** | Client-specific summary |
| **Status Report** | Studies by status |

### Generating Reports

1. Go to **Reports** section
2. Select **report type**
3. Configure **parameters** (dates, filters)
4. Click **"Generate"**
5. **View** or **download**

### Export Options

Reports can be exported as:

| Format | Best For |
|--------|----------|
| **PDF** | Printing, sharing |
| **Excel** | Analysis, manipulation |
| **CSV** | Data import |

---

## Performance Indicators

### Key Performance Indicators (KPIs)

Track these metrics for business health:

#### Efficiency KPIs

| KPI | Target | Description |
|-----|--------|-------------|
| **Time to Proposal** | < 5 days | Request to proposal sent |
| **Time to Completion** | < 60 days | Request to delivery |
| **QA Turnaround** | < 3 days | Submit to approve |

#### Financial KPIs

| KPI | Target | Description |
|-----|--------|-------------|
| **Collection Rate** | > 95% | Invoices collected |
| **Days Outstanding** | < 30 | Average collection time |
| **Revenue per Study** | Varies | Average invoice value |

#### Quality KPIs

| KPI | Target | Description |
|-----|--------|-------------|
| **QA Pass Rate** | > 90% | First-time approvals |
| **Amendment Rate** | < 10% | Scope changes |
| **Client Satisfaction** | > 4.5/5 | If tracked |

---

## Data Export

### Exporting Data

For external analysis:

1. Navigate to the **report** or **list**
2. Click **"Export"**
3. Select **format** (Excel, CSV)
4. Download file

### Exportable Data

| Data | Location |
|------|----------|
| **Study List** | Reserve Studies |
| **Invoice List** | Invoices |
| **Client List** | Customers |
| **Email Log** | Email History |
| **User List** | Manage Users |

### Scheduled Reports

If available:

1. Configure **report parameters**
2. Set **schedule** (daily, weekly, monthly)
3. Specify **recipients**
4. Reports delivered automatically

---

## Best Practices

### Regular Monitoring

| Task | Frequency |
|------|-----------|
| Check dashboard | Daily |
| Review aging report | Weekly |
| Audit unusual activity | As needed |
| Review KPIs | Monthly |

### Using Data Effectively

| Practice | Benefit |
|----------|---------|
| Set targets | Measure progress |
| Track trends | Identify patterns |
| Act on insights | Improve operations |
| Share metrics | Team alignment |

### Maintaining Data Quality

| Practice | Benefit |
|----------|---------|
| Complete all fields | Accurate reporting |
| Update promptly | Current data |
| Review regularly | Catch errors |
| Clean up old data | Better performance |

---

## Troubleshooting

### Common Issues

**Q: Dashboard numbers seem wrong.**
A: Refresh the page. Check date filters. Data may be cached.

**Q: Report won't generate.**
A: Check for required parameters. Try smaller date range. Contact support.

**Q: Can't find an email in logs.**
A: Expand date range. Check spelling of recipient. May not have been sent.

**Q: Export is missing data.**
A: Check filters. Ensure you have permission. Try different format.

---

## Summary

Use reporting tools to:

1. ✅ **Monitor** - Dashboard for daily oversight
2. ✅ **Collect** - Aging reports for receivables
3. ✅ **Track** - Email logs for communication
4. ✅ **Audit** - Activity logs for compliance
5. ✅ **Archive** - Report history for records
6. ✅ **Analyze** - Exports for deep analysis

---

[← Financial Management](./financial-management.md) | [Back to Admin Guide](./index.md)
