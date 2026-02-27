using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services {
    /// <summary>
    /// Service for managing element dependencies and validating relationships.
    /// Example: A Basin requires a Road - if Basin is added without Road, a warning/error is shown.
    /// </summary>
    public class ElementDependencyService : IElementDependencyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ElementDependencyService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }

        /// <inheritdoc/>
        public async Task<List<ElementDependency>> GetAllDependenciesAsync() {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ElementDependencies
                .AsNoTracking()
                .Where(d => d.IsActive && d.DateDeleted == null)
                .OrderBy(d => d.DependentElementType)
                .ThenBy(d => d.DependentElementId)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ElementDependency>> GetDependenciesForElementAsync(Guid elementId, ElementType elementType) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ElementDependencies
                .AsNoTracking()
                .Where(d => d.DependentElementId == elementId 
                         && d.DependentElementType == elementType 
                         && d.IsActive 
                         && d.DateDeleted == null)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ElementDependency>> GetDependentsOfElementAsync(Guid elementId, ElementType elementType) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ElementDependencies
                .AsNoTracking()
                .Where(d => d.RequiredElementId == elementId 
                         && d.RequiredElementType == elementType 
                         && d.IsActive 
                         && d.DateDeleted == null)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ElementDependency> CreateDependencyAsync(
            Guid dependentElementId,
            ElementType dependentElementType,
            Guid requiredElementId,
            ElementType requiredElementType,
            string? description = null,
            bool isHardDependency = false) {
            
            await using var context = await _dbFactory.CreateDbContextAsync();
            
            // Check if this dependency already exists
            var existing = await context.ElementDependencies
                .FirstOrDefaultAsync(d => 
                    d.DependentElementId == dependentElementId &&
                    d.DependentElementType == dependentElementType &&
                    d.RequiredElementId == requiredElementId &&
                    d.RequiredElementType == requiredElementType &&
                    d.DateDeleted == null);

            if (existing != null) {
                // Reactivate if it was deactivated
                existing.IsActive = true;
                existing.Description = description;
                existing.IsHardDependency = isHardDependency;
                existing.DateModified = DateTime.Now;
                await context.SaveChangesAsync();
                return existing;
            }

            var dependency = new ElementDependency {
                DependentElementId = dependentElementId,
                DependentElementType = dependentElementType,
                RequiredElementId = requiredElementId,
                RequiredElementType = requiredElementType,
                Description = description,
                IsHardDependency = isHardDependency,
                IsActive = true
            };

            context.ElementDependencies.Add(dependency);
            await context.SaveChangesAsync();
            return dependency;
        }

        /// <inheritdoc/>
        public async Task<ElementDependency> UpdateDependencyAsync(Guid dependencyId, string? description, bool isHardDependency, bool isActive) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            
            var dependency = await context.ElementDependencies
                .FirstOrDefaultAsync(d => d.Id == dependencyId && d.DateDeleted == null)
                ?? throw new InvalidOperationException($"Dependency with ID {dependencyId} not found");

            dependency.Description = description;
            dependency.IsHardDependency = isHardDependency;
            dependency.IsActive = isActive;
            dependency.DateModified = DateTime.Now;

            await context.SaveChangesAsync();
            return dependency;
        }

        /// <inheritdoc/>
        public async Task DeleteDependencyAsync(Guid dependencyId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            
            var dependency = await context.ElementDependencies
                .FirstOrDefaultAsync(d => d.Id == dependencyId && d.DateDeleted == null)
                ?? throw new InvalidOperationException($"Dependency with ID {dependencyId} not found");

            dependency.DateDeleted = DateTime.Now;
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<ElementDependencyValidationResult> ValidateElementDependenciesAsync(
            Guid elementId,
            ElementType elementType,
            IEnumerable<Guid> existingBuildingElementIds,
            IEnumerable<Guid> existingCommonElementIds) {
            
            await using var context = await _dbFactory.CreateDbContextAsync();
            
            var elementName = await GetElementNameAsync(elementId, elementType);
            var result = new ElementDependencyValidationResult {
                ElementId = elementId,
                ElementType = elementType,
                ElementName = elementName
            };

            // Get dependencies for this element
            var dependencies = await context.ElementDependencies
                .AsNoTracking()
                .Where(d => d.DependentElementId == elementId 
                         && d.DependentElementType == elementType 
                         && d.IsActive 
                         && d.DateDeleted == null)
                .ToListAsync();

            var buildingIds = existingBuildingElementIds.ToHashSet();
            var commonIds = existingCommonElementIds.ToHashSet();

            foreach (var dependency in dependencies) {
                bool isMissing = dependency.RequiredElementType switch {
                    ElementType.Building => !buildingIds.Contains(dependency.RequiredElementId),
                    ElementType.Common => !commonIds.Contains(dependency.RequiredElementId),
                    _ => false
                };

                if (isMissing) {
                    var requiredName = await GetElementNameAsync(dependency.RequiredElementId, dependency.RequiredElementType);
                    result.MissingDependencies.Add(new MissingDependency {
                        RequiredElementId = dependency.RequiredElementId,
                        RequiredElementType = dependency.RequiredElementType,
                        RequiredElementName = requiredName,
                        IsHardDependency = dependency.IsHardDependency,
                        Description = dependency.Description
                    });
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<ElementDependencyValidationResult>> ValidateStudyElementsAsync(
            IEnumerable<Guid> buildingElementIds,
            IEnumerable<Guid> commonElementIds) {
            
            await using var context = await _dbFactory.CreateDbContextAsync();
            var results = new List<ElementDependencyValidationResult>();

            var buildingIds = buildingElementIds.ToList();
            var commonIds = commonElementIds.ToList();

            // Get all active dependencies
            var allDependencies = await context.ElementDependencies
                .AsNoTracking()
                .Where(d => d.IsActive && d.DateDeleted == null)
                .ToListAsync();

            // Check building elements
            foreach (var elementId in buildingIds) {
                var result = await ValidateElementDependenciesAsync(
                    elementId, 
                    ElementType.Building, 
                    buildingIds, 
                    commonIds);
                
                if (result.MissingDependencies.Any()) {
                    results.Add(result);
                }
            }

            // Check common elements
            foreach (var elementId in commonIds) {
                var result = await ValidateElementDependenciesAsync(
                    elementId, 
                    ElementType.Common, 
                    buildingIds, 
                    commonIds);
                
                if (result.MissingDependencies.Any()) {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <inheritdoc/>
        public async Task<string> GetElementNameAsync(Guid elementId, ElementType elementType) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            
            if (elementType == ElementType.Building) {
                var element = await context.BuildingElements
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == elementId);
                return element?.Name ?? "Unknown Element";
            } else {
                var element = await context.CommonElements
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == elementId);
                return element?.Name ?? "Unknown Element";
            }
        }
    }
}
