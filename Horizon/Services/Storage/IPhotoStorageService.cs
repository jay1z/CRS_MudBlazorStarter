using Microsoft.AspNetCore.Components.Forms;

namespace Horizon.Services.Storage;

/// <summary>
/// Configuration for site visit photo uploads.
/// </summary>
public static class PhotoUploadConfig
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    public const long ThumbnailMaxSize = 500 * 1024;      // 500KB for thumbnails
    public const int ThumbnailWidth = 400;
    public const int ThumbnailHeight = 300;
    public const int MaxWidth = 4096;
    public const int MaxHeight = 4096;

    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".heic" };
    public static readonly string[] AllowedMimeTypes = { 
        "image/jpeg", "image/png", "image/webp", "image/heic"
    };
}

/// <summary>
/// Result of uploading a photo to storage.
/// </summary>
public record PhotoUploadResult(
    string StorageUrl,
    string? ThumbnailUrl,
    string ContentType,
    long FileSizeBytes
);

/// <summary>
/// Service for managing site visit photo storage in Azure Blob Storage.
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>
    /// Uploads a photo for a reserve study.
    /// </summary>
    Task<PhotoUploadResult> UploadPhotoAsync(
        int tenantId, 
        Guid reserveStudyId, 
        IBrowserFile file, 
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a file from pre-read bytes (avoids IBrowserFile lifecycle issues).
    /// </summary>
    Task<PhotoUploadResult> UploadFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads a photo from pre-read bytes with thumbnail generation.
    /// </summary>
    Task<PhotoUploadResult> UploadPhotoFromBytesAsync(
        int tenantId,
        Guid reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a photo from storage.
    /// </summary>
    Task<bool> DeletePhotoAsync(string storageUrl, string? thumbnailUrl = null, CancellationToken ct = default);

    /// <summary>
    /// Validates a photo before upload.
    /// </summary>
    Task<(bool Valid, string? Error)> ValidatePhotoAsync(IBrowserFile file, CancellationToken ct = default);

    /// <summary>
    /// Gets a SAS URL for a photo that is valid for a specified duration.
    /// </summary>
    string GetSasUrl(string storageUrl, TimeSpan? validity = null);

    /// <summary>
    /// Gets multiple SAS URLs efficiently.
    /// </summary>
    IEnumerable<string> GetSasUrls(IEnumerable<string> storageUrls, TimeSpan? validity = null);
}
