namespace CRS.Services.Storage;

/// <summary>
/// Configuration for document uploads.
/// </summary>
public static class DocumentUploadConfig
{
    public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public static readonly string[] AllowedExtensions = 
    { 
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".txt", ".csv", ".rtf", ".zip"
    };
    
    public static readonly string[] AllowedMimeTypes = 
    { 
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "text/plain", "text/csv", "application/rtf",
        "application/zip", "application/x-zip-compressed"
    };
}

/// <summary>
/// Result of uploading a document to storage.
/// </summary>
public record DocumentUploadResult(
    string StorageUrl,
    string ContentType,
    long FileSizeBytes
);

/// <summary>
/// Service for managing document storage in Azure Blob Storage.
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Uploads a document from pre-read bytes.
    /// </summary>
    Task<DocumentUploadResult> UploadAsync(
        int tenantId,
        Guid? reserveStudyId,
        string fileName,
        string contentType,
        byte[] fileBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads a document from storage.
    /// </summary>
    Task<byte[]> DownloadAsync(string storageUrl, CancellationToken ct = default);

    /// <summary>
    /// Deletes a document from storage.
    /// </summary>
    Task<bool> DeleteAsync(string storageUrl, CancellationToken ct = default);

    /// <summary>
    /// Gets a SAS URL for a document that is valid for a specified duration.
    /// </summary>
    string GetSasUrl(string storageUrl, TimeSpan? validity = null);

    /// <summary>
    /// Gets multiple SAS URLs efficiently.
    /// </summary>
    IEnumerable<string> GetSasUrls(IEnumerable<string> storageUrls, TimeSpan? validity = null);

    /// <summary>
    /// Validates a file before upload.
    /// </summary>
    (bool Valid, string? Error) ValidateFile(string fileName, string contentType, long fileSize);
}
