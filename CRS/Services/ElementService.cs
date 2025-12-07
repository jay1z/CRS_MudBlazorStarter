using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    /// <summary>
    /// Service for retrieving building and common elements with tenant-specific ordering and visibility.
    /// </summary>
    public class ElementService : IElementService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenantContext;

        public ElementService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenantContext) {
            _dbFactory = dbFactory;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Gets building elements for the current tenant with proper ordering.
        /// Includes global elements (TenantId = null) and tenant-specific elements.
        /// Applies tenant-specific ordering overrides if they exist.
        /// </summary>
        public async Task<List<BuildingElement>> GetBuildingElementsAsync() {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            // Get all applicable elements: global (TenantId = null) OR tenant-specific
            var elementsQuery = context.BuildingElements
                .IgnoreQueryFilters() // Bypass tenant filter since we want global + tenant elements
                .AsNoTracking()
                .Where(be => be.IsActive && (be.TenantId == null || be.TenantId == tenantId));

            // Get tenant-specific ordering overrides
            var orderingOverrides = tenantId.HasValue
                ? await context.TenantElementOrders
                    .AsNoTracking()
                    .Where(o => o.TenantId == tenantId && o.ElementType == ElementType.Building)
                    .ToDictionaryAsync(o => o.ElementId, o => o)
                : new Dictionary<Guid, TenantElementOrder>();

            var elements = await elementsQuery.ToListAsync();

            // Apply ordering and filtering
            var result = elements
                .Where(e => {
                    // If there's an override for this element, check if it's hidden
                    if (orderingOverrides.TryGetValue(e.Id, out var order)) {
                        return !order.IsHidden;
                    }
                    return true; // No override means element is visible
                })
                .OrderBy(e => {
                    // Use tenant-specific ZOrder if available, otherwise use element's default ZOrder
                    if (orderingOverrides.TryGetValue(e.Id, out var order)) {
                        return order.ZOrder;
                    }
                    return e.ZOrder;
                })
                .ThenBy(e => e.Name) // Secondary sort by name for elements with same ZOrder
                .ToList();

            return result;
        }

        /// <summary>
        /// Gets common elements for the current tenant with proper ordering.
        /// Includes global elements (TenantId = null) and tenant-specific elements.
        /// Applies tenant-specific ordering overrides if they exist.
        /// </summary>
        public async Task<List<CommonElement>> GetCommonElementsAsync() {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            // Get all applicable elements: global (TenantId = null) OR tenant-specific
            var elementsQuery = context.CommonElements
                .IgnoreQueryFilters() // Bypass tenant filter since we want global + tenant elements
                .AsNoTracking()
                .Where(ce => ce.IsActive && (ce.TenantId == null || ce.TenantId == tenantId));

            // Get tenant-specific ordering overrides
            var orderingOverrides = tenantId.HasValue
                ? await context.TenantElementOrders
                    .AsNoTracking()
                    .Where(o => o.TenantId == tenantId && o.ElementType == ElementType.Common)
                    .ToDictionaryAsync(o => o.ElementId, o => o)
                : new Dictionary<Guid, TenantElementOrder>();

            var elements = await elementsQuery.ToListAsync();

            // Apply ordering and filtering
            var result = elements
                .Where(e => {
                    // If there's an override for this element, check if it's hidden
                    if (orderingOverrides.TryGetValue(e.Id, out var order)) {
                        return !order.IsHidden;
                    }
                    return true; // No override means element is visible
                })
                .OrderBy(e => {
                    // Use tenant-specific ZOrder if available, otherwise use element's default ZOrder
                    if (orderingOverrides.TryGetValue(e.Id, out var order)) {
                        return order.ZOrder;
                    }
                    return e.ZOrder;
                })
                .ThenBy(e => e.Name) // Secondary sort by name for elements with same ZOrder
                .ToList();

            return result;
        }

        /// <summary>
        /// Gets the display name for an element, considering tenant-specific custom names.
        /// </summary>
        public async Task<string> GetElementDisplayNameAsync(Guid elementId, ElementType elementType) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            if (tenantId.HasValue) {
                var order = await context.TenantElementOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.ElementType == elementType && o.ElementId == elementId);

                if (order != null && !string.IsNullOrWhiteSpace(order.CustomName)) {
                    return order.CustomName;
                }
            }

            // Fall back to the element's original name
            if (elementType == ElementType.Building) {
                var element = await context.BuildingElements
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == elementId);
                return element?.Name ?? string.Empty;
            } else {
                var element = await context.CommonElements
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == elementId);
                return element?.Name ?? string.Empty;
            }
        }

        /// <summary>
        /// Saves a tenant-specific element ordering override.
        /// </summary>
        public async Task SaveElementOrderAsync(Guid elementId, ElementType elementType, int zOrder, bool isHidden = false, string? customName = null) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            if (!tenantId.HasValue) {
                throw new InvalidOperationException("Tenant context is required to save element order.");
            }

            var existing = await context.TenantElementOrders
                .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.ElementType == elementType && o.ElementId == elementId);

            if (existing != null) {
                existing.ZOrder = zOrder;
                existing.IsHidden = isHidden;
                existing.CustomName = customName;
                existing.DateModified = DateTime.UtcNow;
            } else {
                context.TenantElementOrders.Add(new TenantElementOrder {
                    TenantId = tenantId.Value,
                    ElementType = elementType,
                    ElementId = elementId,
                    ZOrder = zOrder,
                    IsHidden = isHidden,
                    CustomName = customName
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a new tenant-specific building element.
        /// </summary>
        public async Task<BuildingElement> CreateTenantBuildingElementAsync(string name, bool needsService, int zOrder) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            if (!tenantId.HasValue) {
                throw new InvalidOperationException("Tenant context is required to create tenant-specific elements.");
            }

            var element = new BuildingElement {
                Name = name,
                NeedsService = needsService,
                IsActive = true,
                ZOrder = zOrder,
                TenantId = tenantId.Value
            };

            context.BuildingElements.Add(element);
            await context.SaveChangesAsync();

            return element;
        }

        /// <summary>
        /// Creates a new tenant-specific common element.
        /// </summary>
        public async Task<CommonElement> CreateTenantCommonElementAsync(string name, bool needsService, int zOrder) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenantId = _tenantContext.TenantId;

            if (!tenantId.HasValue) {
                throw new InvalidOperationException("Tenant context is required to create tenant-specific elements.");
            }

            var element = new CommonElement {
                Name = name,
                NeedsService = needsService,
                IsActive = true,
                ZOrder = zOrder,
                TenantId = tenantId.Value
            };

            context.CommonElements.Add(element);
            await context.SaveChangesAsync();

            return element;
        }
    }
}
