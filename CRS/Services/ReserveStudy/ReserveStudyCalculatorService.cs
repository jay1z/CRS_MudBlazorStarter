using CRS.Core.ReserveCalculator.Models;
using CRS.Core.ReserveCalculator.Services;
using CRS.Data;
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

    public ReserveStudyCalculatorService(ApplicationDbContext context)
    {
        _context = context;
        _calculator = new ReserveStudyCalculator();
        _resolver = new SettingsResolver();
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

        // Resolve effective settings
        var effective = _resolver.Resolve(tenantSettings, scenario);

        // Build calculator input
        var input = _resolver.BuildCalculatorInput(effective, scenario.Components);

        // Execute pure calculation
        var result = _calculator.Calculate(input);

        return result;
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
