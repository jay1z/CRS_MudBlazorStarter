using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Horizon.Services.Tenant;
using Microsoft.AspNetCore.Authorization;

namespace Horizon.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UploadController : ControllerBase {
        private readonly IWebHostEnvironment _env;
        private readonly ITenantContext _tenantContext;
        private static readonly string[] permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; //5 MB

        public UploadController(IWebHostEnvironment env, ITenantContext tenantContext) {
            _env = env;
            _tenantContext = tenantContext;
        }

        [HttpPost]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> Post(IFormFile? file) {
            if (file == null) return BadRequest(new { error = "No file provided" });
            if (file.Length == 0) return BadRequest(new { error = "Empty file" });
            if (file.Length > MaxFileSize) return BadRequest(new { error = "File too large" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!permittedExtensions.Contains(ext)) return BadRequest(new { error = "Invalid file type" });

            var user = User.Identity?.Name ?? "unknown";
            var tenantId = _tenantContext.TenantId ?? 1; // fallback to default tenant
            var tenantFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "tenant-assets", $"tenant-{tenantId}");
            if (!Directory.Exists(tenantFolder)) Directory.CreateDirectory(tenantFolder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var filePath = Path.Combine(tenantFolder, fileName);

            try {
                await using var stream = System.IO.File.Create(filePath);
                await file.CopyToAsync(stream);
            } catch (Exception ex) {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }

            var relativeUrl = Url.Content($"~/tenant-assets/tenant-{tenantId}/{fileName}");
            return Ok(new { url = relativeUrl });
        }

        [HttpGet]
        public IActionResult Get() {
            var tenantId = _tenantContext.TenantId ?? 1;
            var tenantFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "tenant-assets", $"tenant-{tenantId}");
            if (!Directory.Exists(tenantFolder)) Directory.CreateDirectory(tenantFolder);

            var files = Directory.EnumerateFiles(tenantFolder)
                .Select(Path.GetFileName)
                .Where(n => permittedExtensions.Contains(Path.GetExtension(n).ToLowerInvariant()))
                .OrderByDescending(n => n)
                .Select(n => Url.Content($"~/tenant-assets/tenant-{tenantId}/{n}"))
                .ToArray();

            return Ok(new { files });
        }
    }
}
