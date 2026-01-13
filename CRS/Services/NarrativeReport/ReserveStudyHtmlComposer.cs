using System.Text;

using CRS.Data;
using CRS.Models.NarrativeTemplates;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Composes complete HTML documents from narrative templates.
/// Handles tenant-specific template resolution, placeholder replacement, and document assembly.
/// </summary>
public class ReserveStudyHtmlComposer : IReserveStudyHtmlComposer
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IPlaceholderResolver _placeholderResolver;
    private readonly INarrativeHtmlSanitizer _htmlSanitizer;
    private readonly ILogger<ReserveStudyHtmlComposer> _logger;

    public ReserveStudyHtmlComposer(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IPlaceholderResolver placeholderResolver,
        INarrativeHtmlSanitizer htmlSanitizer,
        ILogger<ReserveStudyHtmlComposer> logger)
    {
        _dbFactory = dbFactory;
        _placeholderResolver = placeholderResolver;
        _htmlSanitizer = htmlSanitizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ComposeAsync(
        ReserveStudyReportContext context,
        int? tenantId = null,
        NarrativeCompositionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new NarrativeCompositionOptions();

        // Load sections and blocks
        var sections = await LoadSectionsAsync(tenantId, cancellationToken);
        var blocks = await LoadBlocksAsync(tenantId, cancellationToken);
        var inserts = await LoadInsertsAsync(tenantId, cancellationToken);

        var bodyContent = new StringBuilder();

        foreach (var section in sections.OrderBy(s => s.SortOrder))
        {
            // Check if section should be excluded
            if (options.ExcludedSections?.Contains(section.SectionKey) == true)
                continue;

            // Check if only certain sections should be included
            if (options.OnlyIncludedSections && options.IncludedSections?.Contains(section.SectionKey) != true)
                continue;

            if (!section.IsEnabled)
                continue;

            // Add page break before major sections
            if (section.PageBreakBefore && options.IncludePageBreaks)
            {
                bodyContent.AppendLine("<div class=\"page-break\"></div>");
            }

            // Start section wrapper
            var sectionClass = $"section section-{section.SectionKey.ToLowerInvariant()}";
            if (!string.IsNullOrEmpty(section.CssClass))
                sectionClass += $" {section.CssClass}";

            bodyContent.AppendLine($"<section class=\"{sectionClass}\" data-section=\"{section.SectionKey}\">");

            // Render blocks for this section
            var sectionBlocks = blocks
                .Where(b => b.SectionKey == section.SectionKey && b.IsEnabled)
                .OrderBy(b => b.SortOrder);

            foreach (var block in sectionBlocks)
            {
                var blockHtml = RenderBlock(block, context);
                bodyContent.AppendLine(blockHtml);

                // Check for inserts after this block
                var applicableInserts = inserts
                    .Where(i => i.IsEnabled &&
                               i.TargetSectionKey == section.SectionKey &&
                               i.InsertAfterBlockKey == block.BlockKey)
                    .OrderBy(i => i.SortOrder);

                foreach (var insert in applicableInserts)
                {
                    var insertHtml = RenderInsert(insert, context);
                    bodyContent.AppendLine(insertHtml);
                }
            }

            bodyContent.AppendLine("</section>");
        }

        // Wrap in document structure if requested
        if (options.IncludeDocumentWrapper)
        {
            return WrapInDocument(bodyContent.ToString(), context, options);
        }

        return bodyContent.ToString();
    }

    /// <inheritdoc />
    public async Task<string> ComposeSectionAsync(
        string sectionKey,
        ReserveStudyReportContext context,
        int? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var sections = await LoadSectionsAsync(tenantId, cancellationToken);
        var blocks = await LoadBlocksAsync(tenantId, cancellationToken);

        var section = sections.FirstOrDefault(s => s.SectionKey == sectionKey);
        if (section == null || !section.IsEnabled)
            return string.Empty;

        var sb = new StringBuilder();
        var sectionBlocks = blocks
            .Where(b => b.SectionKey == sectionKey && b.IsEnabled)
            .OrderBy(b => b.SortOrder);

        foreach (var block in sectionBlocks)
        {
            var blockHtml = RenderBlock(block, context);
            sb.AppendLine(blockHtml);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string GetPrintCss()
    {
        return PrintCss;
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════

    private async Task<List<NarrativeTemplateSection>> LoadSectionsAsync(int? tenantId, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Load global defaults (TenantId = 0 or null represented as 0 in ITenantScoped)
        var globalSections = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == 0 && s.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return globalSections;

        // Load tenant-specific sections
        var tenantSections = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == tenantId.Value && s.DateDeleted == null)
            .ToListAsync(ct);

        // Merge: tenant overrides global
        var result = new Dictionary<string, NarrativeTemplateSection>();
        foreach (var section in globalSections)
        {
            result[section.SectionKey] = section;
        }
        foreach (var section in tenantSections)
        {
            result[section.SectionKey] = section; // Override
        }

        return result.Values.ToList();
    }

    private async Task<List<NarrativeTemplateBlock>> LoadBlocksAsync(int? tenantId, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Load global defaults
        var globalBlocks = await context.NarrativeTemplateBlocks
            .Where(b => b.TenantId == 0 && b.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return globalBlocks;

        // Load tenant-specific blocks
        var tenantBlocks = await context.NarrativeTemplateBlocks
            .Where(b => b.TenantId == tenantId.Value && b.DateDeleted == null)
            .ToListAsync(ct);

        // Merge: tenant overrides global (keyed by SectionKey + BlockKey)
        var result = new Dictionary<string, NarrativeTemplateBlock>();
        foreach (var block in globalBlocks)
        {
            var key = $"{block.SectionKey}:{block.BlockKey}";
            result[key] = block;
        }
        foreach (var block in tenantBlocks)
        {
            var key = $"{block.SectionKey}:{block.BlockKey}";
            result[key] = block; // Override
        }

        return result.Values.ToList();
    }

    private async Task<List<NarrativeInsert>> LoadInsertsAsync(int? tenantId, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Load global defaults
        var globalInserts = await context.NarrativeInserts
            .Where(i => i.TenantId == 0 && i.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return globalInserts;

        // Load tenant-specific inserts
        var tenantInserts = await context.NarrativeInserts
            .Where(i => i.TenantId == tenantId.Value && i.DateDeleted == null)
            .ToListAsync(ct);

        // Merge: tenant overrides global
        var result = new Dictionary<string, NarrativeInsert>();
        foreach (var insert in globalInserts)
        {
            result[insert.InsertKey] = insert;
        }
        foreach (var insert in tenantInserts)
        {
            result[insert.InsertKey] = insert; // Override
        }

        return result.Values.ToList();
    }

    private string RenderBlock(NarrativeTemplateBlock block, ReserveStudyReportContext context)
    {
        try
        {
            // Sanitize the template HTML
            var sanitizedHtml = _htmlSanitizer.Sanitize(block.HtmlTemplate);

            // Replace placeholders
            var processedHtml = _placeholderResolver.ReplacePlaceholders(sanitizedHtml, context);

            // Replace tokens
            processedHtml = _placeholderResolver.ReplaceTokens(processedHtml, context);

            // Wrap in block container
            var blockClass = $"block block-{block.BlockKey.ToLowerInvariant()}";
            if (!string.IsNullOrEmpty(block.CssClass))
                blockClass += $" {block.CssClass}";

            return $"<div class=\"{blockClass}\" data-block=\"{block.BlockKey}\">{processedHtml}</div>";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering block {BlockKey}", block.BlockKey);
            return $"<!-- Error rendering block: {block.BlockKey} -->";
        }
    }

    private string RenderInsert(NarrativeInsert insert, ReserveStudyReportContext context)
    {
        try
        {
            // Sanitize the template HTML
            var sanitizedHtml = _htmlSanitizer.Sanitize(insert.HtmlTemplate);

            // Replace placeholders
            var processedHtml = _placeholderResolver.ReplacePlaceholders(sanitizedHtml, context);

            // Replace tokens
            processedHtml = _placeholderResolver.ReplaceTokens(processedHtml, context);

            return $"<div class=\"insert insert-{insert.InsertKey.ToLowerInvariant()}\">{processedHtml}</div>";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering insert {InsertKey}", insert.InsertKey);
            return $"<!-- Error rendering insert: {insert.InsertKey} -->";
        }
    }

    private string WrapInDocument(string bodyContent, ReserveStudyReportContext context, NarrativeCompositionOptions options)
    {
        var title = context.Study.ReportTitle ?? $"Reserve Study - {context.Association.Name}";
        var cssContent = options.IncludePrintCss ? PrintCss : string.Empty;
        if (!string.IsNullOrEmpty(options.CustomCss))
        {
            cssContent += "\n" + options.CustomCss;
        }

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{System.Web.HttpUtility.HtmlEncode(title)}</title>
    <style>
{cssContent}
    </style>
</head>
<body>
    <div class=""report-container"">
{bodyContent}
    </div>
</body>
</html>";
    }

    // ═══════════════════════════════════════════════════════════════
    // PRINT CSS
    // ═══════════════════════════════════════════════════════════════

    private const string PrintCss = @"
/* Reserve Study Narrative - Print-Friendly CSS */

/* Base Styles */
:root {
    --color-primary: #1a365d;
    --color-secondary: #2d3748;
    --color-accent: #3182ce;
    --color-text: #1a202c;
    --color-text-light: #4a5568;
    --color-border: #e2e8f0;
    --color-background: #ffffff;
    --font-family: 'Georgia', 'Times New Roman', serif;
    --font-family-heading: 'Arial', 'Helvetica', sans-serif;
}

* {
    box-sizing: border-box;
}

body {
    font-family: var(--font-family);
    font-size: 11pt;
    line-height: 1.5;
    color: var(--color-text);
    background: var(--color-background);
    margin: 0;
    padding: 0;
}

.report-container {
    max-width: 8.5in;
    margin: 0 auto;
    padding: 0.5in;
}

/* Headings */
h1, h2, h3, h4, h5, h6 {
    font-family: var(--font-family-heading);
    color: var(--color-primary);
    margin-top: 1em;
    margin-bottom: 0.5em;
    page-break-after: avoid;
}

h1 { font-size: 24pt; }
h2 { font-size: 18pt; border-bottom: 2px solid var(--color-primary); padding-bottom: 0.25em; }
h3 { font-size: 14pt; }
h4 { font-size: 12pt; }

/* Paragraphs */
p {
    margin: 0 0 1em 0;
    text-align: justify;
}

/* Lists */
ul, ol {
    margin: 0 0 1em 1.5em;
    padding: 0;
}

li {
    margin-bottom: 0.5em;
}

/* Tables */
table {
    width: 100%;
    border-collapse: collapse;
    margin: 1em 0;
    page-break-inside: avoid;
}

th, td {
    border: 1px solid var(--color-border);
    padding: 0.5em;
    text-align: left;
}

th {
    background-color: var(--color-primary);
    color: white;
    font-weight: bold;
}

tbody tr:nth-child(even) {
    background-color: #f8fafc;
}

/* Specific Table Types */
.contribution-schedule th,
.allocation-table th,
.info-furnished th {
    background-color: var(--color-primary);
}

.contribution-schedule td:nth-child(2),
.contribution-schedule td:nth-child(3),
.allocation-table td:nth-child(2) {
    text-align: right;
}

/* Sections */
.section {
    margin-bottom: 2em;
}

.section-cover {
    text-align: center;
    padding: 2in 0.5in;
}

.section-cover h1 {
    font-size: 28pt;
    margin-bottom: 0.25em;
}

/* Blocks */
.block {
    margin-bottom: 1em;
}

/* Page Breaks */
.page-break {
    page-break-after: always;
    break-after: page;
    height: 0;
    display: block;
}

/* Signatures */
.signature-container {
    display: flex;
    flex-wrap: wrap;
    gap: 2em;
    margin: 2em 0;
}

.signature-block {
    flex: 1 1 200px;
    text-align: center;
    min-width: 200px;
}

.signature-line {
    border-bottom: 1px solid var(--color-text);
    width: 100%;
    height: 50px;
    margin-bottom: 0.5em;
}

.signature-image {
    max-height: 60px;
    max-width: 200px;
    margin-bottom: 0.5em;
}

.signature-name {
    font-weight: bold;
    margin: 0.25em 0;
}

.signature-title,
.signature-credentials,
.signature-date {
    font-size: 10pt;
    color: var(--color-text-light);
    margin: 0.125em 0;
}

/* Photo Gallery */
.photo-gallery {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 1em;
    margin: 1em 0;
}

.photo-item {
    break-inside: avoid;
    page-break-inside: avoid;
}

.photo-image {
    width: 100%;
    max-height: 3in;
    object-fit: contain;
    border: 1px solid var(--color-border);
}

.photo-caption {
    font-size: 10pt;
    font-style: italic;
    text-align: center;
    margin: 0.25em 0;
}

.photo-category,
.photo-condition {
    font-size: 9pt;
    color: var(--color-text-light);
    text-align: center;
    margin: 0;
}

/* Glossary */
.glossary dl {
    margin: 0;
}

.glossary dt {
    font-weight: bold;
    margin-top: 0.5em;
}

.glossary dd {
    margin-left: 1.5em;
    margin-bottom: 0.5em;
}

/* Vendor List */
.vendor-table {
    font-size: 10pt;
}

/* Print-specific styles */
@media print {
    body {
        font-size: 10pt;
    }
    
    .report-container {
        max-width: none;
        padding: 0;
    }
    
    .page-break {
        page-break-after: always;
    }
    
    h1, h2, h3, h4, h5, h6 {
        page-break-after: avoid;
    }
    
    table, figure, .photo-item, .signature-block {
        page-break-inside: avoid;
    }
    
    a {
        text-decoration: none;
        color: inherit;
    }
    
    a[href]:after {
        content: none;
    }
}

/* Header/Footer for PDF (used by PDF engines) */
@page {
    size: letter;
    margin: 0.75in 0.75in 1in 0.75in;
    
    @bottom-center {
        content: counter(page);
        font-size: 9pt;
    }
}

@page :first {
    @bottom-center {
        content: none;
    }
}
";
}
