using System.Security.Cryptography;
using System.Text;
using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Service for managing click-wrap proposal acceptances.
/// </summary>
public class ProposalAcceptanceService : IProposalAcceptanceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IScopeComparisonService _scopeComparisonService;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<ProposalAcceptanceService> _logger;

    public ProposalAcceptanceService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        IScopeComparisonService scopeComparisonService,
        IInvoiceService invoiceService,
        ILogger<ProposalAcceptanceService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _scopeComparisonService = scopeComparisonService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<ProposalAcceptance?> GetAcceptanceByStudyIdAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        return await db.ProposalAcceptances
            .AsNoTracking()
            .Include(a => a.AcceptedByUser)
            .Include(a => a.AcceptanceTermsTemplate)
            .Where(a => a.ReserveStudyId == reserveStudyId && a.IsValid && a.IsActive)
            .OrderByDescending(a => a.AcceptedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ProposalAcceptance>> GetAcceptanceHistoryAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        return await db.ProposalAcceptances
            .AsNoTracking()
            .Include(a => a.AcceptedByUser)
            .Include(a => a.AcceptanceTermsTemplate)
            .Where(a => a.ReserveStudyId == reserveStudyId)
            .OrderByDescending(a => a.AcceptedAt)
            .ToListAsync(ct);
    }

    public async Task<ProposalAcceptance?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        return await db.ProposalAcceptances
            .AsNoTracking()
            .Include(a => a.AcceptedByUser)
            .Include(a => a.AcceptanceTermsTemplate)
            .Include(a => a.ReserveStudy)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<bool> HasValidAcceptanceAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        return await db.ProposalAcceptances
            .AnyAsync(a => a.ReserveStudyId == reserveStudyId && a.IsValid && a.IsActive, ct);
    }

    public async Task<ProposalAcceptance> RecordAcceptanceAsync(ProposalAcceptance acceptance, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Ensure tenant ID is set
        acceptance.TenantId = _tenantContext.TenantId.Value;
        
        // Generate ID if not set
        if (acceptance.Id == Guid.Empty)
            acceptance.Id = Guid.CreateVersion7();

        // Set timestamps
        acceptance.AcceptedAt = DateTime.UtcNow;
        acceptance.DateCreated = DateTime.UtcNow;
        acceptance.IsActive = true;
        acceptance.IsValid = true;

        // Compute terms hash if template is provided
        if (acceptance.AcceptanceTermsTemplate != null && string.IsNullOrEmpty(acceptance.TermsContentHash))
        {
            acceptance.TermsContentHash = ComputeTermsHash(acceptance.AcceptanceTermsTemplate.TermsText);
        }

        db.ProposalAcceptances.Add(acceptance);
        await db.SaveChangesAsync(ct);

        // Capture original scope for variance tracking after site visit
        try
        {
            await _scopeComparisonService.CaptureOriginalScopeAsync(acceptance.ReserveStudyId);
            _logger.LogInformation(
                "Captured original scope for study {StudyId} at proposal acceptance",
                acceptance.ReserveStudyId);
        }
        catch (Exception ex)
        {
            // Log but don't fail the acceptance - scope comparison is optional
            _logger.LogWarning(ex,
                "Failed to capture original scope for study {StudyId}. Scope comparison may not be available.",
                acceptance.ReserveStudyId);
        }
        
        // Check tenant setting for auto-generating invoice on acceptance
        try
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == acceptance.TenantId, ct);
            if (tenant?.AutoGenerateInvoiceOnAcceptance == true)
            {
                _logger.LogInformation(
                    "[AutoGenerateInvoiceOnAcceptance] Auto-generating draft invoice for study {StudyId}",
                    acceptance.ReserveStudyId);
                
                var invoice = await _invoiceService.CreateFromProposalAsync(acceptance.ReserveStudyId, ct);
                _logger.LogInformation(
                    "[AutoGenerateInvoiceOnAcceptance] Created draft invoice {InvoiceNumber} for study {StudyId}",
                    invoice.InvoiceNumber, acceptance.ReserveStudyId);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the acceptance - invoice generation is optional
            _logger.LogWarning(ex,
                "[AutoGenerateInvoiceOnAcceptance] Failed to auto-generate invoice for study {StudyId}",
                acceptance.ReserveStudyId);
        }

        _logger.LogInformation(
            "Recorded proposal acceptance {AcceptanceId} for study {StudyId} by user {UserId}",
            acceptance.Id, acceptance.ReserveStudyId, acceptance.AcceptedByUserId);

        return acceptance;
    }

    public async Task<bool> RevokeAcceptanceAsync(Guid acceptanceId, string reason, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        var acceptance = await db.ProposalAcceptances
            .FirstOrDefaultAsync(a => a.Id == acceptanceId && a.IsValid, ct);

        if (acceptance == null)
            return false;

        acceptance.IsValid = false;
        acceptance.RevocationReason = reason;
        acceptance.RevokedAt = DateTime.UtcNow;
        acceptance.DateModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Revoked proposal acceptance {AcceptanceId} for study {StudyId}. Reason: {Reason}",
            acceptanceId, acceptance.ReserveStudyId, reason);

        return true;
    }

    public async Task<AcceptanceTermsTemplate?> GetActiveTermsTemplateAsync(TermsType type = TermsType.ProposalAcceptance, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            return null;
            
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;

        // Get tenant-specific template
        var template = await db.AcceptanceTermsTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId.Value)
            .Where(t => t.Type == type && t.IsActive && t.EffectiveDate <= now)
            .Where(t => t.ExpirationDate == null || t.ExpirationDate > now)
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        return template;
    }

    public async Task<AcceptanceTermsTemplate?> GetDefaultTermsTemplateAsync(TermsType type = TermsType.ProposalAcceptance, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            return null;
            
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.AcceptanceTermsTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId.Value)
            .Where(t => t.Type == type && t.IsActive && t.IsDefault)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AcceptanceTermsTemplate>> GetAllTermsTemplatesAsync(TermsType? type = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.AcceptanceTermsTemplates.AsNoTracking();

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        return await query
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.Version)
            .ToListAsync(ct);
    }

    public async Task<AcceptanceTermsTemplate> CreateTermsTemplateAsync(AcceptanceTermsTemplate template, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        template.TenantId = _tenantContext.TenantId.Value;
        
        if (template.Id == Guid.Empty)
            template.Id = Guid.CreateVersion7();

        template.DateCreated = DateTime.UtcNow;
        template.ContentHash = ComputeTermsHash(template.TermsText);

        // If this is the first template of this type, make it default
        var existingCount = await db.AcceptanceTermsTemplates
            .CountAsync(t => t.Type == template.Type, ct);
        
        if (existingCount == 0)
            template.IsDefault = true;

        db.AcceptanceTermsTemplates.Add(template);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created terms template {TemplateId} version {Version} for type {Type}",
            template.Id, template.Version, template.Type);

        return template;
    }

    public async Task<AcceptanceTermsTemplate> UpdateTermsTemplateAsync(AcceptanceTermsTemplate template, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.AcceptanceTermsTemplates
            .FirstOrDefaultAsync(t => t.Id == template.Id, ct);

        if (existing == null)
            throw new InvalidOperationException("Template not found.");

        existing.Name = template.Name;
        existing.TermsText = template.TermsText;
        existing.Summary = template.Summary;
        existing.CheckboxText = template.CheckboxText;
        existing.AcceptButtonText = template.AcceptButtonText;
        existing.VersionNotes = template.VersionNotes;
        existing.ContentHash = ComputeTermsHash(template.TermsText);
        existing.DateModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return existing;
    }

    public async Task<bool> SetDefaultTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var template = await db.AcceptanceTermsTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template == null)
            return false;

        // Clear default from other templates of the same type
        var otherDefaults = await db.AcceptanceTermsTemplates
            .Where(t => t.Type == template.Type && t.IsDefault && t.Id != templateId)
            .ToListAsync(ct);

        foreach (var other in otherDefaults)
        {
            other.IsDefault = false;
            other.DateModified = DateTime.UtcNow;
        }

        template.IsDefault = true;
        template.DateModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return true;
    }

    public string ComputeTermsHash(string termsText)
    {
        if (string.IsNullOrEmpty(termsText))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(termsText);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task SeedDefaultTermsIfNeededAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Check if this tenant already has templates
        var hasTemplates = await db.AcceptanceTermsTemplates
            .AnyAsync(t => t.TenantId == _tenantContext.TenantId.Value && t.Type == TermsType.ProposalAcceptance, ct);

        if (hasTemplates)
            return;

        var defaultTemplate = new AcceptanceTermsTemplate
        {
            Id = Guid.CreateVersion7(),
            TenantId = _tenantContext.TenantId.Value,
            Name = "Standard Proposal Acceptance Terms",
            Version = "1.0",
            Type = TermsType.ProposalAcceptance,
            TermsText = GetDefaultProposalTermsText(),
            Summary = "By accepting this proposal, you authorize us to proceed with the reserve study as described.",
            CheckboxText = "I have read and agree to the terms and conditions above. I am authorized to accept this proposal on behalf of the organization.",
            AcceptButtonText = "Accept Proposal",
            EffectiveDate = DateTime.UtcNow,
            IsActive = true,
            IsDefault = true,
            DateCreated = DateTime.UtcNow
        };

        defaultTemplate.ContentHash = ComputeTermsHash(defaultTemplate.TermsText);

        db.AcceptanceTermsTemplates.Add(defaultTemplate);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Seeded default proposal acceptance terms for tenant {TenantId}",
            _tenantContext.TenantId.Value);
    }

    private static string GetDefaultProposalTermsText()
    {
        return """
## Proposal Acceptance Agreement

By clicking "Accept Proposal" below, you agree to the following terms and conditions:

### 1. Scope of Work
You are accepting the reserve study proposal as described in the attached documentation. The scope includes all services, deliverables, and timelines outlined in the proposal.

### 2. Authorization
By accepting this proposal, you represent and warrant that:
- You are authorized to enter into this agreement on behalf of the organization
- You have the authority to bind the organization to these terms
- The information provided in the reserve study request is accurate and complete

### 3. Payment Terms
- Payment is due according to the schedule outlined in the proposal
- All fees and costs are as specified in the proposal document
- Late payments may be subject to additional fees as permitted by law

### 4. Cooperation
You agree to:
- Provide timely access to the property for site visits
- Supply requested documents and information promptly
- Designate a point of contact for communications

### 5. Deliverables
Upon completion, you will receive the reserve study report and associated materials as described in the proposal.

### 6. Electronic Signature
You acknowledge that your electronic acceptance of this proposal constitutes a valid and binding signature under applicable electronic signature laws.

### 7. Acceptance Record
A record of this acceptance, including your name, the date and time, and your IP address, will be maintained for legal and compliance purposes.

---

**By clicking "Accept Proposal," you acknowledge that you have read, understood, and agree to be bound by these terms and conditions.**
""";
    }
}
