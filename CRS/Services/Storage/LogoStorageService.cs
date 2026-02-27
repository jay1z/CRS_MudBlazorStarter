using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.Components.Forms;

namespace Horizon.Services.Storage {
    public class LogoUploadConfig {
        public const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB
        public const long OptimalSizeBytes = 500 * 1024;      // 500KB (warn user)
        
        public static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
        public static readonly string[] AllowedMimeTypes = { 
            "image/png", "image/jpeg", "image/svg+xml", "image/webp" 
        };
    }

    public class LogoDimensions {
        public const int MaxWidth = 2000;
        public const int MaxHeight = 2000;
        public const int RecommendedWidth = 400;
        public const int RecommendedHeight = 400;
        public const int MinWidth = 100;
        public const int MinHeight = 100;
    }

    public interface ILogoStorageService {
        Task<string> UploadLogoAsync(int tenantId, IBrowserFile file, CancellationToken ct = default);
        Task<bool> DeleteLogoAsync(int tenantId, CancellationToken ct = default);
        Task<string?> GetLogoUrlAsync(int tenantId, CancellationToken ct = default);
        Task<(bool Valid, string? Error)> ValidateLogoAsync(IBrowserFile file, CancellationToken ct = default);
    }

    public class LogoStorageService : ILogoStorageService {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<LogoStorageService> _logger;
        private const string ContainerName = "tenant-logos";

        public LogoStorageService(BlobServiceClient blobServiceClient, ILogger<LogoStorageService> logger) {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken ct = default) {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            
            // Create container if it doesn't exist with PRIVATE access (not public)
            if (!await containerClient.ExistsAsync(ct)) {
                await containerClient.CreateAsync(PublicAccessType.None, cancellationToken: ct);
                _logger.LogInformation("Created private blob container: {ContainerName}", ContainerName);
            }
            
            return containerClient;
        }

        public async Task<(bool Valid, string? Error)> ValidateLogoAsync(IBrowserFile file, CancellationToken ct = default) {
            // Check file size
            if (file.Size > LogoUploadConfig.MaxFileSizeBytes) {
                return (false, $"File too large. Max: {LogoUploadConfig.MaxFileSizeBytes / 1024 / 1024}MB");
            }

            // Check extension
            var ext = Path.GetExtension(file.Name).ToLowerInvariant();
            if (!LogoUploadConfig.AllowedExtensions.Contains(ext)) {
                return (false, $"Invalid format. Allowed: {string.Join(", ", LogoUploadConfig.AllowedExtensions)}");
            }

            // Check MIME type (prevent spoofing)
            if (!LogoUploadConfig.AllowedMimeTypes.Contains(file.ContentType)) {
                return (false, "Invalid file type");
            }

            // Validate image dimensions (skip for SVG)
            if (ext != ".svg") {
                try {
                    using var stream = file.OpenReadStream(LogoUploadConfig.MaxFileSizeBytes);
                    using var image = await Image.LoadAsync(stream, ct);

                    if (image.Width > LogoDimensions.MaxWidth || image.Height > LogoDimensions.MaxHeight) {
                        return (false, $"Image too large. Max: {LogoDimensions.MaxWidth}×{LogoDimensions.MaxHeight}px");
                    }

                    if (image.Width < LogoDimensions.MinWidth || image.Height < LogoDimensions.MinHeight) {
                        return (false, $"Image too small. Min: {LogoDimensions.MinWidth}×{LogoDimensions.MinWidth}px");
                    }
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Error validating image for {FileName}", file.Name);
                    return (false, "Invalid image file");
                }
            }

            return (true, null);
        }

        public async Task<string> UploadLogoAsync(int tenantId, IBrowserFile file, CancellationToken ct = default) {
            var containerClient = await GetContainerClientAsync(ct);

            var ext = Path.GetExtension(file.Name).ToLowerInvariant();
            var blobName = $"tenant-{tenantId}/logo{ext}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Process and optimize image (skip for SVG)
            if (ext == ".svg") {
                // Upload SVG as-is
                using var stream = file.OpenReadStream(LogoUploadConfig.MaxFileSizeBytes);
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: ct);
            } else {
                // Load, optimize, and upload raster image
                using var inputStream = file.OpenReadStream(LogoUploadConfig.MaxFileSizeBytes);
                using var image = await Image.LoadAsync(inputStream, ct);

                // Resize if too large
                if (image.Width > LogoDimensions.RecommendedWidth || image.Height > LogoDimensions.RecommendedHeight) {
                    image.Mutate(x => x.Resize(new ResizeOptions {
                        Mode = ResizeMode.Max,
                        Size = new Size(LogoDimensions.RecommendedWidth, LogoDimensions.RecommendedHeight)
                    }));
                }

                // Save optimized image
                using var outputStream = new MemoryStream();
                if (ext == ".png") {
                    await image.SaveAsPngAsync(outputStream, new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression }, ct);
                } else {
                    await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 }, ct);
                }

                outputStream.Position = 0;
                var contentType = ext == ".png" ? "image/png" : "image/jpeg";
                await blobClient.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
            }

            _logger.LogInformation("Uploaded logo for tenant {TenantId} to blob storage: {BlobName}", tenantId, blobName);
            
            // Generate SAS URL with 1 year expiration for logos
            return GenerateSasUrl(blobClient);
        }

        private string GenerateSasUrl(BlobClient blobClient) {
            // Check if we can generate SAS (requires account key, not managed identity)
            if (!blobClient.CanGenerateSasUri) {
                _logger.LogWarning("Cannot generate SAS URI. Returning blob URI directly. Ensure storage account uses account key authentication.");
                return blobClient.Uri.ToString();
            }

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b", // blob
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // allow for clock skew
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1) // logos rarely change
            };
            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        public async Task<bool> DeleteLogoAsync(int tenantId, CancellationToken ct = default) {
            try {
                var containerClient = await GetContainerClientAsync(ct);
                
                // Try all possible extensions
                foreach (var ext in LogoUploadConfig.AllowedExtensions) {
                    var blobName = $"tenant-{tenantId}/logo{ext}";
                    var blobClient = containerClient.GetBlobClient(blobName);
                    
                    if (await blobClient.ExistsAsync(ct)) {
                        await blobClient.DeleteAsync(cancellationToken: ct);
                        _logger.LogInformation("Deleted logo for tenant {TenantId}: {BlobName}", tenantId, blobName);
                        return true;
                    }
                }

                return false;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error deleting logo for tenant {TenantId}", tenantId);
                return false;
            }
        }

        public async Task<string?> GetLogoUrlAsync(int tenantId, CancellationToken ct = default) {
            try {
                var containerClient = await GetContainerClientAsync(ct);

                // Try all possible extensions
                foreach (var ext in LogoUploadConfig.AllowedExtensions) {
                    var blobName = $"tenant-{tenantId}/logo{ext}";
                    var blobClient = containerClient.GetBlobClient(blobName);

                    if (await blobClient.ExistsAsync(ct)) {
                        return GenerateSasUrl(blobClient);
                    }
                }

                return null;
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Error getting logo URL for tenant {TenantId}", tenantId);
                return null;
            }
        }
    }
}
