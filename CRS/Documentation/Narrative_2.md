Now implement the token injection builders and integrate them into the report composition pipeline.

CONTEXT
- Templates are HTML stored in DB.
- We already do placeholder replacement via IPlaceholderResolver and sanitize HTML.
- Now we need to replace special tokens with generated HTML snippets built from ReserveStudyReportContext data.

TOKENS TO SUPPORT
1) [[PAGE_BREAK]] -> `<div class="page-break"></div>`
2) [[TABLE:ContributionSchedule]] -> HTML table showing annual recommended contributions
3) [[TABLE:InfoFurnished]] -> HTML table showing “Information Furnished by Management” summary for year one
4) [[SIGNATURES]] -> signature block HTML (list of signatories)
5) [[PHOTOS]] -> photo gallery HTML (photos with captions)
6) [[VENDORS]] -> vendor list HTML (table or list)
7) [[GLOSSARY]] -> glossary HTML (table of terms/definitions)
(Also support tokens that map to optional charts/large tables via placeholders already: {Chart_PieAllocation}, etc. Don’t implement chart rendering here.)

TASKS

A) Create an interface and implementation
- ITokenRenderer
  - string RenderToken(string tokenName, ReserveStudyReportContext ctx, TokenRenderOptions opt)
- TokenRenderOptions can include:
  - bool IncludeCurrencySymbol, string CultureName, int MoneyDecimals, int PercentDecimals
  - int ContributionScheduleYearsToShow (default 6), bool CondenseContributionTable (true)
  - bool AllowImages (true)

Implement DefaultTokenRenderer that supports the tokens listed above.

B) Table builder: Contribution Schedule
Input: ctx.CalculatedOutputs.ContributionSchedule (List<YearContribution { int Year; decimal Amount; }>)
Output: HTML table (class "table") with columns:
- Year
- Recommended Contribution (formatted money)
Rules:
- If CondenseContributionTable == true, show first N years (default 6), then show every 5 years after, plus the final year (e.g., 30th), matching a typical reserve study summary table.
- If schedule has fewer than N years, show all.
- Format money with ctx/culture options. Create a helper Money().

Example HTML:
```html
<table class="table">
  <thead><tr><th>Year</th><th>Recommended Reserve Contribution</th></tr></thead>
  <tbody>...</tbody>
</table>
```

C) Table builder: Information Furnished by Management
Input: ctx.CalculatedOutputs.FirstYearSummary (StartingBalance, Contribution, Interest, Expenditures, EndingBalance)
Output: a two-column key/value table (class "table") with rows:
- Starting Reserve Cash Balance (as of {StartingReserveBalanceAsOfDate})
- Budgeted Reserve Contributions
- Anticipated Interest Earned
- Less: Anticipated Expenditures
- Anticipated Year-End Reserve Cash Balance
Format all money values. Expenditures should appear as a positive money amount but label indicates subtraction.

D) Signatures builder
Input: ctx.Signatories (List<Signatory { Name, Title, Credentials, LicenseNumber?, SignatureImageUrl? }>)
Output:
- Use a simple layout:
  - Name (bold)
  - Title / credentials line
  - optional license number
  - optional signature image `<img>` if provided and AllowImages == true
Return a `<div>` wrapping each signatory with spacing. Avoid complex CSS.

E) Photos builder
Input: ctx.PhotoItems (List<PhotoItem { ImageUrl, Caption, ComponentName?, Location? }>)
Output:
- A simple grid:
  - Each photo: `<div class="photo-item">`<img ... />`<p class="small">`Caption`</p></div>`
- Add minimal inline structure that works in PDF engines:
  - Use a table layout rather than flex if necessary for compatibility.
Implement as:
```html
<table style="width:100%; border-collapse:collapse;">
  <tr> two columns per row ... </tr>
</table>
```
Ensure images have max-width:100%.

F) Vendors builder
Input: ctx.Vendors (List<Vendor { Name, ServiceType?, Phone?, Email?, Website?, Notes? }>)
Output:
- Prefer a table with columns: Vendor, Service, Contact
- Contact column concatenates phone/email/website if present.

G) Glossary builder
Input: ctx.GlossaryTerms (List<GlossaryTerm { Term, Definition }>)
Output:
- Table with Term (20-30% width) and Definition (rest)
- Escape/encode text appropriately (but you can assume HTML is safe after sanitizer; still HTML-encode term/definition strings to prevent injection).

H) Integrate into composition pipeline
In IReserveStudyHtmlComposer.ComposeAsync:
- After sanitizing + placeholder replacement, apply token replacement:
  - Replace all known tokens by calling ITokenRenderer.
  - Replace unknown tokens with empty string or leave them (choose empty string).
- Token replacement order:
  1) Replace [[PAGE_BREAK]]
  2) Replace token tables/signatures/photos/vendors/glossary

DELIVERABLES
- Provide the code for ITokenRenderer + DefaultTokenRenderer
- Provide helpers: HtmlEncode, Money, Percent, Date formatting
- Provide updates to HtmlComposer where tokens are applied
- Provide unit tests (xUnit) for token rendering
- Provide a small example context and show the output HTML snippets for each token in comments or tests.

