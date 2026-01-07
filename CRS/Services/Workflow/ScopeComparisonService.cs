using CRS.Data;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Workflow;

/// <summary>
/// Service for managing scope comparisons between original (HOA estimate) 
/// and actual (site visit) element counts.
/// </summary>
public class ScopeComparisonService : IScopeComparisonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ScopeComparisonService> _logger;

    public ScopeComparisonService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<ScopeComparisonService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScopeComparison> CaptureOriginalScopeAsync(Guid reserveStudyId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is required");

        // Check if one already exists
        var existing = await context.ScopeComparisons
            .FirstOrDefaultAsync(sc => sc.ReserveStudyId == reserveStudyId && sc.TenantId == tenantId);

        if (existing != null)
        {
            _logger.LogWarning("Scope comparison already exists for study {StudyId}, updating original counts", reserveStudyId);
        }

        // Get HOA estimates from StudyRequest, falling back to actual element counts if no estimates
        var studyRequest = await context.StudyRequests
            .FirstOrDefaultAsync(sr => sr.Id == reserveStudyId && sr.TenantId == tenantId);

        int buildingCount, commonCount, additionalCount;

        if (studyRequest != null && 
            (studyRequest.EstimatedBuildingElementCount.HasValue || 
             studyRequest.EstimatedCommonElementCount.HasValue || 
             studyRequest.EstimatedAdditionalElementCount.HasValue))
        {
            // Use HOA estimates from request
            buildingCount = studyRequest.EstimatedBuildingElementCount ?? 0;
            commonCount = studyRequest.EstimatedCommonElementCount ?? 0;
            additionalCount = studyRequest.EstimatedAdditionalElementCount ?? 0;
            
            _logger.LogInformation(
                "Using HOA estimates for study {StudyId}: {Building} building, {Common} common, {Additional} additional",
                reserveStudyId, buildingCount, commonCount, additionalCount);
        }
        else
        {
            // Fallback: use current actual element counts (legacy behavior)
            var study = await context.ReserveStudies
                .Include(s => s.ReserveStudyBuildingElements)
                .Include(s => s.ReserveStudyCommonElements)
                .Include(s => s.ReserveStudyAdditionalElements)
                .FirstOrDefaultAsync(s => s.Id == reserveStudyId && s.TenantId == tenantId);

            if (study == null)
            {
                throw new InvalidOperationException($"Reserve study {reserveStudyId} not found");
            }

            buildingCount = study.ReserveStudyBuildingElements?.Count ?? 0;
            commonCount = study.ReserveStudyCommonElements?.Count ?? 0;
            additionalCount = study.ReserveStudyAdditionalElements?.Count ?? 0;
            
            _logger.LogInformation(
                "No HOA estimates found, using actual counts for study {StudyId}: {Building} building, {Common} common, {Additional} additional",
                reserveStudyId, buildingCount, commonCount, additionalCount);
        }

        if (existing != null)
        {
            existing.OriginalBuildingElementCount = buildingCount;
            existing.OriginalCommonElementCount = commonCount;
            existing.OriginalAdditionalElementCount = additionalCount;
            existing.OriginalCapturedAt = DateTime.UtcNow;
            existing.Status = ScopeComparisonStatus.Pending;
        }
        else
        {
            existing = new ScopeComparison
            {
                TenantId = tenantId,
                ReserveStudyId = reserveStudyId,
                OriginalBuildingElementCount = buildingCount,
                OriginalCommonElementCount = commonCount,
                OriginalAdditionalElementCount = additionalCount,
                OriginalCapturedAt = DateTime.UtcNow,
                Status = ScopeComparisonStatus.Pending
            };
            context.ScopeComparisons.Add(existing);
        }

        await context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Captured original scope for study {StudyId}: {Building} building, {Common} common, {Additional} additional elements",
            reserveStudyId, buildingCount, commonCount, additionalCount);

        return existing;
    }

    /// <inheritdoc />
    public async Task<ScopeComparisonResult> CompareAndEvaluateAsync(Guid reserveStudyId, Guid userId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is required");

        // Get the scope comparison record
        var comparison = await context.ScopeComparisons
            .FirstOrDefaultAsync(sc => sc.ReserveStudyId == reserveStudyId && sc.TenantId == tenantId);

        if (comparison == null)
        {
            return ScopeComparisonResult.Failure(
                "No scope comparison record found. Original scope may not have been captured at proposal acceptance.");
        }

        // Get current (actual) element counts
        var study = await context.ReserveStudies
            .Include(s => s.ReserveStudyBuildingElements)
            .Include(s => s.ReserveStudyCommonElements)
            .Include(s => s.ReserveStudyAdditionalElements)
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId && s.TenantId == tenantId);

        if (study == null)
        {
            return ScopeComparisonResult.Failure($"Reserve study {reserveStudyId} not found");
        }

        // Update actual counts
        comparison.ActualBuildingElementCount = study.ReserveStudyBuildingElements?.Count ?? 0;
        comparison.ActualCommonElementCount = study.ReserveStudyCommonElements?.Count ?? 0;
        comparison.ActualAdditionalElementCount = study.ReserveStudyAdditionalElements?.Count ?? 0;
        comparison.ComparedAt = DateTime.UtcNow;
        comparison.ComparedByUserId = userId;

        // Get tenant settings
        var settings = await GetSettingsInternalAsync(context, tenantId);

        // Evaluate against thresholds
        var exceedsThreshold = comparison.ExceedsThreshold(settings);
        var shouldBlock = exceedsThreshold && 
                          settings.Mode != ScopeChangeMode.NoAction && 
                          settings.Mode != ScopeChangeMode.VarianceReportOnly;

        // Update status based on evaluation
        if (settings.Mode == ScopeChangeMode.NoAction)
        {
            comparison.Status = ScopeComparisonStatus.WithinThreshold;
        }
        else if (exceedsThreshold)
        {
            comparison.Status = ScopeComparisonStatus.ExceedsThreshold;
        }
        else
        {
            comparison.Status = ScopeComparisonStatus.WithinThreshold;
        }

        await context.SaveChangesAsync();

        // Build result message
        var message = BuildComparisonMessage(comparison, settings, exceedsThreshold);

        _logger.LogInformation(
            "Scope comparison for study {StudyId}: Original={Original}, Actual={Actual}, Variance={Variance} ({VariancePercent}%), ExceedsThreshold={Exceeds}",
            reserveStudyId, 
            comparison.OriginalTotalCount, 
            comparison.ActualTotalCount, 
            comparison.VarianceCount,
            comparison.VariancePercent,
            exceedsThreshold);

        return ScopeComparisonResult.Success(comparison, exceedsThreshold, shouldBlock, message);
    }

    /// <inheritdoc />
    public async Task<ScopeComparison?> GetByStudyIdAsync(Guid reserveStudyId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is required");

        return await context.ScopeComparisons
            .FirstOrDefaultAsync(sc => sc.ReserveStudyId == reserveStudyId && sc.TenantId == tenantId);
    }

    /// <inheritdoc />
    public async Task<TenantScopeChangeSettings> GetSettingsAsync(int tenantId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        return await GetSettingsInternalAsync(context, tenantId);
    }

    private async Task<TenantScopeChangeSettings> GetSettingsInternalAsync(ApplicationDbContext context, int tenantId)
    {
        var settings = await context.TenantScopeChangeSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings == null)
        {
            // Return default settings
            return new TenantScopeChangeSettings { TenantId = tenantId };
        }

        return settings;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(TenantScopeChangeSettings settings)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var existing = await context.TenantScopeChangeSettings
            .FirstOrDefaultAsync(s => s.TenantId == settings.TenantId);

        if (existing == null)
        {
            context.TenantScopeChangeSettings.Add(settings);
        }
        else
        {
            existing.Mode = settings.Mode;
            existing.VarianceThresholdPercent = settings.VarianceThresholdPercent;
            existing.VarianceThresholdCount = settings.VarianceThresholdCount;
            existing.RequireHoaApproval = settings.RequireHoaApproval;
            existing.UseTwoPhaseProposal = settings.UseTwoPhaseProposal;
            existing.AutoNotifyHoaOnVariance = settings.AutoNotifyHoaOnVariance;
            existing.VarianceNotificationTemplateId = settings.VarianceNotificationTemplateId;
            existing.AllowStaffOverride = settings.AllowStaffOverride;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = settings.UpdatedByUserId;
        }

        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ScopeComparison> OverrideVarianceAsync(Guid scopeComparisonId, Guid userId, string reason)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is required");

        var comparison = await context.ScopeComparisons
            .FirstOrDefaultAsync(sc => sc.Id == scopeComparisonId && sc.TenantId == tenantId);

        if (comparison == null)
        {
            throw new InvalidOperationException($"Scope comparison {scopeComparisonId} not found");
        }

        // Check if override is allowed
        var settings = await GetSettingsInternalAsync(context, tenantId);
        if (!settings.AllowStaffOverride)
        {
            throw new InvalidOperationException("Staff override is not allowed for this tenant");
        }

        comparison.Status = ScopeComparisonStatus.Overridden;
        comparison.OverriddenByUserId = userId;
        comparison.OverriddenAt = DateTime.UtcNow;
        comparison.OverrideReason = reason;

        await context.SaveChangesAsync();

        _logger.LogWarning(
            "Scope variance overridden for study {StudyId} by user {UserId}: {Reason}",
            comparison.ReserveStudyId, userId, reason);

        return comparison;
    }

    /// <inheritdoc />
    public async Task<ScopeComparison> UpdateStatusAsync(Guid scopeComparisonId, ScopeComparisonStatus newStatus, string? notes = null)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var tenantId = _tenantContext.TenantId 
            ?? throw new InvalidOperationException("Tenant context is required");

        var comparison = await context.ScopeComparisons
            .FirstOrDefaultAsync(sc => sc.Id == scopeComparisonId && sc.TenantId == tenantId);

        if (comparison == null)
        {
            throw new InvalidOperationException($"Scope comparison {scopeComparisonId} not found");
        }

        comparison.Status = newStatus;
        if (!string.IsNullOrEmpty(notes))
        {
            comparison.Notes = notes;
        }

        await context.SaveChangesAsync();

        _logger.LogInformation(
            "Scope comparison {Id} status updated to {Status}",
            scopeComparisonId, newStatus);

        return comparison;
    }

    private string BuildComparisonMessage(ScopeComparison comparison, TenantScopeChangeSettings settings, bool exceedsThreshold)
    {
        var variance = comparison.VarianceCount;
        var direction = variance > 0 ? "more" : "fewer";
        var absVariance = Math.Abs(variance);

        if (settings.Mode == ScopeChangeMode.NoAction)
        {
            return $"Scope comparison complete. {absVariance} {direction} elements than originally estimated ({comparison.VariancePercent:F1}% variance). Proceeding without action per tenant settings.";
        }

        if (!exceedsThreshold)
        {
            return $"Scope variance within threshold: {absVariance} {direction} elements ({comparison.VariancePercent:F1}% variance). Proceeding normally.";
        }

        return settings.Mode switch
        {
            ScopeChangeMode.VarianceReportOnly => 
                $"⚠️ Scope variance detected: {absVariance} {direction} elements ({comparison.VariancePercent:F1}% variance). Review recommended before proceeding.",
            
            ScopeChangeMode.VarianceWithAmendment => 
                $"🛑 Scope variance exceeds threshold: {absVariance} {direction} elements ({comparison.VariancePercent:F1}% variance). Amendment required before proceeding.",
            
            ScopeChangeMode.AlwaysAmendment => 
                $"🛑 Scope change detected: {absVariance} {direction} elements ({comparison.VariancePercent:F1}% variance). Amendment required per tenant policy.",
            
            ScopeChangeMode.TwoPhase => 
                $"📋 Phase 1 complete: {absVariance} {direction} elements than estimated ({comparison.VariancePercent:F1}% variance). Phase 2 proposal required.",
            
            _ => $"Scope comparison complete: {absVariance} {direction} elements ({comparison.VariancePercent:F1}% variance)."
        };
    }
}
