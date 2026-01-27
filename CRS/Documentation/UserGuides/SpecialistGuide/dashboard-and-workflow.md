# Dashboard and Workflow

The dashboard is your command center for managing reserve studies. This guide covers the metrics, Kanban board, and workflow management tools.

---

## Dashboard Overview

Your specialist dashboard provides a comprehensive view of your workload.

### Metrics Cards

The top section displays key performance indicators:

| Metric | Description |
|--------|-------------|
| **Total Assigned** | All studies assigned to you |
| **In Progress** | Studies you're actively working |
| **Awaiting Action** | Studies waiting for client response |
| **Completed This Month** | Studies finished recently |
| **Upcoming Site Visits** | Visits scheduled in the next 7 days |
| **Overdue Items** | Tasks past their expected completion |

### Color Indicators

Cards use colors to highlight status:

| Color | Meaning |
|-------|---------|
| 🟢 Green | On track, healthy |
| 🟡 Yellow | Needs attention soon |
| 🔴 Red | Overdue or critical |
| 🔵 Blue | Informational |

---

## Kanban Board

The Kanban board provides a visual workflow for managing studies.

### Understanding Columns

Each column represents a workflow stage:

| Column | Description | Your Action |
|--------|-------------|-------------|
| **Assigned** | Newly assigned to you | Create proposal |
| **Proposal Sent** | Waiting for client | Track acceptance |
| **Data Collection** | Gathering information | Request/review data |
| **Site Visit Pending** | Ready for inspection | Schedule visit |
| **Site Visit Complete** | Inspection done | Enter data |
| **Report Draft** | Creating report | Generate narrative |
| **QA Review** | Submitted for approval | Await feedback |

### Cards on the Board

Each study appears as a card showing:

- **Community name** - Which property
- **Study type** - Full, update, etc.
- **Days in stage** - Time tracking
- **Priority indicator** - If flagged
- **Next action** - What's needed

### Moving Cards

To advance a study:

1. **Complete the required action** for the current stage
2. The card **automatically moves** when status updates
3. Or **drag and drop** the card (if manual moves are enabled)

### Filtering the Board

Filter the Kanban to focus on specific work:

| Filter | Use Case |
|--------|----------|
| **By Status** | See only certain stages |
| **By Community** | Focus on one property |
| **By Date Range** | Studies created within timeframe |
| **Overdue Only** | Find items needing attention |

---

## Task Management

Beyond the Kanban, individual tasks help track detailed work.

### Creating Tasks

1. Open a study's detail page
2. Click **"Add Task"** or **"+"**
3. Enter task details:
   - **Title** - Brief description
   - **Due Date** - When it should be done
   - **Priority** - Low, Medium, High
   - **Notes** - Additional details

### Task Examples

| Task | Purpose |
|------|---------|
| Follow up on financial info | Client hasn't submitted |
| Confirm site visit access | Verify gate codes |
| Review pool equipment | Specific inspection item |
| Upload remaining photos | Post-visit action |
| Update component costs | Research current pricing |

### Completing Tasks

1. Find the task in the study or task list
2. Click the **checkbox** to mark complete
3. Task moves to completed section
4. Completion is **logged** with timestamp

### Task Notifications

You'll be reminded about:

- Tasks due **today**
- Tasks **overdue**
- Tasks assigned to you by **admin**

---

## Study List View

The traditional list view provides detailed study management.

### Accessing the List

1. Click **"Reserve Studies"** in navigation
2. Or go to **"My Work"** under Specialist Tools
3. Toggle between **Kanban** and **List** views

### List Columns

| Column | Information |
|--------|-------------|
| **Study #** | Unique identifier |
| **Community** | Property name |
| **Status** | Current workflow stage |
| **Assigned Date** | When assigned to you |
| **Last Updated** | Most recent activity |
| **Due Date** | Target completion |
| **Actions** | Quick action buttons |

### Sorting and Filtering

**Sort by**:
- Status (workflow order)
- Date assigned (newest/oldest)
- Community name (A-Z)
- Due date (soonest first)

**Filter by**:
- Status (specific stage)
- Date range
- Overdue only
- Community

### Bulk Actions

Select multiple studies for:

- **Export** to spreadsheet
- **Assign tasks** to multiple
- **Update status** (if allowed)

---

## Workflow Status Deep Dive

Understanding each status helps you know what action to take.

### Request Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **RequestCreated** | Client submitted | (Admin handles) |
| **RequestApproved** | Ready for work | (Admin assigns) |
| **Assigned** | Given to you | Create proposal |

### Proposal Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **ProposalCreated** | Draft exists | Review and finalize |
| **ProposalReviewed** | Admin checked | Address feedback |
| **ProposalApproved** | Ready to send | Send to client |
| **ProposalSent** | Client has it | Wait for response |
| **ProposalAccepted** | Client signed | Request financial info |

### Data Collection Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **FinancialInfoRequested** | Waiting for client | Follow up if delayed |
| **FinancialInfoSubmitted** | Client sent data | Review for accuracy |
| **FinancialInfoReceived** | Data accepted | Schedule site visit |

### Site Visit Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **SiteVisitPending** | Ready to schedule | Create calendar event |
| **SiteVisitScheduled** | Date set | Conduct inspection |
| **SiteVisitCompleted** | Inspection done | Upload photos/data |
| **SiteVisitDataEntered** | Data in system | Proceed to report |

### Report Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **FundingPlanReady** | Ready for analysis | Configure funding |
| **FundingPlanComplete** | Calculations done | Write narrative |
| **NarrativeInProcess** | Writing report | Complete sections |
| **NarrativeComplete** | Draft finished | Review quality |
| **ReportDrafted** | Ready for QA | Submit to admin |

### Completion Phase

| Status | Meaning | Your Action |
|--------|---------|-------------|
| **FinalReviewPending** | Admin reviewing | Await feedback |
| **ReportComplete** | Approved | (Admin publishes) |
| **RequestCompleted** | Delivered | Study archived |

---

## Calendar Integration

Your calendar helps manage site visits and deadlines.

### Viewing the Calendar

1. Click **"Calendar"** in Specialist Tools
2. See all scheduled events
3. Filter by type (site visits, deadlines, reminders)

### Calendar Views

| View | Best For |
|------|----------|
| **Month** | Overview of scheduling |
| **Week** | Detailed planning |
| **Day** | Today's activities |
| **Agenda** | List of upcoming events |

### Event Types

| Event Type | Color/Icon | Description |
|------------|------------|-------------|
| **Site Visit** | 📍 Blue | Property inspection |
| **Deadline** | ⏰ Red | Due date reminder |
| **Meeting** | 👥 Green | Client or team call |
| **Follow-up** | 🔔 Yellow | Reminder to check in |

### Creating Calendar Events

1. Click on a **date** to add an event
2. Or from a study, click **"Schedule Visit"**
3. Fill in details:
   - Event type
   - Date and time
   - Location
   - Notes
   - Invite client (optional)

---

## Quick Actions

Speed up common tasks with quick actions.

### From Dashboard

| Quick Action | What It Does |
|--------------|--------------|
| **View All Studies** | Jump to study list |
| **Today's Tasks** | See what's due |
| **Create Proposal** | Start new proposal |
| **Schedule Visit** | Add calendar event |

### From Study Card

Click the menu on any study card for:

| Action | Description |
|--------|-------------|
| **View Details** | Open full study page |
| **Add Note** | Quick note without opening |
| **Create Task** | Add a task item |
| **Change Status** | Move to next stage |
| **Contact Client** | Send message |

---

## Productivity Tips

### Morning Routine

1. Check **dashboard** for overview
2. Review **overdue items** first
3. Check **today's calendar** events
4. Process **notifications**
5. Plan your **priorities**

### Managing Workload

| Situation | Strategy |
|-----------|----------|
| Too many studies | Talk to admin about prioritization |
| Waiting on clients | Set follow-up reminders |
| Complex study | Break into smaller tasks |
| Approaching deadline | Focus and escalate blockers |

### Time Tracking (if applicable)

If your organization tracks time:

1. Log time against **specific studies**
2. Note **activity type** (travel, inspection, reporting)
3. Submit **regularly** to avoid backlog

---

## Troubleshooting

### Common Issues

**Q: A study is stuck and won't move to the next status.**
A: Check if all required fields are complete. Some statuses require specific data before advancing.

**Q: I can't see a study I know exists.**
A: Check your filters. Try "All Studies" view. The study may be assigned to someone else.

**Q: The calendar isn't showing my events.**
A: Refresh the page. Check date range filters. Ensure events weren't deleted.

**Q: My Kanban board is empty.**
A: You may not have studies in those stages. Check the List view for all studies.

---

## Next Steps

Master these workflow tools, then learn:

- **[Creating Proposals →](/Help/SpecialistGuide/proposals)** - Draft and send service agreements
- **[Site Visits →](/Help/SpecialistGuide/site-visits)** - Conduct effective inspections

---

[← Getting Started](/Help/SpecialistGuide/getting-started) | [Proposals →](/Help/SpecialistGuide/proposals)
