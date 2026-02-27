using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

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
                ExecutiveSummary = "<p>This Reserve Study has been prepared for {AssociationName} to provide a comprehensive analysis of the association's reserve fund requirements. The study examines the current condition of major components, estimates their remaining useful life, and projects the funding needed to maintain the community's assets over a {ProjectionYears}-year period.</p>",
                Introduction = "<p>{AssociationName} engaged {CompanyName} to prepare this Reserve Study in accordance with state requirements and industry standards. The purpose of this study is to:</p><ul><li>Identify and evaluate the major components that the association is responsible for maintaining</li><li>Estimate the remaining useful life and replacement cost of each component</li><li>Develop a funding plan that ensures adequate reserves are available when needed</li></ul>",
                PropertyDescription = "<p>{AssociationName} is a {CommunityType} community located in {CityState}. The community was established in {EstablishedYear} and consists of {UnitCount} units.</p>",
                Methodology = "<p>This Reserve Study was conducted using the Component Method in accordance with national standards. Our methodology included:</p><ol><li><strong>Site Visit:</strong> A thorough inspection of all common area components was performed on {InspectionDate}.</li><li><strong>Document Review:</strong> Financial records, prior studies, and maintenance history were analyzed.</li><li><strong>Cost Research:</strong> Current replacement costs were obtained from local contractors and industry databases.</li><li><strong>Financial Projections:</strong> A {ProjectionYears}-year cash flow analysis was prepared using the association's current funding levels and projected expenditures.</li></ol>",
                Findings = "<p>Based on our site visit and analysis, we have identified the reserve components for {AssociationName}. Key findings include:</p><ul><li><strong>Current Fund Status:</strong> {FundStatusLabel} ({PercentFunded} funded)</li><li><strong>Starting Reserve Balance:</strong> {StartingReserveBalance}</li><li><strong>Recommended First Year Contribution:</strong> {FirstYearContribution}</li></ul>",
                FundingAnalysis = "<p>Our analysis of the association's reserve fund indicates the following:</p><ul><li><strong>Current Reserve Balance:</strong> {StartingReserveBalance}</li><li><strong>Ideal (Fully Funded) Balance:</strong> {IdealBalance}</li><li><strong>Percent Funded:</strong> {PercentFunded}</li><li><strong>Fund Status:</strong> {FundStatusLabel}</li></ul><p>The funding plan presented in this study is designed to maintain adequate reserves while minimizing the risk of special assessments.</p>",
                Recommendations = "<p>Based on our analysis, we recommend the following:</p><ol><li><strong>Funding:</strong> Adopt the recommended contribution schedule outlined in this study.</li><li><strong>Maintenance:</strong> Implement a preventive maintenance program for major components.</li><li><strong>Updates:</strong> Update this Reserve Study every 3-5 years or sooner if significant changes occur.</li></ol>",
                Conclusion = "<p>This Reserve Study provides {AssociationName} with a roadmap for maintaining its physical assets and ensuring long-term financial stability. By following the funding recommendations in this study, the association can avoid special assessments and maintain property values.</p><p>We appreciate the opportunity to prepare this study and remain available to answer any questions.</p>"
            },
            new NarrativeTemplate
            {
                Name = "Condominium",
                Description = "Template designed for condominium associations with building components",
                ExecutiveSummary = "<p>This Reserve Study analyzes the major building and site components for {AssociationName}. The current fund status is {FundStatusLabel} with {PercentFunded} funded. We recommend adopting the funding plan outlined in this study.</p>",
                Introduction = "<p>In accordance with state condominium statutes, {AssociationName} has commissioned this Reserve Study to evaluate the long-term capital needs of the property.</p>",
                PropertyDescription = "<p>{AssociationName} is a {CommunityType} community located in {CityState}. The community was established in {EstablishedYear} and contains {UnitCount} units.</p>",
                Methodology = "<p>Our reserve study methodology followed the Component Method approach, including a comprehensive site inspection on {InspectionDate}, review of association documents, and analysis of component conditions and costs.</p>",
                Findings = "<p>The site inspection was conducted on {InspectionDate}. Major building systems and common area components were evaluated for current condition and remaining useful life.</p>",
                FundingAnalysis = "<p>The current reserve fund balance is {StartingReserveBalance}, representing {PercentFunded} of the fully-funded balance of {IdealBalance}. Our analysis projects the funding requirements over a {ProjectionYears}-year horizon.</p>",
                Recommendations = "<p>We recommend the association adopt the funding plan presented in this study. The recommended first year contribution is {FirstYearContribution}. These steps will help ensure adequate funding for future capital expenditures.</p>",
                Conclusion = "<p>This Reserve Study provides a foundation for sound financial planning. We recommend the Board review these findings with the membership and implement the recommended funding plan.</p>"
            },
            new NarrativeTemplate
            {
                Name = "Minimal",
                Description = "Minimal template with basic section headers - fill in your own content",
                ExecutiveSummary = "<p>Executive summary for {AssociationName} reserve study.</p>",
                Introduction = "<p>Introduction for {AssociationName} located in {CityState}.</p>",
                PropertyDescription = "<p>Property description for {AssociationName}.</p>",
                Methodology = "<p>Site inspection conducted on {InspectionDate}.</p>",
                Findings = "<p>Findings for {AssociationName}.</p>",
                FundingAnalysis = "<p>Current reserve balance: {StartingReserveBalance}. Percent funded: {PercentFunded}.</p>",
                Recommendations = "<p>Recommendations for {AssociationName}.</p>",
                Conclusion = "<p>Conclusion for {AssociationName} reserve study.</p>"
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
