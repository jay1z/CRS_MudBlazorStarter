using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;

namespace Horizon.Services {
    // SaaS Refactor: Service for tenant-specific homepage management
    public class TenantHomepageService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenantContext;

        public TenantHomepageService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenantContext) {
            _dbFactory = dbFactory;
            _tenantContext = tenantContext;
        }

        private int RequireTenantId() => _tenantContext.TenantId ?? throw new InvalidOperationException("Tenant context not set.");

        public async Task<TenantHomepage?> GetForCurrentTenantAsync(CancellationToken ct = default) {
            if (!_tenantContext.TenantId.HasValue) return null;
            var tenantId = _tenantContext.TenantId.Value;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.TenantHomepages.AsNoTracking().FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
        }

        // SaaS Refactor: helper to fetch by explicit tenant id for preview/testing (no tenant context required)
        public async Task<TenantHomepage?> GetByTenantIdAsync(int tenantId, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.TenantHomepages.AsNoTracking().FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
        }

        // Save draft with optional original RowVersion used for optimistic concurrency checks
        public async Task<TenantHomepage> SaveDraftAsync(TenantHomepage source, byte[]? originalRowVersion = null, string? modifiedBy = null, CancellationToken ct = default) {
            var tenantId = RequireTenantId();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.TenantHomepages.FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
            if (existing == null) {
                existing = new TenantHomepage {
                    TenantId = tenantId,
                    DraftJson = source.DraftJson,
                    DraftHtml = source.DraftHtml,
                    MetaTitle = source.MetaTitle,
                    MetaDescription = source.MetaDescription,
                    TemplateName = source.TemplateName,
                    IsPublished = false,
                    DateModified = DateTime.UtcNow,
                    ModifiedBy = modifiedBy
                };
                db.TenantHomepages.Add(existing);
            } else {
                // If caller provided an original RowVersion, attach it so EF detects concurrency conflicts
                if (originalRowVersion != null) {
                    db.Entry(existing).Property("RowVersion").OriginalValue = originalRowVersion;
                }

                existing.DraftJson = source.DraftJson;
                existing.DraftHtml = source.DraftHtml;
                existing.MetaTitle = source.MetaTitle;
                existing.MetaDescription = source.MetaDescription;
                existing.TemplateName = source.TemplateName;
                existing.DateModified = DateTime.UtcNow;
                existing.ModifiedBy = modifiedBy;
                db.TenantHomepages.Update(existing);
            }
            try {
                await db.SaveChangesAsync(ct);
            } catch (DbUpdateConcurrencyException ex) {
                // Wrap and surface a domain-level concurrency exception
                throw new TenantHomepageConcurrencyException("The homepage was modified by another user. Reload and try again.", ex);
            }
            return existing;
        }

        // Force-save: fetch latest, overwrite, and set OriginalValue to current RowVersion to avoid conflict
        public async Task<TenantHomepage> SaveDraftForceAsync(TenantHomepage source, string? modifiedBy = null, CancellationToken ct = default) {
            var tenantId = RequireTenantId();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.TenantHomepages.FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
            if (existing == null) {
                existing = new TenantHomepage {
                    TenantId = tenantId,
                    DraftJson = source.DraftJson,
                    DraftHtml = source.DraftHtml,
                    MetaTitle = source.MetaTitle,
                    MetaDescription = source.MetaDescription,
                    TemplateName = source.TemplateName,
                    IsPublished = false,
                    DateModified = DateTime.UtcNow,
                    ModifiedBy = modifiedBy
                };
                db.TenantHomepages.Add(existing);
            } else {
                existing.DraftJson = source.DraftJson;
                existing.DraftHtml = source.DraftHtml;
                existing.MetaTitle = source.MetaTitle;
                existing.MetaDescription = source.MetaDescription;
                existing.TemplateName = source.TemplateName;
                existing.DateModified = DateTime.UtcNow;
                existing.ModifiedBy = modifiedBy;
                // Set OriginalValue to current RowVersion to avoid concurrency exception for an intentional overwrite
                var currentRowVersion = db.Entry(existing).Property("RowVersion").CurrentValue as byte[];
                if (currentRowVersion != null) db.Entry(existing).Property("RowVersion").OriginalValue = currentRowVersion;
                db.TenantHomepages.Update(existing);
            }

            await db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<TenantHomepage?> PublishAsync(byte[]? originalRowVersion = null, CancellationToken ct = default) {
            var tenantId = RequireTenantId();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.TenantHomepages.FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
            if (existing == null) return null;

            // SaaS Refactor: Sanitize DraftHtml before publishing to PublishedHtml
            if (!string.IsNullOrWhiteSpace(existing.DraftHtml)) {
                var sanitizer = new HtmlSanitizer();
                // Allow common formatting tags and attributes; tweak as needed
                sanitizer.AllowDataAttributes = false;
                sanitizer.AllowedAttributes.Add("class");
                sanitizer.AllowedAttributes.Add("src");
                sanitizer.AllowedAttributes.Add("alt");
                sanitizer.AllowedAttributes.Add("href");
                sanitizer.AllowedAttributes.Add("title");
                sanitizer.AllowedAttributes.Add("width");
                sanitizer.AllowedAttributes.Add("height");
                sanitizer.AllowedSchemes.Add("data"); // allow data: URIs if you accept base64 images

                // You can further customize allowed tags/attributes here
                existing.PublishedHtml = sanitizer.Sanitize(existing.DraftHtml);
            } else {
                existing.PublishedHtml = null;
            }

            existing.PublishedJson = existing.DraftJson;
            existing.IsPublished = true;
            existing.DateModified = DateTime.UtcNow;

            if (originalRowVersion != null) {
                db.Entry(existing).Property("RowVersion").OriginalValue = originalRowVersion;
            }

            db.TenantHomepages.Update(existing);
            try {
                await db.SaveChangesAsync(ct);
            } catch (DbUpdateConcurrencyException ex) {
                throw new TenantHomepageConcurrencyException("The homepage was modified by another user. Reload and try again.", ex);
            }
            return existing;
        }

        public async Task<TenantHomepage?> PublishForceAsync(CancellationToken ct = default) {
            var tenantId = RequireTenantId();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.TenantHomepages.FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
            if (existing == null) return null;

            if (!string.IsNullOrWhiteSpace(existing.DraftHtml)) {
                var sanitizer = new HtmlSanitizer();
                sanitizer.AllowDataAttributes = false;
                sanitizer.AllowedAttributes.Add("class");
                sanitizer.AllowedAttributes.Add("src");
                sanitizer.AllowedAttributes.Add("alt");
                sanitizer.AllowedAttributes.Add("href");
                sanitizer.AllowedAttributes.Add("title");
                sanitizer.AllowedAttributes.Add("width");
                sanitizer.AllowedAttributes.Add("height");
                sanitizer.AllowedSchemes.Add("data");
                existing.PublishedHtml = sanitizer.Sanitize(existing.DraftHtml);
            } else {
                existing.PublishedHtml = null;
            }

            existing.PublishedJson = existing.DraftJson;
            existing.IsPublished = true;
            existing.DateModified = DateTime.UtcNow;

            // Force overwrite: set OriginalValue to current RowVersion
            var currentRowVersion = db.Entry(existing).Property("RowVersion").CurrentValue as byte[];
            if (currentRowVersion != null) db.Entry(existing).Property("RowVersion").OriginalValue = currentRowVersion;

            db.TenantHomepages.Update(existing);
            await db.SaveChangesAsync(ct);
            return existing;
        }
    }
}
