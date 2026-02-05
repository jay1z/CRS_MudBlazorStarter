using System.IO.Compression;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for archiving tenant data to Azure Blob Storage cold tier.
/// Archives financial records for legal compliance before permanent tenant deletion.
/// </summary>
public class TenantArchiveService : ITenantArchiveService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly ILogger<TenantArchiveService> _logger;
    private const string ArchiveContainerName = "tenant-archives";
    
    // Retention: 7 years for financial records (common legal requirement)
    private static readonly TimeSpan ArchiveRetentionPeriod = TimeSpan.FromDays(7 * 365);

    public TenantArchiveService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<TenantArchiveService> logger,
        BlobServiceClient? blobServiceClient = null)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<TenantArchiveResult> ArchiveTenantDataAsync(int tenantId, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting archive for tenant {TenantId}, reason: {Reason}", tenantId, reason);

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Get tenant info
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null)
        {
            return new TenantArchiveResult(
                Success: false,
                ArchiveUrl: null,
                ArchivedAt: DateTime.UtcNow,
                InvoicesArchived: 0,
                PaymentsArchived: 0,
                ReportsArchived: 0,
                TotalSizeBytes: 0,
                ErrorMessage: "Tenant not found"
            );
        }

        try
        {
            // Gather all data to archive
            var archiveData = new TenantArchiveData
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                TenantSubdomain = tenant.Subdomain,
                ArchivedAt = DateTime.UtcNow,
                Reason = reason,
                RetainUntil = DateTime.UtcNow.Add(ArchiveRetentionPeriod)
            };

            // Archive invoices
            archiveData.Invoices = await context.Invoices
                .Where(i => i.TenantId == tenantId)
                .Select(i => new ArchivedInvoice
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    IssueDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.AmountPaid,
                    Status = i.Status.ToString(),
                    BillToName = i.BillToName,
                    BillToEmail = i.BillToEmail,
                    CreatedAt = i.DateCreated
                })
                .ToListAsync(ct);

            // Archive payments
            archiveData.Payments = await context.PaymentRecords
                .Where(p => p.TenantId == tenantId)
                .Select(p => new ArchivedPayment
                {
                    Id = p.Id,
                    InvoiceId = p.InvoiceId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    StripePaymentIntentId = p.StripePaymentIntentId
                })
                .ToListAsync(ct);

            // Archive credit memos
            archiveData.CreditMemos = await context.CreditMemos
                .Where(c => c.TenantId == tenantId)
                .Select(c => new ArchivedCreditMemo
                {
                    Id = c.Id,
                    CreditMemoNumber = c.CreditMemoNumber,
                    InvoiceId = c.InvoiceId,
                    Amount = c.Amount,
                    IssueDate = c.IssueDate,
                    Reason = c.Reason.ToString(),
                    IsRefunded = c.IsRefunded,
                    StripeRefundId = c.StripeRefundId
                })
                .ToListAsync(ct);

            // Archive generated reports metadata
            var reportRecords = await context.GeneratedReports
                .Where(r => r.TenantId == tenantId)
                .Select(r => new { r.Id, r.ReserveStudyId, r.Type, r.Version, r.GeneratedAt, r.Status, r.StorageUrl })
                .ToListAsync(ct);

            archiveData.Reports = reportRecords.Select(r => new ArchivedReportMetadata
            {
                Id = r.Id,
                ReserveStudyId = r.ReserveStudyId,
                Type = r.Type.ToString(),
                Version = int.TryParse(r.Version, out var v) ? v : 1,
                GeneratedAt = r.GeneratedAt,
                Status = r.Status.ToString(),
                StorageUrl = r.StorageUrl
            }).ToList();

            // Archive reserve study summary
            archiveData.Studies = await context.ReserveStudies
                .Where(s => s.TenantId == tenantId)
                .Include(s => s.Community)
                .Select(s => new ArchivedStudySummary
                {
                    Id = s.Id,
                    CommunityName = s.Community != null ? s.Community.Name : null,
                    CreatedAt = s.DateCreated,
                    CompletedAt = s.IsComplete ? s.DateModified : null,
                    IsComplete = s.IsComplete
                })
                .ToListAsync(ct);

            // Serialize to JSON
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(archiveData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Create ZIP archive
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Add main data file
                var dataEntry = archive.CreateEntry("tenant_data.json", CompressionLevel.Optimal);
                using var entryStream = dataEntry.Open();
                await entryStream.WriteAsync(jsonBytes, ct);
            }
            zipStream.Position = 0;
            var zipBytes = zipStream.ToArray();

            // Upload to Azure Blob Storage (Cool tier for cost-effective long-term storage)
            string? archiveUrl = null;
            if (_blobServiceClient != null)
            {
                archiveUrl = await UploadToAzureAsync(tenantId, tenant.Name, zipBytes, ct);
            }
            else
            {
                // Log for development without Azure
                _logger.LogWarning(
                    "BlobServiceClient not configured - archive data not uploaded. Archive size: {Size} bytes",
                    zipBytes.Length);
            }

            var result = new TenantArchiveResult(
                Success: true,
                ArchiveUrl: archiveUrl,
                ArchivedAt: DateTime.UtcNow,
                InvoicesArchived: archiveData.Invoices.Count,
                PaymentsArchived: archiveData.Payments.Count,
                ReportsArchived: archiveData.Reports.Count,
                TotalSizeBytes: zipBytes.Length
            );

            _logger.LogInformation(
                "Archived tenant {TenantId}: {Invoices} invoices, {Payments} payments, {Reports} reports. Size: {Size} bytes",
                tenantId, result.InvoicesArchived, result.PaymentsArchived, result.ReportsArchived, result.TotalSizeBytes);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive tenant {TenantId}", tenantId);
            return new TenantArchiveResult(
                Success: false,
                ArchiveUrl: null,
                ArchivedAt: DateTime.UtcNow,
                InvoicesArchived: 0,
                PaymentsArchived: 0,
                ReportsArchived: 0,
                TotalSizeBytes: 0,
                ErrorMessage: ex.Message
            );
        }
    }

    private async Task<string> UploadToAzureAsync(int tenantId, string tenantName, byte[] zipBytes, CancellationToken ct)
    {
        var containerClient = _blobServiceClient!.GetBlobContainerClient(ArchiveContainerName);

        // Create container if it doesn't exist (with Cool access tier)
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        // Generate blob name
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var sanitizedName = new string(tenantName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        var blobName = $"tenant-{tenantId}/{sanitizedName}_archive_{timestamp}.zip";

        var blobClient = containerClient.GetBlobClient(blobName);

        // Upload with Cool access tier for cost-effective storage
        using var stream = new MemoryStream(zipBytes);
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = "application/zip" },
            metadata: new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["tenantName"] = tenantName,
                ["archivedAt"] = DateTime.UtcNow.ToString("O"),
                ["retainUntil"] = DateTime.UtcNow.Add(ArchiveRetentionPeriod).ToString("O")
            },
            accessTier: AccessTier.Cool,
            cancellationToken: ct);

        _logger.LogInformation("Uploaded archive to Azure Blob Storage: {BlobName}", blobName);
        return blobClient.Uri.ToString();
    }

    public async Task<TenantArchiveInfo?> GetArchiveInfoAsync(int tenantId, CancellationToken ct = default)
    {
        if (_blobServiceClient == null)
        {
            return null;
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ArchiveContainerName);
            if (!await containerClient.ExistsAsync(ct))
            {
                return null;
            }

            // List blobs for this tenant
            var prefix = $"tenant-{tenantId}/";
            await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: ct))
            {
                // Return the most recent archive
                return new TenantArchiveInfo(
                    TenantId: tenantId,
                    TenantName: blob.Metadata != null && blob.Metadata.TryGetValue("tenantName", out var tenantName) ? tenantName : "Unknown",
                    ArchiveUrl: $"{containerClient.Uri}/{blob.Name}",
                    ArchivedAt: blob.Properties.CreatedOn?.UtcDateTime ?? DateTime.UtcNow,
                    Reason: blob.Metadata != null && blob.Metadata.TryGetValue("reason", out var reason) ? reason : "Unknown",
                    SizeBytes: blob.Properties.ContentLength ?? 0
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get archive info for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> IsArchivedAsync(int tenantId, CancellationToken ct = default)
    {
        var info = await GetArchiveInfoAsync(tenantId, ct);
        return info != null;
    }
}

#region Archive Data Models

internal class TenantArchiveData
{
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? TenantSubdomain { get; set; }
    public DateTime ArchivedAt { get; set; }
    public string? Reason { get; set; }
    public DateTime RetainUntil { get; set; }
    public List<ArchivedInvoice> Invoices { get; set; } = [];
    public List<ArchivedPayment> Payments { get; set; } = [];
    public List<ArchivedCreditMemo> CreditMemos { get; set; } = [];
    public List<ArchivedReportMetadata> Reports { get; set; } = [];
    public List<ArchivedStudySummary> Studies { get; set; } = [];
}

internal class ArchivedInvoice
{
    public Guid Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string? Status { get; set; }
    public string? BillToName { get; set; }
    public string? BillToEmail { get; set; }
    public DateTime? CreatedAt { get; set; }
}

internal class ArchivedPayment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? StripePaymentIntentId { get; set; }
}

internal class ArchivedCreditMemo
{
    public Guid Id { get; set; }
    public string? CreditMemoNumber { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime IssueDate { get; set; }
    public string? Reason { get; set; }
    public bool IsRefunded { get; set; }
    public string? StripeRefundId { get; set; }
}

internal class ArchivedReportMetadata
{
    public Guid Id { get; set; }
    public Guid ReserveStudyId { get; set; }
    public string? Type { get; set; }
    public int Version { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? Status { get; set; }
    public string? StorageUrl { get; set; }
}

internal class ArchivedStudySummary
{
    public Guid Id { get; set; }
    public string? CommunityName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsComplete { get; set; }
}

#endregion
