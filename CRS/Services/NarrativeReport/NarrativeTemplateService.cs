using CRS.Data;
using CRS.Models.NarrativeTemplates;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Service for managing narrative templates (sections, blocks, and inserts).
/// Supports tenant-specific overrides with global defaults.
/// </summary>
public interface INarrativeTemplateService
{
    // ═══════════════════════════════════════════════════════════════
    // SECTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all sections for a tenant with global fallbacks.
    /// </summary>
    Task<List<NarrativeTemplateSection>> GetSectionsAsync(int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a section by key, with tenant override if available.
    /// </summary>
    Task<NarrativeTemplateSection?> GetSectionAsync(string sectionKey, int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a tenant-specific section override.
    /// </summary>
    Task<NarrativeTemplateSection> SaveSectionAsync(NarrativeTemplateSection section, CancellationToken ct = default);

    /// <summary>
    /// Toggles a section's enabled state.
    /// </summary>
    Task<bool> ToggleSectionAsync(string sectionKey, int tenantId, bool isEnabled, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // BLOCKS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all blocks for a section with tenant overrides.
    /// </summary>
    Task<List<NarrativeTemplateBlock>> GetBlocksAsync(string sectionKey, int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a block by section and block key, with tenant override if available.
    /// </summary>
    Task<NarrativeTemplateBlock?> GetBlockAsync(string sectionKey, string blockKey, int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a tenant-specific block override.
    /// </summary>
    Task<NarrativeTemplateBlock> SaveBlockAsync(NarrativeTemplateBlock block, CancellationToken ct = default);

    /// <summary>
    /// Toggles a block's enabled state.
    /// </summary>
    Task<bool> ToggleBlockAsync(string sectionKey, string blockKey, int tenantId, bool isEnabled, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // INSERTS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets all inserts with tenant overrides.
    /// </summary>
    Task<List<NarrativeInsert>> GetInsertsAsync(int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets an insert by key with tenant override if available.
    /// </summary>
    Task<NarrativeInsert?> GetInsertAsync(string insertKey, int? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a tenant-specific insert.
    /// </summary>
    Task<NarrativeInsert> SaveInsertAsync(NarrativeInsert insert, CancellationToken ct = default);

    /// <summary>
    /// Toggles an insert's enabled state.
    /// </summary>
    Task<bool> ToggleInsertAsync(string insertKey, int tenantId, bool isEnabled, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // UTILITY
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resets a tenant's templates to global defaults by removing overrides.
    /// </summary>
    Task<int> ResetToDefaultsAsync(int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Copies templates from one tenant to another.
    /// </summary>
    Task<int> CopyTemplatesAsync(int sourceTenantId, int targetTenantId, CancellationToken ct = default);
}

/// <summary>
/// Implementation of INarrativeTemplateService.
/// </summary>
public class NarrativeTemplateService : INarrativeTemplateService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NarrativeTemplateService> _logger;

    public NarrativeTemplateService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<NarrativeTemplateService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // SECTIONS
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<NarrativeTemplateSection>> GetSectionsAsync(int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Load global defaults
        var global = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == 0 && s.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return global.OrderBy(s => s.SortOrder).ToList();

        // Load tenant overrides
        var tenant = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == tenantId.Value && s.DateDeleted == null)
            .ToListAsync(ct);

        // Merge
        var result = new Dictionary<string, NarrativeTemplateSection>();
        foreach (var s in global) result[s.SectionKey] = s;
        foreach (var s in tenant) result[s.SectionKey] = s;

        return result.Values.OrderBy(s => s.SortOrder).ToList();
    }

    public async Task<NarrativeTemplateSection?> GetSectionAsync(string sectionKey, int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Try tenant-specific first
        if (tenantId.HasValue && tenantId != 0)
        {
            var tenantSection = await context.NarrativeTemplateSections
                .FirstOrDefaultAsync(s => s.SectionKey == sectionKey && s.TenantId == tenantId.Value && s.DateDeleted == null, ct);
            if (tenantSection != null) return tenantSection;
        }

        // Fall back to global
        return await context.NarrativeTemplateSections
            .FirstOrDefaultAsync(s => s.SectionKey == sectionKey && s.TenantId == 0 && s.DateDeleted == null, ct);
    }

    public async Task<NarrativeTemplateSection> SaveSectionAsync(NarrativeTemplateSection section, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        if (section.Id == Guid.Empty)
        {
            context.NarrativeTemplateSections.Add(section);
        }
        else
        {
            context.NarrativeTemplateSections.Update(section);
        }

        await context.SaveChangesAsync(ct);
        return section;
    }

    public async Task<bool> ToggleSectionAsync(string sectionKey, int tenantId, bool isEnabled, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Find or create tenant override
        var section = await context.NarrativeTemplateSections
            .FirstOrDefaultAsync(s => s.SectionKey == sectionKey && s.TenantId == tenantId && s.DateDeleted == null, ct);

        if (section == null)
        {
            // Copy from global
            var global = await context.NarrativeTemplateSections
                .FirstOrDefaultAsync(s => s.SectionKey == sectionKey && s.TenantId == 0 && s.DateDeleted == null, ct);

            if (global == null) return false;

            section = new NarrativeTemplateSection
            {
                TenantId = tenantId,
                SectionKey = global.SectionKey,
                Title = global.Title,
                IsEnabled = isEnabled,
                SortOrder = global.SortOrder,
                Description = global.Description,
                PageBreakBefore = global.PageBreakBefore,
                CssClass = global.CssClass
            };
            context.NarrativeTemplateSections.Add(section);
        }
        else
        {
            section.IsEnabled = isEnabled;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // BLOCKS
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<NarrativeTemplateBlock>> GetBlocksAsync(string sectionKey, int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Load global defaults
        var global = await context.NarrativeTemplateBlocks
            .Where(b => b.SectionKey == sectionKey && b.TenantId == 0 && b.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return global.OrderBy(b => b.SortOrder).ToList();

        // Load tenant overrides
        var tenant = await context.NarrativeTemplateBlocks
            .Where(b => b.SectionKey == sectionKey && b.TenantId == tenantId.Value && b.DateDeleted == null)
            .ToListAsync(ct);

        // Merge
        var result = new Dictionary<string, NarrativeTemplateBlock>();
        foreach (var b in global) result[b.BlockKey] = b;
        foreach (var b in tenant) result[b.BlockKey] = b;

        return result.Values.OrderBy(b => b.SortOrder).ToList();
    }

    public async Task<NarrativeTemplateBlock?> GetBlockAsync(string sectionKey, string blockKey, int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Try tenant-specific first
        if (tenantId.HasValue && tenantId != 0)
        {
            var tenantBlock = await context.NarrativeTemplateBlocks
                .FirstOrDefaultAsync(b => b.SectionKey == sectionKey && b.BlockKey == blockKey && b.TenantId == tenantId.Value && b.DateDeleted == null, ct);
            if (tenantBlock != null) return tenantBlock;
        }

        // Fall back to global
        return await context.NarrativeTemplateBlocks
            .FirstOrDefaultAsync(b => b.SectionKey == sectionKey && b.BlockKey == blockKey && b.TenantId == 0 && b.DateDeleted == null, ct);
    }

    public async Task<NarrativeTemplateBlock> SaveBlockAsync(NarrativeTemplateBlock block, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        if (block.Id == Guid.Empty)
        {
            context.NarrativeTemplateBlocks.Add(block);
        }
        else
        {
            context.NarrativeTemplateBlocks.Update(block);
        }

        await context.SaveChangesAsync(ct);
        return block;
    }

    public async Task<bool> ToggleBlockAsync(string sectionKey, string blockKey, int tenantId, bool isEnabled, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var block = await context.NarrativeTemplateBlocks
            .FirstOrDefaultAsync(b => b.SectionKey == sectionKey && b.BlockKey == blockKey && b.TenantId == tenantId && b.DateDeleted == null, ct);

        if (block == null)
        {
            // Copy from global
            var global = await context.NarrativeTemplateBlocks
                .FirstOrDefaultAsync(b => b.SectionKey == sectionKey && b.BlockKey == blockKey && b.TenantId == 0 && b.DateDeleted == null, ct);

            if (global == null) return false;

            block = new NarrativeTemplateBlock
            {
                TenantId = tenantId,
                SectionKey = global.SectionKey,
                BlockKey = global.BlockKey,
                Title = global.Title,
                HtmlTemplate = global.HtmlTemplate,
                IsEnabled = isEnabled,
                SortOrder = global.SortOrder,
                AppliesWhenJson = global.AppliesWhenJson,
                Description = global.Description,
                CssClass = global.CssClass
            };
            context.NarrativeTemplateBlocks.Add(block);
        }
        else
        {
            block.IsEnabled = isEnabled;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // INSERTS
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<NarrativeInsert>> GetInsertsAsync(int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var global = await context.NarrativeInserts
            .Where(i => i.TenantId == 0 && i.DateDeleted == null)
            .ToListAsync(ct);

        if (!tenantId.HasValue || tenantId == 0)
            return global.OrderBy(i => i.SortOrder).ToList();

        var tenant = await context.NarrativeInserts
            .Where(i => i.TenantId == tenantId.Value && i.DateDeleted == null)
            .ToListAsync(ct);

        var result = new Dictionary<string, NarrativeInsert>();
        foreach (var i in global) result[i.InsertKey] = i;
        foreach (var i in tenant) result[i.InsertKey] = i;

        return result.Values.OrderBy(i => i.SortOrder).ToList();
    }

    public async Task<NarrativeInsert?> GetInsertAsync(string insertKey, int? tenantId = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        if (tenantId.HasValue && tenantId != 0)
        {
            var tenantInsert = await context.NarrativeInserts
                .FirstOrDefaultAsync(i => i.InsertKey == insertKey && i.TenantId == tenantId.Value && i.DateDeleted == null, ct);
            if (tenantInsert != null) return tenantInsert;
        }

        return await context.NarrativeInserts
            .FirstOrDefaultAsync(i => i.InsertKey == insertKey && i.TenantId == 0 && i.DateDeleted == null, ct);
    }

    public async Task<NarrativeInsert> SaveInsertAsync(NarrativeInsert insert, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        if (insert.Id == Guid.Empty)
        {
            context.NarrativeInserts.Add(insert);
        }
        else
        {
            context.NarrativeInserts.Update(insert);
        }

        await context.SaveChangesAsync(ct);
        return insert;
    }

    public async Task<bool> ToggleInsertAsync(string insertKey, int tenantId, bool isEnabled, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var insert = await context.NarrativeInserts
            .FirstOrDefaultAsync(i => i.InsertKey == insertKey && i.TenantId == tenantId && i.DateDeleted == null, ct);

        if (insert == null)
        {
            var global = await context.NarrativeInserts
                .FirstOrDefaultAsync(i => i.InsertKey == insertKey && i.TenantId == 0 && i.DateDeleted == null, ct);

            if (global == null) return false;

            insert = new NarrativeInsert
            {
                TenantId = tenantId,
                InsertKey = global.InsertKey,
                Title = global.Title,
                HtmlTemplate = global.HtmlTemplate,
                IsEnabled = isEnabled,
                AppliesWhenJson = global.AppliesWhenJson,
                Description = global.Description,
                TargetSectionKey = global.TargetSectionKey,
                InsertAfterBlockKey = global.InsertAfterBlockKey,
                SortOrder = global.SortOrder
            };
            context.NarrativeInserts.Add(insert);
        }
        else
        {
            insert.IsEnabled = isEnabled;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // UTILITY
    // ═══════════════════════════════════════════════════════════════

    public async Task<int> ResetToDefaultsAsync(int tenantId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var count = 0;

        // Soft delete all tenant-specific sections
        var sections = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == tenantId && s.DateDeleted == null)
            .ToListAsync(ct);
        foreach (var s in sections)
        {
            s.DateDeleted = DateTime.UtcNow;
            count++;
        }

        // Soft delete all tenant-specific blocks
        var blocks = await context.NarrativeTemplateBlocks
            .Where(b => b.TenantId == tenantId && b.DateDeleted == null)
            .ToListAsync(ct);
        foreach (var b in blocks)
        {
            b.DateDeleted = DateTime.UtcNow;
            count++;
        }

        // Soft delete all tenant-specific inserts
        var inserts = await context.NarrativeInserts
            .Where(i => i.TenantId == tenantId && i.DateDeleted == null)
            .ToListAsync(ct);
        foreach (var i in inserts)
        {
            i.DateDeleted = DateTime.UtcNow;
            count++;
        }

        await context.SaveChangesAsync(ct);
        return count;
    }

    public async Task<int> CopyTemplatesAsync(int sourceTenantId, int targetTenantId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var count = 0;

        // Copy sections
        var sections = await context.NarrativeTemplateSections
            .Where(s => s.TenantId == sourceTenantId && s.DateDeleted == null)
            .ToListAsync(ct);

        foreach (var s in sections)
        {
            var copy = new NarrativeTemplateSection
            {
                TenantId = targetTenantId,
                SectionKey = s.SectionKey,
                Title = s.Title,
                IsEnabled = s.IsEnabled,
                SortOrder = s.SortOrder,
                Description = s.Description,
                PageBreakBefore = s.PageBreakBefore,
                CssClass = s.CssClass
            };
            context.NarrativeTemplateSections.Add(copy);
            count++;
        }

        // Copy blocks
        var blocks = await context.NarrativeTemplateBlocks
            .Where(b => b.TenantId == sourceTenantId && b.DateDeleted == null)
            .ToListAsync(ct);

        foreach (var b in blocks)
        {
            var copy = new NarrativeTemplateBlock
            {
                TenantId = targetTenantId,
                SectionKey = b.SectionKey,
                BlockKey = b.BlockKey,
                Title = b.Title,
                HtmlTemplate = b.HtmlTemplate,
                IsEnabled = b.IsEnabled,
                SortOrder = b.SortOrder,
                AppliesWhenJson = b.AppliesWhenJson,
                Description = b.Description,
                CssClass = b.CssClass
            };
            context.NarrativeTemplateBlocks.Add(copy);
            count++;
        }

        // Copy inserts
        var inserts = await context.NarrativeInserts
            .Where(i => i.TenantId == sourceTenantId && i.DateDeleted == null)
            .ToListAsync(ct);

        foreach (var i in inserts)
        {
            var copy = new NarrativeInsert
            {
                TenantId = targetTenantId,
                InsertKey = i.InsertKey,
                Title = i.Title,
                HtmlTemplate = i.HtmlTemplate,
                IsEnabled = i.IsEnabled,
                AppliesWhenJson = i.AppliesWhenJson,
                Description = i.Description,
                TargetSectionKey = i.TargetSectionKey,
                InsertAfterBlockKey = i.InsertAfterBlockKey,
                SortOrder = i.SortOrder
            };
            context.NarrativeInserts.Add(copy);
            count++;
        }

        await context.SaveChangesAsync(ct);
        return count;
    }
}
