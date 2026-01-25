# Report Generation

This is where everything comes together—creating the comprehensive reserve study report. This guide covers component editing, funding plans, narrative creation, and submitting for QA review.

---

## Overview

The report generation phase transforms your data into a deliverable document.

### Report Components

| Component | Description |
|-----------|-------------|
| **Component Inventory** | List of all assets with costs and life |
| **Condition Assessment** | Current state of each component |
| **Funding Analysis** | Financial projections and scenarios |
| **Narrative** | Written explanations and recommendations |
| **Appendices** | Tables, charts, methodology |

### Workflow

```
Site Visit Complete
      ↓
Edit Components → Configure Funding → Write Narrative
      ↓
Preview Report → Quality Check → Submit for QA
```

---

## Working with Components

### Accessing Components

1. Open the **study**
2. Navigate to **Components** or **Elements** tab
3. View the component list

### Component List View

The list shows all tracked assets:

| Column | Information |
|--------|-------------|
| **Name** | Component description |
| **Category** | Grouping (Roofing, Paving, etc.) |
| **Quantity** | Count or measurement |
| **Useful Life** | Total expected lifespan |
| **Remaining Life** | Years until replacement |
| **Current Cost** | Today's replacement cost |
| **Condition** | Current state rating |

### Editing a Component

Click on a component to edit:

#### Basic Information

| Field | Description |
|-------|-------------|
| **Name** | Descriptive name |
| **Category** | Classification |
| **Location** | Where in property |
| **Quantity** | Amount (each, SF, LF, etc.) |
| **Unit of Measure** | How quantity is measured |

#### Life Cycle

| Field | Description |
|-------|-------------|
| **Useful Life** | Total years before replacement |
| **Remaining Life** | Years left (from site visit) |
| **Install Year** | When originally installed |
| **Last Replaced** | Most recent replacement |
| **Next Replacement** | Calculated year |

> **Tip**: Remaining life is typically set during the site visit based on condition assessment.

#### Cost Information

| Field | Description |
|-------|-------------|
| **Unit Cost** | Cost per unit |
| **Total Cost** | Quantity × Unit Cost |
| **Cost Source** | Where cost came from |
| **Cost Date** | When cost was quoted |

#### Condition

| Field | Description |
|-------|-------------|
| **Condition Rating** | Excellent/Good/Fair/Poor |
| **Condition Notes** | Observations |
| **Photos** | Linked site visit photos |

### Adding Components

To add a new component:

1. Click **"Add Component"** or **"+"**
2. Fill in required fields
3. Set life cycle and costs
4. Add condition information
5. Save

### Additional Components

Components discovered during site visit but not in original scope:

- Add them to the study
- Document why they were added
- May trigger **scope change/amendment** if significant

### Component Categories

Standard categories:

| Category | Examples |
|----------|----------|
| **Roofing** | Shingle, flat, tile, gutters |
| **Exterior** | Siding, paint, stucco, balconies |
| **Paving** | Asphalt, concrete, seal coating |
| **Recreation** | Pool, spa, tennis, playground |
| **Landscaping** | Irrigation, fencing, trees |
| **Mechanical** | HVAC, elevators, pumps |
| **Plumbing** | Water heaters, pipes |
| **Electrical** | Lighting, panels, generators |
| **Structure** | Foundations, retaining walls |

---

## Cost Research

### Finding Current Costs

| Source | How to Use |
|--------|------------|
| **Vendor quotes** | Contact service providers |
| **Industry databases** | RSMeans, Marshall & Swift |
| **Recent invoices** | From client documents |
| **Historical data** | Prior studies with inflation |
| **Company library** | Your organization's cost data |

### Cost Factors

| Factor | Impact |
|--------|--------|
| **Location** | Regional cost differences |
| **Access** | Difficult access = higher cost |
| **Quantity** | Economy of scale |
| **Quality** | Material grade |
| **Market conditions** | Supply/demand |

### Documenting Costs

For each cost, note:

- **Source** - Where it came from
- **Date** - When it was valid
- **Assumptions** - What's included
- **Escalation** - Inflation factor

---

## Funding Plan Configuration

### Understanding Funding Strategies

The system calculates different funding approaches:

| Strategy | Description |
|----------|-------------|
| **Full Funding** | Maintain 100% funded at all times |
| **Threshold Funding** | Stay above a minimum (e.g., 30%) |
| **Baseline Funding** | Current contribution level |
| **Statutory** | Meet state-required minimums |
| **Component** | Fund each item individually |

### Configuring the Funding Plan

1. Go to **Funding Plan** tab
2. Set **parameters**:
   - Start date (fiscal year start)
   - Projection length (typically 30 years)
   - Inflation rate
   - Interest rate on reserves
   - Starting balance (from financial info)
   - Current contribution

### Funding Parameters

| Parameter | Typical Value |
|-----------|---------------|
| **Projection Period** | 30 years |
| **Inflation Rate** | 2-4% |
| **Investment Return** | 1-3% |
| **Contribution Frequency** | Monthly or Annual |
| **Minimum Balance** | $0 or threshold |

### Running Calculations

1. Click **"Calculate"** or **"Run Analysis"**
2. System generates projections
3. Review results for each scenario

### Reviewing Results

For each scenario, view:

| Output | Description |
|--------|-------------|
| **Annual Contribution** | Recommended yearly amount |
| **Per-Unit Cost** | Monthly per door |
| **Ending Balance** | Year-by-year reserves |
| **Percent Funded** | Funding level over time |
| **Cash Flow** | Income vs. expenditures |

### Charts and Graphs

The system generates visualizations:

| Chart | Shows |
|-------|-------|
| **Reserve Balance** | Year-by-year balance trend |
| **Percent Funded** | Funding level over time |
| **Cash Flow** | Contributions vs. expenditures |
| **Component Timeline** | When replacements occur |

---

## Narrative Creation

### What is the Narrative?

The written portion of the report that:

- **Explains** the methodology
- **Describes** the property
- **Summarizes** findings
- **Interprets** the numbers
- **Provides** recommendations

### Narrative Sections

| Section | Content |
|---------|---------|
| **Executive Summary** | High-level overview |
| **Property Description** | Community details |
| **Methodology** | How study was conducted |
| **Component Summary** | Assessment findings |
| **Funding Analysis** | Financial recommendations |
| **Recommendations** | Suggested actions |

### Using Narrative Templates

Templates pre-populate standard content:

1. Go to **Narrative** tab
2. Select a **template** (if not auto-applied)
3. Template **sections load**
4. **Customize** for this study

### Editing Narrative Sections

For each section:

1. Click to **edit**
2. Modify the text
3. Use **tokens** for dynamic data
4. Format with the editor toolbar
5. Save changes

### Tokens/Placeholders

Insert dynamic data using tokens:

| Token | Inserts |
|-------|---------|
| `{{CommunityName}}` | Property name |
| `{{TotalUnits}}` | Number of units |
| `{{ComponentCount}}` | Number of components |
| `{{CurrentBalance}}` | Starting reserve balance |
| `{{RecommendedContribution}}` | Calculated amount |
| `{{SiteVisitDate}}` | When inspection occurred |

> **Example**: "The inspection of {{CommunityName}} was conducted on {{SiteVisitDate}}."

### Writing Tips

| Tip | Why |
|-----|-----|
| Use clear language | Clients aren't experts |
| Avoid jargon | Or define terms used |
| Be specific | Use actual numbers |
| Stay objective | Report facts, not opinions |
| Include recommendations | Provide actionable advice |

---

## Report Preview

### Previewing the Draft

1. Click **"Preview Report"** or **"Generate Preview"**
2. A PDF-style preview opens
3. Review all sections
4. Check for errors or issues

### Preview Checklist

| Check | What to Look For |
|-------|------------------|
| **Cover page** | Correct community name, date |
| **Table of contents** | Sections listed |
| **Executive summary** | Accurate summary |
| **Property info** | Correct details |
| **Component table** | All components included |
| **Calculations** | Numbers look right |
| **Charts** | Legible and accurate |
| **Photos** | Included and clear |
| **Formatting** | Consistent, professional |

### Making Corrections

If you find issues:

1. **Note the section/page**
2. Return to **edit mode**
3. Make corrections
4. **Re-preview** to verify

---

## Quality Checklist

Before submitting, verify:

### Data Accuracy

- [ ] All components from site visit included
- [ ] Remaining life reflects current condition
- [ ] Costs are current and documented
- [ ] Financial data matches client submission
- [ ] Calculations appear reasonable

### Report Completeness

- [ ] All narrative sections written
- [ ] Charts and graphs generated
- [ ] Photos included where appropriate
- [ ] Table of contents accurate
- [ ] Page numbers correct

### Professional Quality

- [ ] No spelling or grammar errors
- [ ] Consistent formatting
- [ ] Clear and readable
- [ ] Company branding applied
- [ ] Contact information included

---

## Submitting for QA Review

### When Ready

After passing your quality check:

1. Click **"Submit for Review"** or **"Submit for QA"**
2. Add any **notes for reviewer**
3. Confirm submission
4. Status changes to **"ReportDrafted"** or **"FinalReviewPending"**

### What Happens Next

1. **Admin notified** of pending review
2. Admin **reviews** your work
3. Admin either **approves** or **requests changes**

### If Changes Requested

1. You receive **notification**
2. View **admin feedback**
3. Make requested **corrections**
4. **Resubmit** for review

### If Approved

1. Status changes to **"ReportComplete"** or **"ApprovedReport"**
2. Admin can **publish** to client
3. Your work on this study is **substantially complete**

---

## Report Generation Tips

### Efficiency

| Tip | Benefit |
|-----|---------|
| Use templates | Faster starting point |
| Copy similar studies | Reuse descriptions |
| Batch similar components | Group editing |
| Use keyboard shortcuts | Speed up data entry |

### Quality

| Tip | Benefit |
|-----|---------|
| Review before submitting | Catch errors early |
| Check calculations | Ensure accuracy |
| Proofread narrative | Professional appearance |
| Get second opinion | Fresh eyes find issues |

### Common Mistakes

| Mistake | How to Avoid |
|---------|--------------|
| Wrong remaining life | Double-check site notes |
| Outdated costs | Verify sources and dates |
| Missing components | Cross-reference site checklist |
| Template text not updated | Search for placeholder text |
| Photos not linked | Review photo attachments |

---

## Troubleshooting

### Common Issues

**Q: Calculations seem wrong.**
A: Check input parameters (inflation, interest, starting balance). Verify component costs.

**Q: PDF won't generate.**
A: Try refreshing. Check for missing required fields. Contact support if persistent.

**Q: Photos aren't appearing in the report.**
A: Ensure photos are linked to components. Check photo upload status.

**Q: Narrative tokens aren't populating.**
A: Verify token syntax. Ensure referenced data exists (e.g., community name is entered).

**Q: I submitted but need to make changes.**
A: Contact your admin to return the study for edits.

---

## Summary

Report generation workflow:

1. ✅ **Edit components** - Finalize all data
2. ✅ **Configure funding** - Run calculations
3. ✅ **Write narrative** - Complete all sections
4. ✅ **Preview report** - Check everything
5. ✅ **Quality check** - Self-review
6. ✅ **Submit for QA** - Send to admin

---

## Next Steps

Congratulations! You've completed the specialist workflow.

- Return to **[Dashboard](./dashboard-and-workflow.md)** for your next assignment
- Reference the **[Admin Guide](../AdminGuide/index.md)** if you're also an admin

---

[← Site Visits](./site-visits.md) | [Back to Specialist Guide](./index.md)
