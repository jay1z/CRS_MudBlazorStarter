# Settings and Configuration

Configure your organization's settings to customize how the system works for your business. This guide covers tenant settings, reserve study defaults, element management, themes, and templates.

---

## Settings Overview

Your organization's settings are organized into categories:

| Category | What It Controls |
|----------|------------------|
| **Tenant Settings** | Company info, branding, contact details |
| **Reserve Study Settings** | Calculation defaults, funding strategies |
| **Element Management** | Component library and defaults |
| **Theme Customization** | Visual appearance |
| **Invoice Settings** | Billing configuration |
| **Narrative Templates** | Report text templates |

---

## Tenant Settings

### Accessing Tenant Settings

1. Go to **Tenant Admin** → **Tenant Settings**
2. Or navigate to `/Admin/Settings`

### Company Information

| Field | Description |
|-------|-------------|
| **Company Name** | Your organization's legal name |
| **Display Name** | Name shown in the portal |
| **Address** | Business address |
| **Phone** | Main contact number |
| **Email** | General contact email |
| **Website** | Your company website |

### Branding

Customize your portal appearance:

| Element | Description |
|---------|-------------|
| **Logo** | Company logo (PNG, JPG) |
| **Favicon** | Browser tab icon |
| **Primary Color** | Main accent color |
| **Secondary Color** | Supporting color |

#### Uploading a Logo

1. Click **"Upload Logo"**
2. Select image file (recommended: 200x60px, PNG with transparency)
3. Preview the result
4. Save changes

### Contact Details

Configure support information shown to clients:

| Field | Description |
|-------|-------------|
| **Support Email** | Where clients send questions |
| **Support Phone** | Phone support number |
| **Hours** | Business hours |

---

## Reserve Study Settings

### Accessing Calculator Defaults

1. Go to **Tenant Admin** → **Calculator Defaults**
2. Or navigate to `/Settings/ReserveStudy`

### Default Parameters

Set defaults that apply to new studies:

| Parameter | Description | Typical Value |
|-----------|-------------|---------------|
| **Projection Period** | Years to forecast | 30 years |
| **Inflation Rate** | Annual cost increase | 2-4% |
| **Investment Return** | Interest on reserves | 1-3% |
| **Fiscal Year Start** | First month of fiscal year | January or July |

### Funding Strategies

Configure available funding approaches:

| Strategy | Description |
|----------|-------------|
| **Full Funding** | Maintain 100% funded |
| **Threshold Funding** | Stay above minimum % |
| **Baseline Funding** | Current contribution level |
| **Statutory** | Meet state requirements |
| **Component** | Fund each item separately |

### Enabling/Disabling Strategies

1. Toggle strategies **on/off**
2. Set **default strategy** for new studies
3. Configure **threshold percentage** if using threshold funding

### Calculation Options

| Option | Description |
|--------|-------------|
| **Rounding** | Round to nearest dollar, hundred, etc. |
| **Contribution Frequency** | Monthly or annual |
| **Include Interest** | Calculate investment returns |
| **Inflation Model** | How to apply inflation |

---

## Element/Component Management

### Accessing Element Management

1. Go to **Settings** → **Element Management**
2. Or navigate to `/Settings/ElementManagement`

### Understanding Elements

Elements are the **default component templates** used when creating studies:

| Concept | Description |
|---------|-------------|
| **Element Library** | Master list of component types |
| **Default Values** | Pre-set useful life, costs |
| **Categories** | Groupings (Roofing, Paving, etc.) |

### Element List

View all elements in your library:

| Column | Information |
|--------|-------------|
| **Name** | Component description |
| **Category** | Classification |
| **Useful Life** | Default lifespan |
| **Unit** | Measurement (each, SF, LF) |
| **Actions** | Edit, Delete |

### Adding Elements

1. Click **"Add Element"**
2. Fill in details:

| Field | Description |
|-------|-------------|
| **Name** | Descriptive name |
| **Category** | Select or create category |
| **Useful Life** | Expected lifespan in years |
| **Unit of Measure** | Each, SF, LF, etc. |
| **Default Cost** | Typical replacement cost |
| **Notes** | Additional info |

3. Click **"Save"**

### Editing Elements

1. Find the element in the list
2. Click **"Edit"**
3. Modify values
4. Save changes

> **Note**: Changes affect **new studies only**, not existing ones.

### Deleting Elements

1. Click **"Delete"** on the element
2. Confirm deletion
3. Element removed from library

> **Warning**: Deleting doesn't affect existing studies using this element.

### Element Categories

Organize elements into categories:

| Category | Examples |
|----------|----------|
| **Roofing** | Shingles, Flat Roof, Gutters |
| **Exterior** | Siding, Paint, Balconies |
| **Paving** | Asphalt, Concrete, Seal Coat |
| **Recreation** | Pool, Spa, Tennis Court |
| **Landscape** | Irrigation, Fencing, Trees |
| **Mechanical** | HVAC, Elevator, Pumps |

### Managing Categories

1. Go to **Categories** tab
2. Add, edit, or reorder categories
3. Assign elements to categories

### Element Order

Control the order elements appear in studies:

1. Drag and drop elements
2. Or set **sort order** numbers
3. Elements display in this order during study creation

---

## Theme Customization

### Accessing Themes

1. Go to **Settings** → **Themes**
2. Or navigate to `/Settings/Themes`

### Preset Themes

Choose from built-in themes:

| Theme | Description |
|-------|-------------|
| **Default** | Standard professional look |
| **Litera** | Light, clean design |
| **BlackAndWhite** | High contrast |
| **Sunset Serenade** | Warm colors |
| **Space Walk** | Dark theme |

### Applying a Theme

1. Preview themes
2. Click **"Apply"** on desired theme
3. Theme changes immediately
4. Visible to all users

### Custom Colors

If supported, customize colors:

| Element | What It Affects |
|---------|-----------------|
| **Primary** | Main buttons, links |
| **Secondary** | Supporting elements |
| **Background** | Page backgrounds |
| **Surface** | Cards and panels |
| **Text** | Font colors |

---

## Homepage Customization

### Public Homepage

Customize your public-facing landing page:

1. Go to **Settings** → **Homepage** or **Tenant Admin** → **Homepage**
2. Edit content blocks

### Content Blocks

| Block | Content |
|-------|---------|
| **Hero** | Main banner with headline |
| **About** | Company description |
| **Services** | What you offer |
| **Contact** | How to reach you |
| **Testimonials** | Client quotes |

### Editing Content

1. Click on a **content block**
2. Edit text using the **rich text editor**
3. Upload images as needed
4. Save changes

### Preview and Publish

1. Click **"Preview"** to see changes
2. Review on desktop and mobile
3. Click **"Publish"** to go live

---

## Narrative Templates

### Understanding Templates

Narrative templates pre-populate report text:

| Benefit | Description |
|---------|-------------|
| **Consistency** | Same quality every report |
| **Efficiency** | Less writing per study |
| **Accuracy** | Tested language |
| **Branding** | Your company voice |

### Template Structure

Templates contain:

| Component | Description |
|-----------|-------------|
| **Sections** | Major report divisions |
| **Blocks** | Paragraphs within sections |
| **Tokens** | Dynamic data placeholders |

### Managing Templates

1. Go to **Settings** → **Narrative Templates**
2. View existing templates
3. Create, edit, or delete templates

### Creating a Template

1. Click **"New Template"**
2. Name the template
3. Add **sections** (Executive Summary, Methodology, etc.)
4. Add **blocks** within sections
5. Use **tokens** for dynamic content
6. Save template

### Using Tokens

Tokens insert study-specific data:

| Token | Inserts |
|-------|---------|
| `{{CommunityName}}` | Property name |
| `{{TotalUnits}}` | Number of units |
| `{{SiteVisitDate}}` | Inspection date |
| `{{RecommendedContribution}}` | Calculated amount |
| `{{PercentFunded}}` | Current funding level |
| `{{ComponentCount}}` | Number of components |

### Template Example

```
The {{CommunityName}} community was inspected on {{SiteVisitDate}}. 
This {{TotalUnits}}-unit property includes {{ComponentCount}} major 
components with a combined replacement value of {{TotalReplacementCost}}.

Based on our analysis, we recommend an annual contribution of 
{{RecommendedContribution}} to maintain adequate reserves.
```

---

## Workflow Settings

### Configuring Workflow Behavior

| Setting | Options | Description |
|---------|---------|-------------|
| **Require Proposal Review** | Yes/No | Admin must approve before sending |
| **Auto-Request Financial Info** | Yes/No | Send request on proposal acceptance |
| **Amendment Threshold** | Percentage | Variance that triggers amendment |
| **Require Final Review** | Yes/No | Admin review before completion |

### Amendment Settings

Configure scope change handling:

| Setting | Description |
|---------|-------------|
| **Threshold %** | Variance % that triggers amendment |
| **Auto-Create** | Automatically create amendment document |
| **Require Approval** | Admin must approve before sending |

---

## Email Settings

### Email Templates

Customize automated emails:

| Email | When Sent |
|-------|-----------|
| **Welcome** | New user registration |
| **Proposal Ready** | Proposal available for review |
| **Financial Info Request** | Request for client data |
| **Site Visit Scheduled** | Visit confirmation |
| **Report Complete** | Final report ready |
| **Invoice Sent** | New invoice notification |

### Editing Email Templates

1. Go to **Settings** → **Email Templates**
2. Select template to edit
3. Modify subject and body
4. Use tokens for personalization
5. Preview and save

---

## Best Practices

### Initial Setup

When first configuring:

1. Complete **company information**
2. Upload **logo** and set **colors**
3. Set **reserve study defaults**
4. Build **element library**
5. Configure **narrative template**
6. Set up **invoice settings**

### Ongoing Maintenance

| Task | Frequency |
|------|-----------|
| Review element costs | Quarterly |
| Update narrative templates | As needed |
| Check email templates | Annually |
| Review workflow settings | As business changes |

### Testing Changes

Before committing to changes:

1. **Preview** where possible
2. **Test** with a sample study
3. **Document** what changed
4. **Train** staff on updates

---

## Troubleshooting

### Common Issues

**Q: Logo isn't displaying correctly.**
A: Check file format (PNG/JPG) and dimensions. Clear browser cache.

**Q: Element changes aren't showing in studies.**
A: Changes only affect new studies. Existing studies keep their values.

**Q: Template tokens aren't populating.**
A: Verify token syntax ({{ }}). Ensure data exists in the study.

**Q: Theme isn't applying.**
A: Hard refresh the page (Ctrl+F5). Clear browser cache.

---

## Next Steps

Configure your financial settings:

- **[Financial Management →](./financial-management.md)** - Invoicing and payments

---

[← Workflow Management](./workflow-management.md) | [Financial Management →](./financial-management.md)
