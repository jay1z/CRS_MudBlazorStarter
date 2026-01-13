using CRS.Models.NarrativeTemplates;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Builds a <see cref="ReserveStudyReportContext"/> from a reserve study and its related entities.
/// This service aggregates data from multiple sources into a single context for template rendering.
/// </summary>
public interface IReportContextBuilder
{
    /// <summary>
    /// Builds a complete report context for the specified reserve study.
    /// </summary>
    /// <param name="reserveStudyId">The ID of the reserve study.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully populated <see cref="ReserveStudyReportContext"/>.</returns>
    Task<ReserveStudyReportContext> BuildContextAsync(
        Guid reserveStudyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a report context with optional customization.
    /// </summary>
    /// <param name="reserveStudyId">The ID of the reserve study.</param>
    /// <param name="options">Options for customizing context building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully populated <see cref="ReserveStudyReportContext"/>.</returns>
    Task<ReserveStudyReportContext> BuildContextAsync(
        Guid reserveStudyId,
        ReportContextOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for customizing report context building.
/// </summary>
public class ReportContextOptions
{
    /// <summary>
    /// Include photos in the context. Default is true.
    /// </summary>
    public bool IncludePhotos { get; set; } = true;

    /// <summary>
    /// Include vendor information. Default is true.
    /// </summary>
    public bool IncludeVendors { get; set; } = true;

    /// <summary>
    /// Include glossary terms. Default is true.
    /// </summary>
    public bool IncludeGlossary { get; set; } = true;

    /// <summary>
    /// Include calculated outputs from the reserve calculator. Default is true.
    /// </summary>
    public bool IncludeCalculatedOutputs { get; set; } = true;

    /// <summary>
    /// Custom report title override.
    /// </summary>
    public string? ReportTitleOverride { get; set; }

    /// <summary>
    /// Custom report date override (defaults to current date).
    /// </summary>
    public DateTime? ReportDateOverride { get; set; }
}
