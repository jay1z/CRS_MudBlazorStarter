using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Components.Forms;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Horizon.Services.Storage;

/// <summary>
/// Implementation of photo storage service using Azure Blob Storage.
/// </summary>
public class PhotoStorageService : IPhotoStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<PhotoStorageService> _logger;
    private const string ContainerName = "site-visit-photos";

    public PhotoStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<PhotoStorageService> logger)
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

    public async Task<(bool Valid, string? Error)> ValidatePhotoAsync(IBrowserFile file, CancellationToken ct = default)
    {
        // Check file size
        if (file.Size > PhotoUploadConfig.MaxFileSizeBytes)
        {
            return (false, $"File too large. Maximum size is {PhotoUploadConfig.MaxFileSizeBytes / 1024 / 1024}MB");
        }

        // Check extension
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!PhotoUploadConfig.AllowedExtensions.Contains(ext))
        {
            return (false, $"Invalid file type. Allowed types: {string.Join(", ", PhotoUploadConfig.AllowedExtensions)}");
        }

        // Check MIME type
        if (!PhotoUploadConfig.AllowedMimeTypes.Contains(file.ContentType))
        {
            return (false, "Invalid file type");
        }

        // Validate image dimensions
        try
        {
            using var stream = file.OpenReadStream(PhotoUploadConfig.MaxFileSizeBytes);
            using var image = await Image.LoadAsync(stream, ct);

            if (image.Width > PhotoUploadConfig.MaxWidth || image.Height > PhotoUploadConfig.MaxHeight)
            {
                return (false, $"Image too large. Maximum dimensions: {PhotoUploadConfig.MaxWidth}×{PhotoUploadConfig.MaxHeight}px");
            }

            if (image.Width < 100 || image.Height < 100)
            {
                return (false, "Image too small. Minimum dimensions: 100×100px");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validating image for {FileName}", file.Name);
            return (false, "Invalid or corrupted image file");
        }

        return (true, null);
    }

    public async Task<PhotoUploadResult> UploadPhotoAsync(
        int tenantId,
        Guid reserveStudyId,
        IBrowserFile file,
        CancellationToken ct = default)
    {
        var containerClient = await GetContainerClientAsync(ct);

        // Generate unique blob name with path structure
        var photoId = Guid.CreateVersion7();
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        var basePath = $"tenant-{tenantId}/study-{reserveStudyId}";
        var mainBlobName = $"{basePath}/{photoId}{ext}";
        var thumbnailBlobName = $"{basePath}/thumbs/{photoId}_thumb.jpg";

        // Load and process image
        using var inputStream = file.OpenReadStream(PhotoUploadConfig.MaxFileSizeBytes);
        using var image = await Image.LoadAsync(inputStream, ct);

        // Upload main image (optimized)
        using var mainStream = new MemoryStream();
        await image.SaveAsJpegAsync(mainStream, new JpegEncoder { Quality = 85 }, ct);
        mainStream.Position = 0;

        var mainBlobClient = containerClient.GetBlobClient(mainBlobName);
        await mainBlobClient.UploadAsync(
            mainStream,
            new BlobHttpHeaders { ContentType = "image/jpeg" },
            cancellationToken: ct);

        // Generate and upload thumbnail
        using var thumbStream = new MemoryStream();
        using var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(PhotoUploadConfig.ThumbnailWidth, PhotoUploadConfig.ThumbnailHeight)
        }));
        await thumbnail.SaveAsJpegAsync(thumbStream, new JpegEncoder { Quality = 75 }, ct);
        thumbStream.Position = 0;

        var thumbBlobClient = containerClient.GetBlobClient(thumbnailBlobName);
        await thumbBlobClient.UploadAsync(
            thumbStream,
            new BlobHttpHeaders { ContentType = "image/jpeg" },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded photo for tenant {TenantId}, study {StudyId}: {BlobName}",
            tenantId, reserveStudyId, mainBlobName);

        return new PhotoUploadResult(
            StorageUrl: mainBlobName,
            ThumbnailUrl: thumbnailBlobName,
            ContentType: "image/jpeg",
            FileSizeBytes: mainStream.Length
        );
    }

    public async Task<PhotoUploadResult> UploadFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        var containerClient = await GetContainerClientAsync(ct);

        // Generate unique blob name with path structure
        var photoId = Guid.CreateVersion7();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var basePath = $"tenant-{tenantId}/study-{reserveStudyId}";
        var mainBlobName = $"{basePath}/{photoId}{ext}";

        // Upload directly to blob storage
        using var uploadStream = new MemoryStream(fileBytes);
        var mainBlobClient = containerClient.GetBlobClient(mainBlobName);
        await mainBlobClient.UploadAsync(
            uploadStream,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded file for tenant {TenantId}, study {StudyId}: {BlobName}",
            tenantId, reserveStudyId, mainBlobName);

        return new PhotoUploadResult(
            StorageUrl: mainBlobName,
            ThumbnailUrl: null,
            ContentType: contentType,
            FileSizeBytes: fileBytes.Length
        );
    }

    public async Task<PhotoUploadResult> UploadPhotoFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        var containerClient = await GetContainerClientAsync(ct);

        // Generate unique blob name with path structure
        var photoId = Guid.CreateVersion7();
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var basePath = $"tenant-{tenantId}/study-{reserveStudyId}";
        var mainBlobName = $"{basePath}/{photoId}{ext}";
        var thumbnailBlobName = $"{basePath}/thumbs/{photoId}_thumb.jpg";

        // Load and process image from bytes
        using var inputStream = new MemoryStream(fileBytes);
        using var image = await Image.LoadAsync(inputStream, ct);

        // Upload main image (optimized)
        using var mainStream = new MemoryStream();
        await image.SaveAsJpegAsync(mainStream, new JpegEncoder { Quality = 85 }, ct);
        mainStream.Position = 0;

        var mainBlobClient = containerClient.GetBlobClient(mainBlobName);
        await mainBlobClient.UploadAsync(
            mainStream,
            new BlobHttpHeaders { ContentType = "image/jpeg" },
            cancellationToken: ct);

        // Generate and upload thumbnail
        using var thumbStream = new MemoryStream();
        using var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(PhotoUploadConfig.ThumbnailWidth, PhotoUploadConfig.ThumbnailHeight)
        }));
        await thumbnail.SaveAsJpegAsync(thumbStream, new JpegEncoder { Quality = 75 }, ct);
        thumbStream.Position = 0;

        var thumbBlobClient = containerClient.GetBlobClient(thumbnailBlobName);
        await thumbBlobClient.UploadAsync(
            thumbStream,
            new BlobHttpHeaders { ContentType = "image/jpeg" },
            cancellationToken: ct);

        _logger.LogInformation(
            "Uploaded photo for tenant {TenantId}, study {StudyId}: {BlobName}",
            tenantId, reserveStudyId, mainBlobName);

        return new PhotoUploadResult(
            StorageUrl: mainBlobName,
            ThumbnailUrl: thumbnailBlobName,
            ContentType: "image/jpeg",
            FileSizeBytes: mainStream.Length
        );
    }

    public async Task<bool> DeletePhotoAsync(string storageUrl, string? thumbnailUrl = null, CancellationToken ct = default)
    {
        try
        {
            var containerClient = await GetContainerClientAsync(ct);

            // Delete main photo
            var mainBlobClient = containerClient.GetBlobClient(storageUrl);
            if (await mainBlobClient.ExistsAsync(ct))
            {
                await mainBlobClient.DeleteAsync(cancellationToken: ct);
                _logger.LogInformation("Deleted photo: {BlobName}", storageUrl);
            }

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                var thumbBlobClient = containerClient.GetBlobClient(thumbnailUrl);
                if (await thumbBlobClient.ExistsAsync(ct))
                {
                    await thumbBlobClient.DeleteAsync(cancellationToken: ct);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo: {StorageUrl}", storageUrl);
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
public class NullPhotoStorageService : IPhotoStorageService
{
    private readonly ILogger<NullPhotoStorageService> _logger;

    public NullPhotoStorageService(ILogger<NullPhotoStorageService> logger)
    {
        _logger = logger;
    }

    public Task<PhotoUploadResult> UploadPhotoAsync(
        int tenantId,
        Guid reserveStudyId,
        IBrowserFile file,
        CancellationToken ct = default)
    {
        _logger.LogWarning("PhotoStorageService not configured - using placeholder URL");
        return Task.FromResult(new PhotoUploadResult(
            StorageUrl: $"placeholder/{Guid.NewGuid()}.jpg",
            ThumbnailUrl: $"placeholder/thumb_{Guid.NewGuid()}.jpg",
            ContentType: file.ContentType,
            FileSizeBytes: file.Size
        ));
    }

    public Task<PhotoUploadResult> UploadFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        _logger.LogWarning("PhotoStorageService not configured - using placeholder URL");
        return Task.FromResult(new PhotoUploadResult(
            StorageUrl: $"placeholder/{Guid.NewGuid()}{Path.GetExtension(fileName)}",
            ThumbnailUrl: null,
            ContentType: contentType,
            FileSizeBytes: fileBytes.Length
        ));
    }

    public Task<PhotoUploadResult> UploadPhotoFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default)
    {
        _logger.LogWarning("PhotoStorageService not configured - using placeholder URL");
        return Task.FromResult(new PhotoUploadResult(
            StorageUrl: $"placeholder/{Guid.NewGuid()}.jpg",
            ThumbnailUrl: $"placeholder/thumb_{Guid.NewGuid()}.jpg",
            ContentType: "image/jpeg",
            FileSizeBytes: fileBytes.Length
        ));
    }

    public Task<bool> DeletePhotoAsync(string storageUrl, string? thumbnailUrl = null, CancellationToken ct = default)
    {
        _logger.LogWarning("PhotoStorageService not configured - simulating delete");
        return Task.FromResult(true);
    }

    public Task<(bool Valid, string? Error)> ValidatePhotoAsync(IBrowserFile file, CancellationToken ct = default)
    {
        if (file.Size > PhotoUploadConfig.MaxFileSizeBytes)
        {
            return Task.FromResult<(bool, string?)>((false, "File too large"));
        }
        return Task.FromResult<(bool, string?)>((true, null));
    }

    public string GetSasUrl(string storageUrl, TimeSpan? validity = null)
    {
        return $"https://placehold.co/800x600?text=Photo";
    }

    public IEnumerable<string> GetSasUrls(IEnumerable<string> storageUrls, TimeSpan? validity = null)
    {
        return storageUrls.Select(_ => GetSasUrl("", validity));
    }
}
