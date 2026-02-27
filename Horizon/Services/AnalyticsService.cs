using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Workflow;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Implementation of tenant business analytics service.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<AnalyticsService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<TenantAnalytics> GetTenantAnalyticsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        var analytics = new TenantAnalytics
        {
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTime.UtcNow
        };

        // Run queries in parallel for better performance
        var studiesTask = GetStudyMetricsAsync(tenantId, startDate, endDate);
        var revenueTask = GetRevenueMetricsAsync(tenantId, startDate, endDate);
        var clientsTask = GetClientMetricsAsync(tenantId, startDate, endDate);
        var operationsTask = GetOperationalMetricsAsync(tenantId, startDate, endDate);

        await Task.WhenAll(studiesTask, revenueTask, clientsTask, operationsTask);

        analytics.Studies = await studiesTask;
        analytics.Revenue = await revenueTask;
        analytics.Clients = await clientsTask;
        analytics.Operations = await operationsTask;

        return analytics;
    }

    public async Task<StudyMetrics> GetStudyMetricsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var metrics = new StudyMetrics();

        // Calculate previous period for comparison
        var periodLength = endDate - startDate;
        var previousStart = startDate - periodLength;
        var previousEnd = startDate;

        // Get all studies for the tenant (ignoring tenant filter since we're specifying tenant)
        var studies = await context.ReserveStudies
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.DateDeleted == null)
            .Select(s => new
            {
                s.Id,
                s.DateCreated,
                s.DateApproved,
                s.IsComplete,
                s.IsActive,
                CurrentStatus = s.StudyRequest != null ? s.StudyRequest.CurrentStatus : StudyStatus.RequestCreated
            })
            .ToListAsync();

        // Current period metrics
        metrics.TotalStudies = studies.Count;
        metrics.StudiesCreated = studies.Count(s => s.DateCreated >= startDate && s.DateCreated <= endDate);
        metrics.StudiesCompleted = studies.Count(s => s.IsComplete && s.DateApproved >= startDate && s.DateApproved <= endDate);
        metrics.StudiesInProgress = studies.Count(s => s.IsActive && !s.IsComplete);
        metrics.StudiesCancelled = studies.Count(s => 
            s.CurrentStatus == StudyStatus.RequestCancelled && 
            s.DateCreated >= startDate && s.DateCreated <= endDate);

        // Previous period for comparison
        metrics.StudiesCreatedPreviousPeriod = studies.Count(s => s.DateCreated >= previousStart && s.DateCreated < previousEnd);
        metrics.StudiesCompletedPreviousPeriod = studies.Count(s => s.IsComplete && s.DateApproved >= previousStart && s.DateApproved < previousEnd);

        // Calculate completion time for completed studies
        var completedStudies = studies
            .Where(s => s.IsComplete && s.DateApproved.HasValue && s.DateCreated.HasValue)
            .Select(s => (s.DateApproved!.Value - s.DateCreated!.Value).TotalDays)
            .OrderBy(d => d)
            .ToList();

        if (completedStudies.Count != 0)
        {
            metrics.AverageCompletionDays = completedStudies.Average();
            metrics.MedianCompletionDays = completedStudies[completedStudies.Count / 2];
            metrics.FastestCompletionDays = (int)completedStudies.Min();
            metrics.SlowestCompletionDays = (int)completedStudies.Max();
        }

        // Studies by workflow status
        metrics.StudiesByStatus = studies
            .GroupBy(s => s.CurrentStatus.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return metrics;
    }

    public async Task<RevenueMetrics> GetRevenueMetricsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var metrics = new RevenueMetrics();

        // Calculate previous period
        var periodLength = endDate - startDate;
        var previousStart = startDate - periodLength;
        var previousEnd = startDate;

        // Get invoices for the tenant
        var invoices = await context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId && i.DateDeleted == null)
            .Select(i => new
            {
                i.Id,
                i.Status,
                i.TotalAmount,
                i.AmountPaid,
                i.InvoiceDate,
                i.DueDate,
                i.PaidAt,
                i.SentAt
            })
            .ToListAsync();

        // Current period invoices
        var periodInvoices = invoices.Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate).ToList();
        var previousPeriodInvoices = invoices.Where(i => i.InvoiceDate >= previousStart && i.InvoiceDate < previousEnd).ToList();

        metrics.TotalInvoiced = periodInvoices.Sum(i => i.TotalAmount);
        metrics.TotalCollected = periodInvoices.Sum(i => i.AmountPaid);
        metrics.TotalOutstanding = periodInvoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Voided)
            .Sum(i => i.TotalAmount - i.AmountPaid);
        metrics.TotalOverdue = periodInvoices
            .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Voided && i.DueDate < DateTime.UtcNow)
            .Sum(i => i.TotalAmount - i.AmountPaid);

        metrics.InvoicesSent = periodInvoices.Count(i => i.SentAt.HasValue);
        metrics.InvoicesPaid = periodInvoices.Count(i => i.Status == InvoiceStatus.Paid);
        metrics.InvoicesOverdue = periodInvoices.Count(i => 
            i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Voided && i.DueDate < DateTime.UtcNow);

        // Previous period comparison
        metrics.InvoicedPreviousPeriod = previousPeriodInvoices.Sum(i => i.TotalAmount);
        metrics.CollectedPreviousPeriod = previousPeriodInvoices.Sum(i => i.AmountPaid);

        // Averages
        if (periodInvoices.Count != 0)
        {
            metrics.AverageInvoiceAmount = periodInvoices.Average(i => i.TotalAmount);
        }

        // Average days to payment
        var paidInvoices = periodInvoices
            .Where(i => i.PaidAt.HasValue && i.SentAt.HasValue)
            .Select(i => (i.PaidAt!.Value - i.SentAt!.Value).TotalDays)
            .ToList();

        if (paidInvoices.Count != 0)
        {
            metrics.AverageDaysToPayment = paidInvoices.Average();
        }

        return metrics;
    }

    private async Task<ClientMetrics> GetClientMetricsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var metrics = new ClientMetrics();

        // Communities
        var communities = await context.Communities
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.DateDeleted == null)
            .Select(c => new { c.Id, c.DateCreated })
            .ToListAsync();

        metrics.TotalCommunities = communities.Count;
        metrics.NewCommunitiesThisPeriod = communities.Count(c => c.DateCreated >= startDate && c.DateCreated <= endDate);

        // Customer accounts
        metrics.ActiveCustomerAccounts = await context.CustomerAccounts
            .IgnoreQueryFilters()
            .CountAsync(ca => ca.TenantId == tenantId && ca.IsActive && ca.DateDeleted == null);

        // Repeat clients (communities with more than one study)
        var communityStudyCounts = await context.ReserveStudies
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.DateDeleted == null && s.CommunityId != null)
            .GroupBy(s => s.CommunityId)
            .Select(g => new { CommunityId = g.Key, Count = g.Count() })
            .ToListAsync();

        metrics.CommunitiesWithMultipleStudies = communityStudyCounts.Count(c => c.Count > 1);
        metrics.RepeatClients = metrics.CommunitiesWithMultipleStudies;

        // Retention rate (communities that came back for another study)
        if (metrics.TotalCommunities > 0)
        {
            metrics.RetentionRate = ((decimal)metrics.CommunitiesWithMultipleStudies / metrics.TotalCommunities) * 100;
        }

        return metrics;
    }

    private async Task<OperationalMetrics> GetOperationalMetricsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        var metrics = new OperationalMetrics();

        // Specialists (users with TenantSpecialist role)
        var specialistUserIds = await context.UserRoleAssignments
            .Where(ura => ura.TenantId == tenantId && ura.Role.Name == "TenantSpecialist")
            .Select(ura => ura.UserId)
            .Distinct()
            .ToListAsync();

        metrics.TotalSpecialists = specialistUserIds.Count;

        // Active specialists (those with studies assigned in the period)
        var activeSpecialistIds = await context.ReserveStudies
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && 
                        s.SpecialistUserId != null && 
                        s.DateCreated >= startDate && 
                        s.DateCreated <= endDate)
            .Select(s => s.SpecialistUserId)
            .Distinct()
            .ToListAsync();

        metrics.ActiveSpecialists = activeSpecialistIds.Count;

        // Average studies per specialist
        if (metrics.ActiveSpecialists > 0)
        {
            var studiesInPeriod = await context.ReserveStudies
                .IgnoreQueryFilters()
                .CountAsync(s => s.TenantId == tenantId && 
                                 s.DateCreated >= startDate && 
                                 s.DateCreated <= endDate);
            metrics.AverageStudiesPerSpecialist = (double)studiesInPeriod / metrics.ActiveSpecialists;
        }

        // Site visits completed (studies that have a site visit date in the period)
        metrics.SiteVisitsCompleted = await context.ReserveStudies
            .IgnoreQueryFilters()
            .CountAsync(s => s.TenantId == tenantId && 
                            s.SiteVisitDate >= startDate && 
                            s.SiteVisitDate <= endDate);

        // Reports generated
        metrics.ReportsGenerated = await context.GeneratedReports
            .IgnoreQueryFilters()
            .CountAsync(r => r.TenantId == tenantId && 
                            r.GeneratedAt >= startDate && 
                            r.GeneratedAt <= endDate);

        // Proposals
        var proposals = await context.Proposals
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && 
                       p.DateDeleted == null &&
                       p.ProposalDate >= startDate && 
                       p.ProposalDate <= endDate)
            .Select(p => new { p.DateSent, p.IsApproved })
            .ToListAsync();

        metrics.ProposalsSent = proposals.Count(p => p.DateSent.HasValue);
        metrics.ProposalsAccepted = proposals.Count(p => p.IsApproved);

        return metrics;
    }

    public async Task<List<SpecialistMetric>> GetSpecialistMetricsAsync(int tenantId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        // Get specialists for this tenant
        var specialists = await context.UserRoleAssignments
            .Where(ura => ura.TenantId == tenantId && ura.Role.Name == "TenantSpecialist")
            .Select(ura => new { ura.UserId, ura.User.Email, Name = ura.User.FullName ?? ura.User.Email })
            .Distinct()
            .ToListAsync();

        var metrics = new List<SpecialistMetric>();

        foreach (var specialist in specialists)
        {
            var studies = await context.ReserveStudies
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && 
                           s.SpecialistUserId == specialist.UserId &&
                           s.DateDeleted == null)
                .Select(s => new
                {
                    s.IsComplete,
                    s.DateCreated,
                    s.DateApproved,
                    s.SiteVisitDate
                })
                .ToListAsync();

            var periodStudies = studies.Where(s => s.DateCreated >= startDate && s.DateCreated <= endDate).ToList();
            var completedStudies = periodStudies.Where(s => s.IsComplete && s.DateApproved.HasValue).ToList();

            // Calculate revenue from completed studies
            var completedStudyIds = await context.ReserveStudies
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && 
                           s.SpecialistUserId == specialist.UserId &&
                           s.IsComplete &&
                           s.DateApproved >= startDate &&
                           s.DateApproved <= endDate)
                .Select(s => s.Id)
                .ToListAsync();

            var revenue = await context.Invoices
                .IgnoreQueryFilters()
                .Where(i => completedStudyIds.Contains(i.ReserveStudyId) && i.Status == InvoiceStatus.Paid)
                .SumAsync(i => i.AmountPaid);

            var metric = new SpecialistMetric
            {
                UserId = specialist.UserId,
                Name = specialist.Name ?? specialist.Email ?? "Unknown",
                Email = specialist.Email ?? "",
                StudiesAssigned = periodStudies.Count,
                StudiesCompleted = completedStudies.Count,
                SiteVisitsCompleted = periodStudies.Count(s => s.SiteVisitDate >= startDate && s.SiteVisitDate <= endDate),
                RevenueGenerated = revenue
            };

            // Average completion time
            var completionDays = completedStudies
                .Where(s => s.DateCreated.HasValue)
                .Select(s => (s.DateApproved!.Value - s.DateCreated!.Value).TotalDays)
                .ToList();

            if (completionDays.Count != 0)
            {
                metric.AverageCompletionDays = completionDays.Average();
            }

            metrics.Add(metric);
        }

        return metrics.OrderByDescending(m => m.StudiesCompleted).ToList();
    }

    public async Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(int tenantId, int monthsBack = 12)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var startDate = DateTime.UtcNow.AddMonths(-monthsBack).Date;
        startDate = new DateTime(startDate.Year, startDate.Month, 1); // Start of month

        var trends = new List<MonthlyTrend>();

        // Get all relevant data in one query each
        var studies = await context.ReserveStudies
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && 
                       s.DateDeleted == null &&
                       s.DateCreated >= startDate)
            .Select(s => new { s.DateCreated, s.DateApproved, s.IsComplete })
            .ToListAsync();

        var invoices = await context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId && 
                       i.DateDeleted == null &&
                       i.PaidAt >= startDate &&
                       i.Status == InvoiceStatus.Paid)
            .Select(i => new { i.PaidAt, i.AmountPaid })
            .ToListAsync();

        var communities = await context.Communities
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && 
                       c.DateDeleted == null &&
                       c.DateCreated >= startDate)
            .Select(c => c.DateCreated)
            .ToListAsync();

        // Generate monthly data
        for (int i = 0; i < monthsBack; i++)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-monthsBack + i + 1);
            monthStart = new DateTime(monthStart.Year, monthStart.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var trend = new MonthlyTrend
            {
                Year = monthStart.Year,
                Month = monthStart.Month,
                MonthName = monthStart.ToString("MMM"),
                StudiesCreated = studies.Count(s => s.DateCreated >= monthStart && s.DateCreated < monthEnd),
                StudiesCompleted = studies.Count(s => s.IsComplete && s.DateApproved >= monthStart && s.DateApproved < monthEnd),
                Revenue = invoices.Where(i => i.PaidAt >= monthStart && i.PaidAt < monthEnd).Sum(i => i.AmountPaid),
                NewCommunities = communities.Count(c => c >= monthStart && c < monthEnd)
            };

            trends.Add(trend);
        }

        return trends;
    }

    public async Task<List<ElementPopularity>> GetElementPopularityAsync(int tenantId, int topCount = 20)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        // Building elements
        var buildingElements = await context.ReserveStudyBuildingElements
            .IgnoreQueryFilters()
            .Where(e => e.ReserveStudy != null && e.ReserveStudy.TenantId == tenantId)
            .GroupBy(e => new { e.BuildingElementId, e.BuildingElement!.Name })
            .Select(g => new ElementPopularity
            {
                ElementId = g.Key.BuildingElementId,
                ElementName = g.Key.Name ?? "Unknown",
                ElementType = "Building",
                UsageCount = g.Count(),
                AverageReplacementCost = g.Average(e => e.ReplacementCost ?? 0),
                TotalReplacementCost = g.Sum(e => e.ReplacementCost ?? 0)
            })
            .ToListAsync();

        // Common elements
        var commonElements = await context.ReserveStudyCommonElements
            .IgnoreQueryFilters()
            .Where(e => e.ReserveStudy != null && e.ReserveStudy.TenantId == tenantId)
            .GroupBy(e => new { e.CommonElementId, e.CommonElement!.Name })
            .Select(g => new ElementPopularity
            {
                ElementId = g.Key.CommonElementId,
                ElementName = g.Key.Name ?? "Unknown",
                ElementType = "Common",
                UsageCount = g.Count(),
                AverageReplacementCost = g.Average(e => e.ReplacementCost ?? 0),
                TotalReplacementCost = g.Sum(e => e.ReplacementCost ?? 0)
            })
            .ToListAsync();

        // Combine and sort by usage
        return buildingElements
            .Concat(commonElements)
            .OrderByDescending(e => e.UsageCount)
            .Take(topCount)
            .ToList();
    }
}
