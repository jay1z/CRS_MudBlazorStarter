Here is my new prompt for you. you can modify what you've created already if you need to. make me aware of any issues you foresee. I want to store and render the narrative as HTML (NOT markdown). The output must be close to a typical reserve study narrative structure: Cover, Exhibits Intro, Executive Summary, Funding Summary (fund status + recommended contribution schedule table), Signature page, Narrative Report sections (Evaluation, Assumptions—Inflation/Interest, Operating vs Reserves guidance, Sources, Reserve Components + 4-part test, Chart explanation, Conclusion, Information Furnished by Management table, Definitions), then optional exhibits (Photos, Tables/Spreadsheets, Charts, Disclosures/Laws/Credentials/Vendors).

GOALS
1) Use HTML templates stored in the database as the source of truth.
2) Replace placeholders like {AssociationName}, {CityState}, {InspectionMonthYear}, {InflationRate}, {StartingReserveBalance}, etc.
3) Support per-tenant overrides with global defaults and per-study rendering.
4) Support optional section enable/disable and optional “insert” paragraphs (feature flagged).
5) Generate final HTML for the whole report + convert to PDF (engine-agnostic, adapter interface).
6) Keep it easy to add/modify sections later.

TECH CHOICES / CONSTRAINTS
- HTML templates may be authored by tenant admins: sanitize HTML (no scripts, no iframes, safe tags only).
- Provide a CSS strategy suitable for PDF (print-friendly). Include page-break helpers.

IMPLEMENTATION TASKS

A) DATA CONTRACTS
Create DTOs:
- ReserveStudyReportContext
  - AssociationInfo (Name, Address, City, State, Zip, CommunityType, UnitCount, EstablishedYear)
  - StudyInfo (StudyType, ReportTitle, InspectionDate, EffectiveDate, FiscalYearStart, FiscalYearEnd)
  - FinancialAssumptions (InflationRate, InterestRate, TaxTreatment, MinimumComponentThreshold, OperatingBudgetTotal)
  - CalculatedOutputs
      - FundStatusLabel, FundingMethod
      - ContributionSchedule (list of YearContribution: Year, Amount)
      - FirstYearSummary (StartingBalance, Contribution, Interest, Expenditures, EndingBalance)
      - PerUnitMonthlyIncrease (optional)
      - ReservePercentOfBudget (optional)
      - AllocationByCategory (optional list)
  - Branding (CompanyName, Phone, Email, Website, Address, LogoUrl, CoverImageUrl, FooterText)
  - Collections: Signatories, Vendors, GlossaryTerms, PhotoItems
Include computed helpers (CityState, InspectionMonthYear, FiscalYearLabel).

B) TEMPLATE STORAGE MODEL (EF CORE ENTITIES)
Create entities:
- NarrativeTemplateSection
  - Id, TenantId (nullable for global), SectionKey, Title, IsEnabled, SortOrder
- NarrativeTemplateBlock
  - Id, TenantId (nullable for global), SectionKey, BlockKey, Title, HtmlTemplate, IsEnabled, SortOrder, AppliesWhenJson (optional)
- NarrativeInsert
  - Id, TenantId (nullable), InsertKey, HtmlTemplate, IsEnabled, AppliesWhenJson

Seed global defaults for each section with minimal HTML templates close to a typical reserve study narrative (headings, paragraphs, bullet lists). Use placeholders in braces:

Examples:
```html
<h1>{AssociationName}</h1>

<p>This {StudyType} Reserve Study was prepared for {AssociationName} located in {CityState}...</p>
```

C) PLACEHOLDER REPLACEMENT + FORMATTING
Implement:
- IPlaceholderResolver with Resolve(context) => Dictionary<string,string>
Include formatting helpers:
- Money formatting (e.g., StartingReserveBalance -> $3,116.21)
- Percent formatting (InflationRate 0.03 -> 3.0%)
- Date formatting (InspectionMonthYear)
Support optional placeholders gracefully (empty string if missing).
Replace placeholders in HTML templates safely (do not break HTML).
Add support for special tokens:
- [[PAGE_BREAK]] -> 
```html 
<div class="page-break"></div>
``` 
- [[TABLE:ContributionSchedule]] -> injected HTML table
- [[TABLE:InfoFurnished]] -> injected HTML table
- [[SIGNATURES]] -> injected signature block HTML
- [[PHOTOS]] -> injected photo gallery HTML
- [[VENDORS]] and [[GLOSSARY]] optional.

D) HTML SANITIZATION
Implement IHtmlSanitizer:
- Remove 
```html 
<script>
```
```html
<iframe>
```
, inline event handlers (onclick= etc.)
- Restrict allowed tags and attributes (p, div, span, h1-h4, ul/ol/li, table, thead/tbody/tr/th/td, img, strong, em, br, hr)
- Allow img src only from trusted sources or data URIs (make configurable)
- Allow class attribute for styling
Return sanitized HTML.

E) REPORT ASSEMBLY PIPELINE
Implement:
- IReserveStudyHtmlComposer.ComposeAsync(context, tenantId, options) => string
Steps:
1) Load enabled sections/blocks for tenant with fallback to global (tenant override wins).
2) For each section in SortOrder:
   - For each block: sanitize HTML template, replace placeholders and tokens, append.
3) Wrap with a base document layout:
```html
   <html><head> include print CSS, fonts safe for PDF, simple layout </head><body> ... </body></html>
```
4) Add header/footer placeholders if needed (or CSS for footer text).
5) Ensure page breaks for major sections (cover, exec summary, narrative divider, exhibits).

F) PDF CONVERSION
Create:
- i'd like to test the output as html and then render to pdf when ready

G) ADMIN EDITING SUPPORT (OPTIONAL BUT HELPFUL SCAFFOLD)
Create endpoints/services to:
- Get templates for tenant + global fallback
- Save tenant override template blocks
- Toggle enable/disable per section/block/insert
(Keep minimal; no full UI, just service + sample controller or minimal API endpoints.)

H) TESTS
Add unit tests:
- Placeholder replacement: {AssociationName} and formatted money/percent/dates
- Token replacement: [[PAGE_BREAK]], [[TABLE:ContributionSchedule]]
- Sanitizer removes scripts and onclick
- Section ordering and enable/disable logic
- ComposeAsync returns a full HTML document containing expected headings

DELIVERABLES
- Provide the full class scaffolding, interfaces, EF entities, and a basic seed dataset for templates.
- Provide a minimal example usage:
  - Build ReserveStudyReportContext with sample values
  - Compose HTML preview
  - Call PDF service and confirm bytes returned

Keep the code clean, organized, and easy to extend.
