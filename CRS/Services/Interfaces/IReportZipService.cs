using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for creating ZIP archives of multiple reports.
/// </summary>
public interface IReportZipService
{
    /// <summary>
    /// Creates a ZIP archive containing multiple PDF reports.
    /// </summary>
    /// <param name="reportIds">IDs of reports to include</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ZIP file bytes and suggested filename</returns>
    Task<ReportZipResult> CreateZipAsync(IEnumerable<Guid> reportIds, CancellationToken ct = default);

    /// <summary>
    /// Creates a ZIP archive of all reports for a reserve study.
    /// </summary>
    /// <param name="studyId">Reserve study ID</param>
    /// <param name="includeSuperseded">Whether to include superseded reports</param>
    /// <param name="ct">Cancellation token</param>
    Task<ReportZipResult> CreateStudyReportsZipAsync(Guid studyId, bool includeSuperseded = false, CancellationToken ct = default);

    /// <summary>
    /// Creates a ZIP archive of selected report types for a study.
    /// </summary>
    /// <param name="studyId">Reserve study ID</param>
    /// <param name="reportTypes">Types of reports to include</param>
    /// <param name="ct">Cancellation token</param>
    Task<ReportZipResult> CreateStudyReportsZipAsync(Guid studyId, IEnumerable<ReportType> reportTypes, CancellationToken ct = default);
}

/// <summary>
/// Result of creating a ZIP archive.
/// </summary>
public record ReportZipResult(
    byte[] ZipBytes,
    string FileName,
    int ReportCount,
    long TotalSizeBytes
);
