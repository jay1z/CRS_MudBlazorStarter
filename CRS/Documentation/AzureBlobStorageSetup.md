# Azure Blob Storage Configuration for Logo Uploads

## Setup Instructions

### 1. Create Azure Storage Account

1. Go to Azure Portal: https://portal.azure.com
2. Create a new Storage Account:
   - **Name**: `{yourapp}storage` (e.g., `crsprodstorage`)
   - **Performance**: Standard
   - **Replication**: LRS (Locally Redundant Storage) - cheapest option
   - **Access tier**: Hot (for frequently accessed logos)

### 2. Get Connection String

1. In your storage account, go to **Security + networking** → **Access keys**
2. Copy **Connection string** from key1 or key2

### 3. Add to Configuration

**Development (User Secrets):**
```bash
dotnet user-secrets set "ConnectionStrings:AzureStorage" "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=yourkey;EndpointSuffix=core.windows.net"
```

**Production (Azure App Service Application Settings):**
```
Name: ConnectionStrings__AzureStorage
Value: DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=yourkey;EndpointSuffix=core.windows.net
```

**Or in appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=yourkey;EndpointSuffix=core.windows.net"
  }
}
```

### 4. Container Creation

The container `tenant-logos` is created automatically with **public read access** when the first logo is uploaded.

**Blob naming structure:**
```
tenant-logos/
  ├── tenant-1/
  │   └── logo.png
  ├── tenant-2/
  │   └── logo.jpg
  └── tenant-3/
      └── logo.svg
```

## Cost Estimate

### Storage
- **100 tenants × 100KB average** = 10MB = $0.0002/month
- **1,000 tenants × 100KB** = 100MB = $0.002/month
- **10,000 tenants × 100KB** = 1GB = $0.018/month

### Operations (Hot tier)
- **Write**: $0.05 per 10,000 operations
- **Read**: $0.004 per 10,000 operations

### Typical Monthly Cost Examples:
- **100 tenants, 10K logo views**: ~$0.10/month
- **1,000 tenants, 100K views**: ~$1/month
- **10,000 tenants, 1M views**: ~$5-10/month

## Features Implemented

✅ **Validation**
- File size: Max 2MB (optimal 500KB)
- Formats: PNG, JPG, SVG, WebP
- Dimensions: 100×100px min, 2000×2000px max
- MIME type verification (prevent spoofing)

✅ **Automatic Optimization**
- Images resized to 400×400px if larger
- PNG compression (best quality)
- JPEG quality 85%
- SVG uploaded as-is

✅ **Tenant Isolation**
- Each tenant gets own folder: `tenant-{id}/`
- No cross-tenant access

✅ **Fallback Mode**
- `NullLogoStorageService` used if no connection string
- Logs warnings but doesn't break app

## Security Best Practices

### 1. Use Azure Key Vault (Recommended for Production)
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 2. Use Managed Identity (Best Practice)
```csharp
// In Program.cs, replace connection string with:
builder.Services.AddSingleton(x => {
    var accountName = builder.Configuration["AzureStorage:AccountName"];
    var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
    return new BlobServiceClient(blobUri, new DefaultAzureCredential());
});
```

Then set in App Service:
- Enable **System-assigned managed identity**
- Grant **Storage Blob Data Contributor** role to App Service identity

### 3. Enable CDN (Optional - Better Performance)
1. Create Azure CDN endpoint pointing to storage account
2. Update `GetLogoUrlAsync` to return CDN URL instead of blob URL
3. First 100GB egress free with Azure CDN

## Monitoring

### View Uploaded Logos
Use **Azure Storage Explorer** (free desktop app) or Azure Portal → Storage Account → Containers → `tenant-logos`

### Cost Monitoring
Azure Portal → Cost Management → Cost Analysis → Filter by Resource: Your Storage Account

## Troubleshooting

### Logo not appearing?
1. Check blob container public access: Container → Change access level → **Blob (anonymous read access for blobs only)**
2. Verify connection string in Application Settings
3. Check logs: `ILogger<LogoStorageService>` for upload errors

### Development without Azure Storage?
The app will use `NullLogoStorageService` and log warnings. To test locally, use Azurite (Azure Storage Emulator):
```bash
npm install -g azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```
Connection string for Azurite:
```
UseDevelopmentStorage=true
```

## Migration from wwwroot

If you have existing logos in `/wwwroot/tenant-assets/`, create a migration script:

```csharp
public async Task MigrateLogosToBlob() {
    var tenants = await _db.Tenants.ToListAsync();
    foreach (var tenant in tenants) {
        var localPath = Path.Combine(_env.WebRootPath, "tenant-assets", $"tenant-{tenant.Id}");
        if (!Directory.Exists(localPath)) continue;
        
        var logoFile = Directory.GetFiles(localPath, "logo.*").FirstOrDefault();
        if (logoFile == null) continue;
        
        // Upload to blob
        var bytes = await File.ReadAllBytesAsync(logoFile);
        using var stream = new MemoryStream(bytes);
        // ... upload using LogoStorageService
    }
}
```

## Next Steps

1. **Add connection string to Azure App Service**
2. **Test upload in production**
3. **Optional: Enable Azure CDN for global performance**
4. **Optional: Add lifecycle policy to delete old logos after X days**

---

**Cost-saving tip**: Use **Cool tier** ($0.01/GB) if logos are accessed less than once per month per tenant.
