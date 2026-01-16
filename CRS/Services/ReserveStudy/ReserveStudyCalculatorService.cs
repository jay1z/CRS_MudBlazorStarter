using CRS.Core.ReserveCalculator.Models;
using CRS.Core.ReserveCalculator.Services;
using CRS.Data;
using CRS.Models;
using CRS.Models.ReserveStudyCalculator;
using Microsoft.EntityFrameworkCore;

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
    private readonly ApplicationDbContext _context;
    private readonly ReserveStudyCalculator _calculator;
    private readonly SettingsResolver _resolver;
    private readonly ReserveStudyAdapter _adapter;

    public ReserveStudyCalculatorService(ApplicationDbContext context)
    {
        _context = context;
        _calculator = new ReserveStudyCalculator();
        _resolver = new SettingsResolver();
        _adapter = new ReserveStudyAdapter();
    }

    /// <inheritdoc />
    public async Task<ReserveStudyResult> CalculateScenarioAsync(int scenarioId)
    {
        // Load scenario with components
        var scenario = await _context.ReserveStudyScenarios
            .Include(s => s.Components.Where(c => c.DateDeleted == null))
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.DateDeleted == null);

        if (scenario == null)
        {
            return ReserveStudyResult.Failure($"Scenario {scenarioId} not found.");
        }

        // Load tenant settings
        var tenantSettings = await GetOrCreateTenantSettingsAsync(scenario.TenantId);

        // Build calculator input using adapter
        var input = _adapter.BuildInputFromScenario(scenario, tenantSettings);

        // Execute pure calculation
        var result = _calculator.Calculate(input);

        return result;
    }

    /// <inheritdoc />
    public async Task<ReserveStudyResult> CalculateFromStudyAsync(Guid studyId)
    {
        // Load study with all related elements
        var study = await _context.ReserveStudies
            .Include(s => s.FinancialInfo)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.BuildingElement)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.CommonElement)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .Include(s => s.CurrentProposal)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
        {
            return ReserveStudyResult.Failure($"ReserveStudy {studyId} not found.");
        }

        // Load tenant settings
        var tenantSettings = await GetOrCreateTenantSettingsAsync(study.TenantId);

        // Build calculator input using adapter
        var input = _adapter.BuildInputFromStudy(study, tenantSettings);

        // Execute pure calculation
        var result = _calculator.Calculate(input);

        return result;
    }

    /// <inheritdoc />
    public async Task<ReserveStudyScenario?> GetOrCreateScenarioForStudyAsync(Guid studyId, bool createIfMissing = true)
    {
        // Check for existing scenario
        var scenario = await _context.ReserveStudyScenarios
            .Include(s => s.Components.Where(c => c.DateDeleted == null))
            .FirstOrDefaultAsync(s => s.ReserveStudyId == studyId && s.DateDeleted == null);

        if (scenario != null)
            return scenario;

        if (!createIfMissing)
            return null;

        // Load study with elements to create scenario
        var study = await _context.ReserveStudies
            .Include(s => s.FinancialInfo)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.BuildingElement)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.CommonElement)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.UsefulLifeOption)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.RemainingLifeOption)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return null;

        // Create scenario from study using adapter
        scenario = _adapter.CreateScenarioFromStudy(study, "From Proposal");

        _context.ReserveStudyScenarios.Add(scenario);
        await _context.SaveChangesAsync();

        return scenario;
    }

    /// <inheritdoc />
    public async Task<TenantReserveSettings> GetOrCreateTenantSettingsAsync(int tenantId)
    {
        var settings = await _context.TenantReserveSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings == null)
        {
            settings = SettingsResolver.CreateDefaultSettings(tenantId);
            _context.TenantReserveSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    /// <inheritdoc />
    public async Task<TenantReserveSettings> UpdateTenantSettingsAsync(TenantReserveSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.TenantReserveSettings.Update(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    /// <inheritdoc />
    public async Task<EffectiveReserveSettings> GetEffectiveSettingsAsync(int scenarioId)
    {
        var scenario = await _context.ReserveStudyScenarios
            .FirstOrDefaultAsync(s => s.Id == scenarioId && s.DateDeleted == null);

        if (scenario == null)
        {
            throw new InvalidOperationException($"Scenario {scenarioId} not found.");
        }

        var tenantSettings = await GetOrCreateTenantSettingsAsync(scenario.TenantId);
        return _resolver.Resolve(tenantSettings, scenario);
    }
}
