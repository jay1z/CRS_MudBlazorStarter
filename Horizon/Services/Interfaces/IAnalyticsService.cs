using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for tenant business analytics and reporting.
/// Provides metrics on studies, revenue, and operational performance.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Gets comprehensive analytics for a tenant within a date range.
    /// </summary>
    Task<TenantAnalytics> GetTenantAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets study-specific metrics for a tenant.
    /// </summary>
    Task<StudyMetrics> GetStudyMetricsAsync(int tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets revenue and billing metrics for a tenant.
    /// </summary>
    Task<RevenueMetrics> GetRevenueMetricsAsync(int tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets specialist/staff productivity metrics.
    /// </summary>
    Task<List<SpecialistMetric>> GetSpecialistMetricsAsync(int tenantId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets monthly trend data for charting.
    /// </summary>
    Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(int tenantId, int monthsBack = 12);

    /// <summary>
    /// Gets element popularity statistics.
    /// </summary>
    Task<List<ElementPopularity>> GetElementPopularityAsync(int tenantId, int topCount = 20);
}

/// <summary>
/// Comprehensive analytics data for a tenant.
/// </summary>
public class TenantAnalytics
{
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Study metrics
    public StudyMetrics Studies { get; set; } = new();

    // Revenue metrics
    public RevenueMetrics Revenue { get; set; } = new();

    // Client metrics
    public ClientMetrics Clients { get; set; } = new();

    // Operational metrics
    public OperationalMetrics Operations { get; set; } = new();
}

/// <summary>
/// Study-related metrics.
/// </summary>
public class StudyMetrics
{
    // Counts
    public int TotalStudies { get; set; }
    public int StudiesCreated { get; set; }
    public int StudiesCompleted { get; set; }
    public int StudiesInProgress { get; set; }
    public int StudiesCancelled { get; set; }

    // Comparison to previous period
    public int StudiesCreatedPreviousPeriod { get; set; }
    public int StudiesCompletedPreviousPeriod { get; set; }

    // Calculated growth
    public decimal CreatedGrowthPercent => StudiesCreatedPreviousPeriod > 0
        ? ((decimal)(StudiesCreated - StudiesCreatedPreviousPeriod) / StudiesCreatedPreviousPeriod) * 100
        : StudiesCreated > 0 ? 100 : 0;

    public decimal CompletedGrowthPercent => StudiesCompletedPreviousPeriod > 0
        ? ((decimal)(StudiesCompleted - StudiesCompletedPreviousPeriod) / StudiesCompletedPreviousPeriod) * 100
        : StudiesCompleted > 0 ? 100 : 0;

    // Timing metrics
    public double AverageCompletionDays { get; set; }
    public double MedianCompletionDays { get; set; }
    public int FastestCompletionDays { get; set; }
    public int SlowestCompletionDays { get; set; }

    // Workflow stage breakdown
    public Dictionary<string, int> StudiesByStatus { get; set; } = new();
}

/// <summary>
/// Revenue and billing metrics.
/// </summary>
public class RevenueMetrics
{
    // Invoice totals
    public decimal TotalInvoiced { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }

    // Invoice counts
    public int InvoicesSent { get; set; }
    public int InvoicesPaid { get; set; }
    public int InvoicesOverdue { get; set; }

    // Comparison to previous period
    public decimal CollectedPreviousPeriod { get; set; }
    public decimal InvoicedPreviousPeriod { get; set; }

    // Calculated growth
    public decimal CollectionGrowthPercent => CollectedPreviousPeriod > 0
        ? ((TotalCollected - CollectedPreviousPeriod) / CollectedPreviousPeriod) * 100
        : TotalCollected > 0 ? 100 : 0;

    // Averages
    public decimal AverageInvoiceAmount { get; set; }
    public decimal AverageStudyValue { get; set; }
    public double AverageDaysToPayment { get; set; }

    // Collection rate
    public decimal CollectionRate => TotalInvoiced > 0 
        ? (TotalCollected / TotalInvoiced) * 100 
        : 0;
}

/// <summary>
/// Client/community metrics.
/// </summary>
public class ClientMetrics
{
    public int TotalCommunities { get; set; }
    public int NewCommunitiesThisPeriod { get; set; }
    public int ActiveCustomerAccounts { get; set; }
    public int RepeatClients { get; set; }

    // Retention
    public decimal RetentionRate { get; set; }
    public int CommunitiesWithMultipleStudies { get; set; }
}

/// <summary>
/// Operational efficiency metrics.
/// </summary>
public class OperationalMetrics
{
    public int TotalSpecialists { get; set; }
    public int ActiveSpecialists { get; set; }
    public double AverageStudiesPerSpecialist { get; set; }
    public int SiteVisitsCompleted { get; set; }
    public int ReportsGenerated { get; set; }
    public int ProposalsSent { get; set; }
    public int ProposalsAccepted { get; set; }
    public decimal ProposalAcceptanceRate => ProposalsSent > 0 
        ? ((decimal)ProposalsAccepted / ProposalsSent) * 100 
        : 0;
}

/// <summary>
/// Per-specialist productivity metrics.
/// </summary>
public class SpecialistMetric
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int StudiesAssigned { get; set; }
    public int StudiesCompleted { get; set; }
    public int SiteVisitsCompleted { get; set; }
    public double AverageCompletionDays { get; set; }
    public decimal RevenueGenerated { get; set; }
}

/// <summary>
/// Monthly trend data point for charts.
/// </summary>
public class MonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string Label => $"{MonthName} {Year}";
    
    public int StudiesCreated { get; set; }
    public int StudiesCompleted { get; set; }
    public decimal Revenue { get; set; }
    public int NewCommunities { get; set; }
}

/// <summary>
/// Element popularity data for analysis.
/// </summary>
public class ElementPopularity
{
    public Guid ElementId { get; set; }
    public string ElementName { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty; // "Building" or "Common"
    public int UsageCount { get; set; }
    public decimal AverageReplacementCost { get; set; }
    public decimal TotalReplacementCost { get; set; }
}
