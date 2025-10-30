using System;
using System.ComponentModel.DataAnnotations;

using CRS.Services.Tenant;

namespace CRS.Models {
    // Block-model homepage per tenant — MVP scaffolding
    public class TenantHomepage : ITenantScoped {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Tenant scoping
        public int TenantId { get; set; }

        // Optional template name for selecting layout
        public string? TemplateName { get; set; }

        // Draft content (JSON block model)
        public string? DraftJson { get; set; }

        // Published content (JSON block model)
        public string? PublishedJson { get; set; }

        // Optional cached HTML (sanitized) for preview and fast serving
        public string? DraftHtml { get; set; }
        public string? PublishedHtml { get; set; }

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public bool IsPublished { get; set; }

        public DateTime DateModified { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
    }
}
