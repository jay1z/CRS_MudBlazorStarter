using CRS.Data;
using CRS.Models;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;

namespace CRS.Services {
    // SaaS Refactor: Service for tenant-specific homepage management
    public class TenantHomepageService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenantContext;

        public TenantHomepageService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenantContext) {
            _dbFactory = dbFactory;
            _tenantContext = tenantContext;
        }

        private int GetTenantId() => _tenantContext.TenantId ?? 1;

        public async Task<TenantHomepage?> GetForCurrentTenantAsync(CancellationToken ct = default) {
            var tenantId = GetTenantId();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.TenantHomepages.AsNoTracking().FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
        }

        // SaaS Refactor: helper to fetch by explicit tenant id for preview/testing (no tenant context required)
        public async Task<TenantHomepage?> GetByTenantIdAsync(int tenantId, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.TenantHomepages.AsNoTracking().FirstOrDefaultAsync(h => h.TenantId == tenantId, ct);
        }

        public async Task<TenantHomepage> SaveDraftAsync(TenantHomepage source, string? modifiedBy = null, CancellationToken ct = default) {
            var tenantId = GetTenantId();
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
                db.TenantHomepages.Update(existing);
            }
            await db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<TenantHomepage?> PublishAsync(CancellationToken ct = default) {
            var tenantId = GetTenantId();
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
            db.TenantHomepages.Update(existing);
            await db.SaveChangesAsync(ct);
            return existing;
        }
    }
}
