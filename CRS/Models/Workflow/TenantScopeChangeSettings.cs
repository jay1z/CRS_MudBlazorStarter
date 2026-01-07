using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Services.Tenant;

namespace CRS.Models.Workflow;

/// <summary>
/// Tenant-specific settings for handling scope changes after site visits.
/// </summary>
[Table("TenantScopeChangeSettings")]
public class TenantScopeChangeSettings : ITenantScoped
{
    /// <summary>
    /// Primary key - matches TenantId (one settings row per tenant).
    /// </summary>
    [Key]
    public int TenantId { get; set; }

    /// <summary>
    /// How to handle scope changes after proposal acceptance.
    /// </summary>
    public ScopeChangeMode Mode { get; set; } = ScopeChangeMode.VarianceWithAmendment;

    /// <summary>
    /// Variance threshold (%) that triggers action. 0 = always trigger.
    /// Example: 10 means 10% variance triggers action.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal VarianceThresholdPercent { get; set; } = 10m;

    /// <summary>
    /// Variance threshold (absolute count) that triggers action. 
    /// 0 = use percentage only.
    /// Threshold is met if EITHER % or count is exceeded.
    /// </summary>
    public int VarianceThresholdCount { get; set; } = 5;

    /// <summary>
    /// Whether HOA must approve scope changes before proceeding.
    /// </summary>
    public bool RequireHoaApproval { get; set; } = true;

    /// <summary>
    /// Whether to use two-phase proposals (site visit separate from full study).
    /// </summary>
    public bool UseTwoPhaseProposal { get; set; } = false;

    /// <summary>
    /// Whether to automatically notify HOA when variance is detected.
    /// </summary>
    public bool AutoNotifyHoaOnVariance { get; set; } = true;

    /// <summary>
    /// Email template ID for variance notification (if using templates).
    /// </summary>
    public Guid? VarianceNotificationTemplateId { get; set; }

    /// <summary>
    /// Whether staff can override variance requirements and proceed anyway.
    /// </summary>
    public bool AllowStaffOverride { get; set; } = true;

    /// <summary>
    /// When settings were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who last updated these settings.
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}
