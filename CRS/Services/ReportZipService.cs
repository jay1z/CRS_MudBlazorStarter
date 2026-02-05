using System.IO.Compression;
using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Storage;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for creating ZIP archives of multiple PDF reports.
/// </summary>
public class ReportZipService : IReportZipService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IDocumentStorageService _storageService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReportZipService> _logger;

    public ReportZipService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IDocumentStorageService storageService,
        ITenantContext tenantContext,
        ILogger<ReportZipService> logger)
    {
        _dbFactory = dbFactory;
        _storageService = storageService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<ReportZipResult> CreateZipAsync(IEnumerable<Guid> reportIds, CancellationToken ct = default)
    {
        var idList = reportIds.ToList();
        if (idList.Count == 0)
        {
            throw new InvalidOperationException("No report IDs provided");
        }

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var reports = await context.GeneratedReports
            .Include(r => r.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(r => idList.Contains(r.Id) && r.DateDeleted == null)
            .ToListAsync(ct);

        if (reports.Count == 0)
        {
            throw new InvalidOperationException("No reports found");
        }

        return await CreateZipFromReportsAsync(reports, ct);
    }

    public async Task<ReportZipResult> CreateStudyReportsZipAsync(Guid studyId, bool includeSuperseded = false, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var query = context.GeneratedReports
            .Include(r => r.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(r => r.ReserveStudyId == studyId && r.DateDeleted == null);

        if (!includeSuperseded)
        {
            query = query.Where(r => r.Status != ReportStatus.Superseded);
        }

        var reports = await query
            .OrderBy(r => r.Type)
            .ThenByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

        if (reports.Count == 0)
        {
            throw new InvalidOperationException("No reports found for this study");
        }

        return await CreateZipFromReportsAsync(reports, ct);
    }

    public async Task<ReportZipResult> CreateStudyReportsZipAsync(Guid studyId, IEnumerable<ReportType> reportTypes, CancellationToken ct = default)
    {
        var typeList = reportTypes.ToList();
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var reports = await context.GeneratedReports
            .Include(r => r.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Where(r => r.ReserveStudyId == studyId && 
                        r.DateDeleted == null && 
                        r.Status != ReportStatus.Superseded &&
                        typeList.Contains(r.Type))
            .OrderBy(r => r.Type)
            .ThenByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

        if (reports.Count == 0)
        {
            throw new InvalidOperationException("No reports found matching criteria");
        }

        return await CreateZipFromReportsAsync(reports, ct);
    }

    private async Task<ReportZipResult> CreateZipFromReportsAsync(List<GeneratedReport> reports, CancellationToken ct)
    {
        using var memoryStream = new MemoryStream();
        long totalSize = 0;
        var studyName = reports.FirstOrDefault()?.ReserveStudy?.Community?.Name ?? "Reports";
        var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var report in reports)
            {
                if (string.IsNullOrEmpty(report.StorageUrl))
                {
                    _logger.LogWarning("Report {ReportId} has no storage URL, skipping", report.Id);
                    continue;
                }

                try
                {
                    // Download the PDF
                    var pdfBytes = await _storageService.DownloadAsync(report.StorageUrl, ct);
                    if (pdfBytes.Length == 0)
                    {
                        _logger.LogWarning("Report {ReportId} returned empty content, skipping", report.Id);
                        continue;
                    }

                    // Generate unique filename
                    var baseFileName = GenerateFileName(report);
                    var fileName = EnsureUniqueFileName(baseFileName, usedFileNames);
                    usedFileNames.Add(fileName);

                    // Add to ZIP
                    var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pdfBytes, ct);

                    totalSize += pdfBytes.Length;
                    _logger.LogDebug("Added {FileName} to ZIP ({Size} bytes)", fileName, pdfBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add report {ReportId} to ZIP", report.Id);
                    // Continue with other reports
                }
            }
        }

        memoryStream.Position = 0;
        var zipBytes = memoryStream.ToArray();

        // Generate ZIP filename
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var sanitizedStudyName = SanitizeFileName(studyName);
        var zipFileName = $"{sanitizedStudyName}_Reports_{timestamp}.zip";

        _logger.LogInformation(
            "Created ZIP with {Count} reports, total size: {TotalSize} bytes, ZIP size: {ZipSize} bytes",
            reports.Count, totalSize, zipBytes.Length);

        return new ReportZipResult(
            ZipBytes: zipBytes,
            FileName: zipFileName,
            ReportCount: reports.Count,
            TotalSizeBytes: totalSize
        );
    }

    private static string GenerateFileName(GeneratedReport report)
    {
        var communityName = report.ReserveStudy?.Community?.Name ?? "Report";
        var typeName = report.Type.ToString();
        var date = report.GeneratedAt.ToString("yyyyMMdd");
        var version = report.Version != "1.0" && report.Version != "1" ? $"_v{report.Version}" : "";
        
        return $"{SanitizeFileName(communityName)}_{typeName}{version}_{date}.pdf";
    }

    private static string EnsureUniqueFileName(string fileName, HashSet<string> usedNames)
    {
        if (!usedNames.Contains(fileName))
        {
            return fileName;
        }

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 2;

        while (usedNames.Contains($"{nameWithoutExt}_{counter}{ext}"))
        {
            counter++;
        }

        return $"{nameWithoutExt}_{counter}{ext}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());
        
        // Replace multiple underscores with single
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        return sanitized.Trim('_');
    }
}
