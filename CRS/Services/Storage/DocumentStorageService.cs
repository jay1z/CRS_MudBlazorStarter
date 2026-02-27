using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Horizon.Services.Storage;

/// <summary>
/// Implementation of document storage service using Azure Blob Storage.
/// </summary>
public class DocumentStorageService : IDocumentStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<DocumentStorageService> _logger;
    private const string ContainerName = "documents";

    public DocumentStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<DocumentStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

        if (!await containerClient.ExistsAsync(ct))
        {
            await containerClient.CreateAsync(PublicAccessType.None, cancellationToken: ct);
            _logger.LogInformation("Created private blob container: {ContainerName}", ContainerName);
        }

        return containerClient;
    }

    public (bool Valid, string? Error) ValidateFile(string fileName, string contentType, long fileSize)
    {
        // Check file size
        if (fileSize > DocumentUploadConfig.MaxFileSizeBytes)
        {
            return (false, $"File too large. Maximum size is {DocumentUploadConfig.MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Check extension
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!DocumentUploadConfig.AllowedExtensions.Contains(ext))
        {
            return (false, $"Invalid file type. Allowed types: {string.Join(", ", DocumentUploadConfig.AllowedExtensions)}");
        }

        return (true, null);
    }

    public async Task<DocumentUploadResult> UploadAsync(
        int tenantId,
        Guid? reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        var containerClient = await GetContainerClientAsync(ct);

        // Generate unique blob name with path structure
        var documentId = Guid.CreateVersion7();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        
        // Path structure: tenant-{id}/study-{id}/{documentId}{ext} or tenant-{id}/general/{documentId}{ext}
        var basePath = reserveStudyId.HasValue && reserveStudyId != Guid.Empty
            ? $"tenant-{tenantId}/study-{reserveStudyId}"
            : $"tenant-{tenantId}/general";
        
        var blobName = $"{basePath}/{documentId}{ext}";

        // Upload to blob storage
        using var uploadStream = new MemoryStream(fileBytes);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            uploadStream,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded document for tenant {TenantId}: {BlobName} ({Size} bytes)",
            tenantId, blobName, fileBytes.Length);

        return new DocumentUploadResult(
            StorageUrl: blobName,
            ContentType: contentType,
            FileSizeBytes: fileBytes.Length
        );
    }

    public async Task<byte[]> DownloadAsync(string storageUrl, CancellationToken ct = default)
    {
        var containerClient = await GetContainerClientAsync(ct);
        var blobClient = containerClient.GetBlobClient(storageUrl);

        using var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream, ct);

        _logger.LogInformation("Downloaded document: {BlobName}", storageUrl);
        return stream.ToArray();
    }

    public async Task<bool> DeleteAsync(string storageUrl, CancellationToken ct = default)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(ct);
            var blobClient = containerClient.GetBlobClient(storageUrl);

            if (await blobClient.ExistsAsync(ct))
            {
                await blobClient.DeleteAsync(cancellationToken: ct);
                _logger.LogInformation("Deleted document: {BlobName}", storageUrl);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {StorageUrl}", storageUrl);
            return false;
        }
    }

    public string GetSasUrl(string storageUrl, TimeSpan? validity = null)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(storageUrl);

        if (!blobClient.CanGenerateSasUri)
        {
            _logger.LogWarning("Cannot generate SAS URI for {StorageUrl}", storageUrl);
            return blobClient.Uri.ToString();
        }

        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(validity ?? TimeSpan.FromHours(1))
        };
        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }

    public IEnumerable<string> GetSasUrls(IEnumerable<string> storageUrls, TimeSpan? validity = null)
    {
        return storageUrls.Select(url => GetSasUrl(url, validity));
    }
}

/// <summary>
/// Null implementation for development without Azure Storage.
/// </summary>
public class NullDocumentStorageService : IDocumentStorageService
{
    private readonly ILogger<NullDocumentStorageService> _logger;

    public NullDocumentStorageService(ILogger<NullDocumentStorageService> logger)
    {
        _logger = logger;
    }

    public (bool Valid, string? Error) ValidateFile(string fileName, string contentType, long fileSize)
    {
        if (fileSize > DocumentUploadConfig.MaxFileSizeBytes)
        {
            return (false, "File too large");
        }
        return (true, null);
    }

    public Task<DocumentUploadResult> UploadAsync(
        int tenantId,
        Guid? reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        _logger.LogWarning("DocumentStorageService not configured - using placeholder URL");
        return Task.FromResult(new DocumentUploadResult(
            StorageUrl: $"placeholder/{Guid.NewGuid()}{Path.GetExtension(fileName)}",
            ContentType: contentType,
            FileSizeBytes: fileBytes.Length
        ));
    }

    public Task<byte[]> DownloadAsync(string storageUrl, CancellationToken ct = default)
    {
        _logger.LogWarning("DocumentStorageService not configured - returning empty bytes");
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<bool> DeleteAsync(string storageUrl, CancellationToken ct = default)
    {
        _logger.LogWarning("DocumentStorageService not configured - simulating delete");
        return Task.FromResult(true);
    }

    public string GetSasUrl(string storageUrl, TimeSpan? validity = null)
    {
        return $"https://placehold.co/800x600?text=Document";
    }

    public IEnumerable<string> GetSasUrls(IEnumerable<string> storageUrls, TimeSpan? validity = null)
    {
        return storageUrls.Select(_ => GetSasUrl("", validity));
    }
}
