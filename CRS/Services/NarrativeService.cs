using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for managing narrative content for reserve studies.
/// </summary>
public class NarrativeService : INarrativeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NarrativeService> _logger;

    public NarrativeService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<NarrativeService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // CRUD OPERATIONS
    // ═══════════════════════════════════════════════════════════════

    public async Task<Narrative?> GetByStudyIdAsync(Guid studyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.Narratives
            .Include(n => n.Author)
            .Include(n => n.CompletedBy)
            .Include(n => n.ReviewedBy)
            .Where(n => n.TenantId == _tenantContext.TenantId.Value &&
                       n.ReserveStudyId == studyId &&
                       n.DateDeleted == null &&
                       n.Status != NarrativeStatus.Superseded &&
                       n.Status != NarrativeStatus.Archived)
            .OrderByDescending(n => n.DateCreated)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Narrative?> GetByIdAsync(Guid narrativeId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.Narratives
            .Include(n => n.Author)
            .Include(n => n.CompletedBy)
            .Include(n => n.ReviewedBy)
            .Include(n => n.ReserveStudy)
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);
    }

    public async Task<IEnumerable<Narrative>> GetAllByStudyIdAsync(Guid studyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.Narratives
            .Include(n => n.Author)
            .Where(n => n.TenantId == _tenantContext.TenantId.Value &&
                       n.ReserveStudyId == studyId &&
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<Narrative> CreateAsync(Guid studyId, Guid authorUserId, string? templateName = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var narrative = new Narrative
        {
            TenantId = _tenantContext.TenantId.Value,
            ReserveStudyId = studyId,
            AuthorUserId = authorUserId,
            Status = NarrativeStatus.Draft,
            StartedAt = DateTime.UtcNow,
            Version = "1.0",
            TemplateUsed = templateName
        };

        // Apply template if specified
        if (!string.IsNullOrEmpty(templateName))
        {
            var template = GetAvailableTemplates().FirstOrDefault(t => t.Name == templateName);
            if (template != null)
            {
                ApplyTemplateToNarrative(narrative, template);
            }
        }

        context.Narratives.Add(narrative);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Created narrative {NarrativeId} for study {StudyId}", narrative.Id, studyId);

        return narrative;
    }

    public async Task<bool> UpdateAsync(Narrative narrative, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrative.Id && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (existing == null) return false;

        // Update content sections
        existing.ExecutiveSummary = narrative.ExecutiveSummary;
        existing.Introduction = narrative.Introduction;
        existing.PropertyDescription = narrative.PropertyDescription;
        existing.Methodology = narrative.Methodology;
        existing.Findings = narrative.Findings;
        existing.FundingAnalysis = narrative.FundingAnalysis;
        existing.Recommendations = narrative.Recommendations;
        existing.Conclusion = narrative.Conclusion;
        existing.AdditionalNotes = narrative.AdditionalNotes;
        
        // Update word count
        existing.TotalWordCount = existing.CalculateWordCount();
        existing.DateModified = DateTime.UtcNow;

        // Update status to InProgress if still Draft and has content
        if (existing.Status == NarrativeStatus.Draft && existing.TotalWordCount > 0)
        {
            existing.Status = NarrativeStatus.InProgress;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SaveSectionAsync(Guid narrativeId, string sectionName, string content, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        // Update the specific section
        switch (sectionName.ToLowerInvariant())
        {
            case "executivesummary":
                narrative.ExecutiveSummary = content;
                break;
            case "introduction":
                narrative.Introduction = content;
                break;
            case "propertydescription":
                narrative.PropertyDescription = content;
                break;
            case "methodology":
                narrative.Methodology = content;
                break;
            case "findings":
                narrative.Findings = content;
                break;
            case "fundinganalysis":
                narrative.FundingAnalysis = content;
                break;
            case "recommendations":
                narrative.Recommendations = content;
                break;
            case "conclusion":
                narrative.Conclusion = content;
                break;
            case "additionalnotes":
                narrative.AdditionalNotes = content;
                break;
            default:
                _logger.LogWarning("Unknown section name: {SectionName}", sectionName);
                return false;
        }

        narrative.TotalWordCount = narrative.CalculateWordCount();
        narrative.DateModified = DateTime.UtcNow;

        // Update status if needed
        if (narrative.Status == NarrativeStatus.Draft && narrative.TotalWordCount > 0)
        {
            narrative.Status = NarrativeStatus.InProgress;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid narrativeId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        narrative.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Deleted narrative {NarrativeId}", narrativeId);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // STATUS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public async Task<bool> SubmitForReviewAsync(Guid narrativeId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        narrative.Status = NarrativeStatus.PendingReview;
        narrative.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ApproveAsync(Guid narrativeId, Guid reviewerUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        narrative.Status = NarrativeStatus.Approved;
        narrative.ReviewedByUserId = reviewerUserId;
        narrative.ReviewedAt = DateTime.UtcNow;
        narrative.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Narrative {NarrativeId} approved by user {UserId}", narrativeId, reviewerUserId);
        return true;
    }

    public async Task<bool> RequestRevisionsAsync(Guid narrativeId, Guid reviewerUserId, string? notes = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        narrative.Status = NarrativeStatus.RevisionRequired;
        narrative.ReviewedByUserId = reviewerUserId;
        narrative.ReviewedAt = DateTime.UtcNow;
        narrative.ReviewNotes = notes;
        narrative.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CompleteAsync(Guid narrativeId, Guid completedByUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        // Validate before completing
        if (!narrative.HasRequiredSections())
        {
            _logger.LogWarning("Cannot complete narrative {NarrativeId} - missing required sections", narrativeId);
            return false;
        }

        narrative.CompletedByUserId = completedByUserId;
        narrative.CompletedAt = DateTime.UtcNow;
        narrative.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkAsPublishedAsync(Guid narrativeId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var narrative = await context.Narratives
            .FirstOrDefaultAsync(n => n.Id == narrativeId && 
                                      n.TenantId == _tenantContext.TenantId.Value &&
                                      n.DateDeleted == null, ct);

        if (narrative == null) return false;

        narrative.Status = NarrativeStatus.Published;
        narrative.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    public async Task<NarrativeValidationResult> ValidateAsync(Guid narrativeId, CancellationToken ct = default)
    {
        var narrative = await GetByIdAsync(narrativeId, ct);
        
        if (narrative == null)
        {
            return new NarrativeValidationResult
            {
                IsValid = false,
                MissingSections = ["Narrative not found"]
            };
        }

        var missingSections = narrative.GetMissingSections();
        var wordCount = narrative.CalculateWordCount();
        var warnings = new List<string>();

        if (wordCount < 500)
        {
            warnings.Add($"Narrative has only {wordCount} words. Recommended minimum is 500 words.");
        }

        if (string.IsNullOrWhiteSpace(narrative.Methodology))
        {
            warnings.Add("Methodology section is empty - consider explaining your approach.");
        }

        if (string.IsNullOrWhiteSpace(narrative.FundingAnalysis))
        {
            warnings.Add("Funding Analysis section is empty - consider explaining the financial projections.");
        }

        return new NarrativeValidationResult
        {
            IsValid = missingSections.Count == 0,
            MissingSections = missingSections,
            Warnings = warnings,
            TotalWordCount = wordCount
        };
    }

    public async Task<bool> HasApprovedNarrativeAsync(Guid studyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.Narratives
            .AnyAsync(n => n.TenantId == _tenantContext.TenantId.Value &&
                          n.ReserveStudyId == studyId &&
                          n.DateDeleted == null &&
                          (n.Status == NarrativeStatus.Approved || n.Status == NarrativeStatus.Published), ct);
    }

    // ═══════════════════════════════════════════════════════════════
    // TEMPLATES
    // ═══════════════════════════════════════════════════════════════

    public IEnumerable<NarrativeTemplate> GetAvailableTemplates()
    {
        return
        [
            new NarrativeTemplate
            {
                Name = "Standard HOA",
                Description = "Standard template for homeowner association reserve studies",
                ExecutiveSummary = "<p>This Reserve Study has been prepared for [COMMUNITY_NAME] to provide a comprehensive analysis of the association's reserve fund requirements. The study examines the current condition of major components, estimates their remaining useful life, and projects the funding needed to maintain the community's assets over a 30-year period.</p>",
                Introduction = "<p>[COMMUNITY_NAME] engaged [COMPANY_NAME] to prepare this Reserve Study in accordance with state requirements and industry standards. The purpose of this study is to:</p><ul><li>Identify and evaluate the major components that the association is responsible for maintaining</li><li>Estimate the remaining useful life and replacement cost of each component</li><li>Develop a funding plan that ensures adequate reserves are available when needed</li></ul>",
                PropertyDescription = "<p>[COMMUNITY_NAME] is a [COMMUNITY_TYPE] community located in [CITY], [STATE]. The community was built in [YEAR_BUILT] and consists of [UNIT_COUNT] units.</p><p>Key features include:</p><ul><li>[FEATURE_1]</li><li>[FEATURE_2]</li><li>[FEATURE_3]</li></ul>",
                Methodology = "<p>This Reserve Study was conducted using the Component Method in accordance with national standards. Our methodology included:</p><ol><li><strong>Site Visit:</strong> A thorough inspection of all common area components was performed on [SITE_VISIT_DATE].</li><li><strong>Document Review:</strong> Financial records, prior studies, and maintenance history were analyzed.</li><li><strong>Cost Research:</strong> Current replacement costs were obtained from local contractors and industry databases.</li><li><strong>Financial Projections:</strong> A 30-year cash flow analysis was prepared using the association's current funding levels and projected expenditures.</li></ol>",
                Findings = "<p>Based on our site visit and analysis, we have identified [COMPONENT_COUNT] reserve components. Key findings include:</p><ul><li><strong>Overall Condition:</strong> The property is in [CONDITION] condition for its age.</li><li><strong>Immediate Concerns:</strong> [IMMEDIATE_CONCERNS]</li><li><strong>Upcoming Major Expenses:</strong> [UPCOMING_EXPENSES]</li></ul>",
                FundingAnalysis = "<p>Our analysis of the association's reserve fund indicates the following:</p><ul><li><strong>Current Reserve Balance:</strong> $[CURRENT_BALANCE]</li><li><strong>Recommended Annual Contribution:</strong> $[RECOMMENDED_CONTRIBUTION]</li><li><strong>Percent Funded:</strong> [PERCENT_FUNDED]%</li></ul><p>The funding plan presented in this study is designed to maintain adequate reserves while minimizing the risk of special assessments.</p>",
                Recommendations = "<p>Based on our analysis, we recommend the following:</p><ol><li><strong>Funding:</strong> Adjust annual reserve contributions to $[RECOMMENDED_CONTRIBUTION] per year.</li><li><strong>Maintenance:</strong> Implement a preventive maintenance program for [COMPONENTS].</li><li><strong>Updates:</strong> Update this Reserve Study in [UPDATE_FREQUENCY] years.</li></ol>",
                Conclusion = "<p>This Reserve Study provides [COMMUNITY_NAME] with a roadmap for maintaining its physical assets and ensuring long-term financial stability. By following the funding recommendations in this study, the association can avoid special assessments and maintain property values.</p><p>We appreciate the opportunity to prepare this study and remain available to answer any questions.</p>"
            },
            new NarrativeTemplate
            {
                Name = "Condominium",
                Description = "Template designed for condominium associations with building components",
                ExecutiveSummary = "<p>This Reserve Study analyzes the major building and site components for [COMMUNITY_NAME] Condominium Association. Our findings indicate that [KEY_FINDING] and we recommend [KEY_RECOMMENDATION].</p>",
                Introduction = "<p>In accordance with state condominium statutes, [COMMUNITY_NAME] Condominium Association has commissioned this Reserve Study to evaluate the long-term capital needs of the property.</p>",
                PropertyDescription = "<p>[COMMUNITY_NAME] is a [STORY_COUNT]-story condominium building containing [UNIT_COUNT] residential units. The building was constructed in [YEAR_BUILT] and features [CONSTRUCTION_TYPE] construction.</p>",
                Methodology = "<p>Our reserve study methodology followed the Component Method approach, including a comprehensive site inspection, review of association documents, and analysis of component conditions and costs.</p>",
                Findings = "<p>The building inspection revealed [FINDING_SUMMARY]. Major systems including [SYSTEMS_LIST] were evaluated for current condition and remaining useful life.</p>",
                FundingAnalysis = "<p>The current reserve fund stands at $[CURRENT_BALANCE], representing [PERCENT_FUNDED]% of the fully-funded balance. Our analysis projects the funding requirements over a 30-year horizon.</p>",
                Recommendations = "<p>We recommend the association [RECOMMENDATIONS]. These steps will help ensure adequate funding for future capital expenditures.</p>",
                Conclusion = "<p>This Reserve Study provides a foundation for sound financial planning. We recommend the Board review these findings with the membership and implement the recommended funding plan.</p>"
            },
            new NarrativeTemplate
            {
                Name = "Minimal",
                Description = "Minimal template with basic section headers",
                ExecutiveSummary = "<p>Executive summary to be completed.</p>",
                Introduction = "<p>Introduction to be completed.</p>",
                PropertyDescription = "<p>Property description to be completed.</p>",
                Methodology = "<p>Methodology to be completed.</p>",
                Findings = "<p>Findings to be completed.</p>",
                FundingAnalysis = "<p>Funding analysis to be completed.</p>",
                Recommendations = "<p>Recommendations to be completed.</p>",
                Conclusion = "<p>Conclusion to be completed.</p>"
            }
        ];
    }

    public async Task<bool> ApplyTemplateAsync(Guid narrativeId, string templateName, CancellationToken ct = default)
    {
        var template = GetAvailableTemplates().FirstOrDefault(t => t.Name == templateName);
        if (template == null) return false;

        var narrative = await GetByIdAsync(narrativeId, ct);
        if (narrative == null) return false;

        ApplyTemplateToNarrative(narrative, template);
        narrative.TemplateUsed = templateName;

        return await UpdateAsync(narrative, ct);
    }

    private static void ApplyTemplateToNarrative(Narrative narrative, NarrativeTemplate template)
    {
        narrative.ExecutiveSummary = template.ExecutiveSummary;
        narrative.Introduction = template.Introduction;
        narrative.PropertyDescription = template.PropertyDescription;
        narrative.Methodology = template.Methodology;
        narrative.Findings = template.Findings;
        narrative.FundingAnalysis = template.FundingAnalysis;
        narrative.Recommendations = template.Recommendations;
        narrative.Conclusion = template.Conclusion;
    }

    // ═══════════════════════════════════════════════════════════════
    // VERSION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public async Task<Narrative?> CreateNewVersionAsync(Guid existingNarrativeId, Guid authorUserId, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(existingNarrativeId, ct);
        if (existing == null) return null;

        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Mark existing as superseded
        var existingEntity = await context.Narratives.FindAsync([existingNarrativeId], ct);
        if (existingEntity != null)
        {
            existingEntity.Status = NarrativeStatus.Superseded;
            existingEntity.DateModified = DateTime.UtcNow;
        }

        // Parse version and increment
        var versionParts = existing.Version.Split('.');
        var majorVersion = int.Parse(versionParts[0]);
        var minorVersion = versionParts.Length > 1 ? int.Parse(versionParts[1]) : 0;
        var newVersion = $"{majorVersion}.{minorVersion + 1}";

        // Create new version with copied content
        var newNarrative = new Narrative
        {
            TenantId = _tenantContext.TenantId.Value,
            ReserveStudyId = existing.ReserveStudyId,
            AuthorUserId = authorUserId,
            Status = NarrativeStatus.Draft,
            StartedAt = DateTime.UtcNow,
            Version = newVersion,
            TemplateUsed = existing.TemplateUsed,
            // Copy content
            ExecutiveSummary = existing.ExecutiveSummary,
            Introduction = existing.Introduction,
            PropertyDescription = existing.PropertyDescription,
            Methodology = existing.Methodology,
            Findings = existing.Findings,
            FundingAnalysis = existing.FundingAnalysis,
            Recommendations = existing.Recommendations,
            Conclusion = existing.Conclusion,
            AdditionalNotes = existing.AdditionalNotes,
            TotalWordCount = existing.TotalWordCount
        };

        context.Narratives.Add(newNarrative);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Created new version {Version} of narrative for study {StudyId}", 
            newVersion, existing.ReserveStudyId);

        return newNarrative;
    }
}
