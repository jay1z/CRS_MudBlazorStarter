using CRS.Core.ReserveCalculator.Models;
using CRS.Core.ReserveCalculator.Services;
using CRS.Data;
using CRS.Models;
using CRS.Models.ReserveStudyCalculator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.Services.ReserveCalculator;

/// <summary>
/// Application service for reserve study calculations.
/// Bridges EF entities with the pure Core calculator.
/// </summary>
public interface IReserveStudyCalculatorService
{
    /// <summary>
    /// Calculates results for a specific scenario.
    /// </summary>
    Task<ReserveStudyResult> CalculateScenarioAsync(int scenarioId);

    /// <summary>
    /// Calculates results directly from a ReserveStudy (with proposal data).
    /// Converts study elements to components and uses study financial data.
    /// </summary>
    /// <param name="studyId">The ReserveStudy ID.</param>
    /// <returns>Calculation result from study data.</returns>
    Task<ReserveStudyResult> CalculateFromStudyAsync(Guid studyId);

    /// <summary>
    /// Gets or creates a scenario from a ReserveStudy.
    /// If no scenario exists for the study, creates one from study elements.
    /// </summary>
    /// <param name="studyId">The ReserveStudy ID.</param>
    /// <param name="createIfMissing">If true, creates a scenario when none exists.</param>
    /// <returns>The scenario, or null if not found and createIfMissing is false.</returns>
    Task<ReserveStudyScenario?> GetOrCreateScenarioForStudyAsync(Guid studyId, bool createIfMissing = true);

    /// <summary>
    /// Gets or creates tenant reserve settings.
    /// </summary>
    Task<TenantReserveSettings> GetOrCreateTenantSettingsAsync(int tenantId);

    /// <summary>
    /// Updates tenant reserve settings.
    /// </summary>
    Task<TenantReserveSettings> UpdateTenantSettingsAsync(TenantReserveSettings settings);

    /// <summary>
    /// Gets effective settings for a scenario (for display in UI).
    /// </summary>
    Task<EffectiveReserveSettings> GetEffectiveSettingsAsync(int scenarioId);
}

/// <summary>
/// Implementation of reserve study calculator service.
/// </summary>
public class ReserveStudyCalculatorService : IReserveStudyCalculatorService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ReserveStudyCalculator _calculator;
    private readonly SettingsResolver _resolver;
    private readonly ReserveStudyAdapter _adapter;
    private readonly ILogger<ReserveStudyCalculatorService>? _logger;

    public ReserveStudyCalculatorService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<ReserveStudyCalculatorService>? logger = null)
    {
        _dbFactory = dbFactory;
        _calculator = new ReserveStudyCalculator();
        _resolver = new SettingsResolver();
        _adapter = new ReserveStudyAdapter(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReserveStudyResult> CalculateScenarioAsync(int scenarioId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        // Load scenario with components
        var scenario = await context.ReserveStudyScenarios
            .Include(s => s.Components.Where(c => c.DateDeleted == null))
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.DateDeleted == null);

        if (scenario == null)
        {
            return ReserveStudyResult.Failure($"Scenario {scenarioId} not found.");
        }

        // Load tenant settings
        var tenantSettings = await GetOrCreateTenantSettingsAsync(context, scenario.TenantId);

        // Build calculator input using adapter
        var input = _adapter.BuildInputFromScenario(scenario, tenantSettings);

        // Execute pure calculation
        var result = _calculator.Calculate(input);

        return result;
    }

    /// <inheritdoc />
    public async Task<ReserveStudyResult> CalculateFromStudyAsync(Guid studyId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        // Load study with financial info first (use AsNoTracking since this is read-only)
        var study = await context.ReserveStudies
            .AsNoTracking()
            .Include(s => s.FinancialInfo)
            .Include(s => s.CurrentProposal)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
        {
            return ReserveStudyResult.Failure($"ReserveStudy {studyId} not found.");
        }

        // Load elements directly (more reliable than nested includes for composite-key junction tables)
        study.ReserveStudyBuildingElements = await context.ReserveStudyBuildingElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.BuildingElement)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        study.ReserveStudyCommonElements = await context.ReserveStudyCommonElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.CommonElement)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        study.ReserveStudyAdditionalElements = await context.ReserveStudyAdditionalElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        // Log element counts for debugging
        _logger?.LogInformation("CalculateFromStudyAsync: Study {StudyId} loaded with " +
            "BuildingElements={BuildingCount}, CommonElements={CommonCount}, AdditionalElements={AdditionalCount}",
            studyId,
            study.ReserveStudyBuildingElements?.Count ?? 0,
            study.ReserveStudyCommonElements?.Count ?? 0,
            study.ReserveStudyAdditionalElements?.Count ?? 0);

        // Log building element details
        if (study.ReserveStudyBuildingElements != null && _logger != null)
        {
            foreach (var be in study.ReserveStudyBuildingElements)
            {
                _logger.LogInformation("BuildingElement: Name={Name}, MinUsefulLife={Min}, MaxUsefulLife={Max}, RemainingLife={Remaining}",
                    be.BuildingElement?.Name ?? "(null)",
                    be.MinUsefulLifeOption?.DisplayText ?? "(null)",
                    be.MaxUsefulLifeOption?.DisplayText ?? "(null)",
                    be.RemainingLifeYears);
            }
        }

        // Load tenant settings
        var tenantSettings = await GetOrCreateTenantSettingsAsync(context, study.TenantId);

        // Build calculator input using adapter
        var input = _adapter.BuildInputFromStudy(study, tenantSettings);

        _logger?.LogInformation("CalculateFromStudyAsync: Adapter produced {ComponentCount} components", 
            input.Components.Count);

        // Execute pure calculation
        var result = _calculator.Calculate(input);

        return result;
    }

    /// <inheritdoc />
    public async Task<ReserveStudyScenario?> GetOrCreateScenarioForStudyAsync(Guid studyId, bool createIfMissing = true)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        // Check for existing scenario
        var scenario = await context.ReserveStudyScenarios
            .Include(s => s.Components.Where(c => c.DateDeleted == null))
            .FirstOrDefaultAsync(s => s.ReserveStudyId == studyId && s.DateDeleted == null);

        if (scenario != null)
            return scenario;

        if (!createIfMissing)
            return null;

        // Load study with financial info (don't need to track since we're just reading)
        var study = await context.ReserveStudies
            .AsNoTracking()
            .Include(s => s.FinancialInfo)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return null;

        // Load elements directly (more reliable for composite-key junction tables)
        // Use AsNoTracking to avoid tracking conflicts when multiple elements share the same options
        study.ReserveStudyBuildingElements = await context.ReserveStudyBuildingElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.BuildingElement)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        study.ReserveStudyCommonElements = await context.ReserveStudyCommonElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.CommonElement)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        study.ReserveStudyAdditionalElements = await context.ReserveStudyAdditionalElements
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == studyId)
            .Include(e => e.MinUsefulLifeOption)
            .Include(e => e.MaxUsefulLifeOption)
            .Include(e => e.UsefulLifeOption)
            .Include(e => e.RemainingLifeOption)
            .ToListAsync();

        // Create scenario from study using adapter
        scenario = _adapter.CreateScenarioFromStudy(study, "From Proposal");

        context.ReserveStudyScenarios.Add(scenario);
        await context.SaveChangesAsync();

        return scenario;
    }

    /// <inheritdoc />
    public async Task<TenantReserveSettings> GetOrCreateTenantSettingsAsync(int tenantId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        return await GetOrCreateTenantSettingsAsync(context, tenantId);
    }

    private async Task<TenantReserveSettings> GetOrCreateTenantSettingsAsync(ApplicationDbContext context, int tenantId)
    {
        var settings = await context.TenantReserveSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings == null)
        {
            settings = SettingsResolver.CreateDefaultSettings(tenantId);
            context.TenantReserveSettings.Add(settings);
            await context.SaveChangesAsync();
        }

        return settings;
    }

    /// <inheritdoc />
    public async Task<TenantReserveSettings> UpdateTenantSettingsAsync(TenantReserveSettings settings)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        settings.UpdatedAt = DateTime.UtcNow;
        context.TenantReserveSettings.Update(settings);
        await context.SaveChangesAsync();
        return settings;
    }

    /// <inheritdoc />
    public async Task<EffectiveReserveSettings> GetEffectiveSettingsAsync(int scenarioId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var scenario = await context.ReserveStudyScenarios
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.DateDeleted == null);

        if (scenario == null)
        {
            throw new InvalidOperationException($"Scenario {scenarioId} not found.");
        }

        var tenantSettings = await GetOrCreateTenantSettingsAsync(context, scenario.TenantId);
        return _resolver.Resolve(tenantSettings, scenario);
    }
}
