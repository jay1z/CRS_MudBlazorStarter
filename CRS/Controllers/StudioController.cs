using Horizon.Services;
using Horizon.Services.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Ganss.Xss;
using Microsoft.Extensions.Logging;

namespace Horizon.Controllers {
    [ApiController]
    [Route("api/studio/projects")]
    [Authorize(Roles = "Admin,Editor")]
    public class StudioController : ControllerBase {
        private readonly TenantHomepageService _homepageService;
        private readonly ITenantContext _tenantContext;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StudioController> _logger;

        public StudioController(TenantHomepageService homepageService, ITenantContext tenantContext, IWebHostEnvironment env, ILogger<StudioController> logger) {
            _homepageService = homepageService;
            _tenantContext = tenantContext;
            _env = env;
            _logger = logger;
        }

        // GET api/studio/projects/load?tenantId=1
        [HttpGet("load")]
        public async Task<IActionResult> Load([FromQuery] int? tenantId) {
            var id = tenantId ?? _tenantContext.TenantId ?? 0;
            if (id == 0) {
                return BadRequest("tenantId is required");
            }

            var homepage = await _homepageService.GetByTenantIdAsync(id);
            if (homepage == null) {
                return NotFound();
            }

            // Prefer draft HTML for editing; fall back to published HTML
            var html = homepage.DraftHtml ?? homepage.PublishedHtml ?? string.Empty;

            // Sanitize returned HTML for Studio: remove full-document wrappers and scripts
            if (!string.IsNullOrWhiteSpace(html)) {
                try {
                    // Remove <script>...</script>
                    html = Regex.Replace(html, "<script[\\s\\S]*?>[\\s\\S]*?<\\/script>", string.Empty, RegexOptions.IgnoreCase);
                    // If there's a <body>, extract inner HTML
                    var bodyMatch = Regex.Match(html, "<body[^>]*>([\\s\\S]*?)<\\/body>", RegexOptions.IgnoreCase);
                    if (bodyMatch.Success) {
                        html = bodyMatch.Groups[1].Value;
                    } else {
                        // If it's a full HTML document without body tags (unlikely), remove DOCTYPE and html/head tags
                        html = Regex.Replace(html, "<!DOCTYPE[^>]*>", string.Empty, RegexOptions.IgnoreCase).Trim();
                        html = Regex.Replace(html, "<html[\\s\\S]*?>", string.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "<\\/html>", string.Empty, RegexOptions.IgnoreCase);
                        html = Regex.Replace(html, "<head[\\s\\S]*?>[\\s\\S]*?<\\/head>", string.Empty, RegexOptions.IgnoreCase);
                    }
                } catch {
                    // If any parsing fails, fall back to empty fragment to avoid breaking the editor
                    html = string.Empty;
                }
            }

            var project = new {
                project = new {
                    type = "web",
                    pages = new[] {
                        new {
                            id = "home",
                            name = "Home",
                            components = html
                        }
                    },
                    activePageId = "home"
                }
            };

            return Ok(project);
        }

        public record SaveProjectRequest(string? Name, JsonElement? Project, string? Html, string? TemplateName, string? MetaTitle, string? MetaDescription);

        // POST api/studio/projects/save - save draft to TenantHomepage (DB)
        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveProjectRequest? req) {
            if (req == null) {
                return BadRequest("request body is required");
            }

            var tenantId = _tenantContext.TenantId ?? 0;
            if (tenantId == 0) {
                return BadRequest("Tenant context not set");
            }

            // Serialize project JSON if provided
            string? projectJson = null;
            try {
                if (req.Project.HasValue) {
                    projectJson = JsonSerializer.Serialize(req.Project.Value);
                }
            } catch {
                // ignore serialization errors
                projectJson = null;
            }

            // Build TenantHomepage model for saving
            var homepage = new Horizon.Models.TenantHomepage {
                DraftJson = projectJson ?? req.Html,
                DraftHtml = req.Html,
                MetaTitle = req.MetaTitle,
                MetaDescription = req.MetaDescription,
                TemplateName = req.TemplateName
            };

            var saved = await _homepageService.SaveDraftAsync(homepage, modifiedBy: User?.Identity?.Name);

            var url = $"/tenant/preview/{(saved?.TenantId ?? tenantId)}";
            return Ok(new { success = true, url });
        }

        // POST api/studio/projects/assets/upload
        [HttpPost("assets/upload")]
        [RequestSizeLimit(20_000_000)] //20 MB limit (adjust as needed)
        public async Task<IActionResult> UploadAssets([FromForm] List<IFormFile> files) {
            if (files == null || files.Count == 0) return BadRequest("No files uploaded.");

            var tenantId = _tenantContext.TenantId ?? 1;
            var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var destDir = Path.Combine(webroot, "tenant-assets", $"tenant-{tenantId}", "studio");
            Directory.CreateDirectory(destDir);

            var results = new List<object>();

            foreach (var file in files) {
                try {
                    var ext = Path.GetExtension(file.FileName);
                    var allowed = new[] {
                        ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp", ".pdf", ".mp4", ".mov"
                    };
                    if (!allowed.Contains(ext.ToLowerInvariant())) {
                        _logger.LogWarning("Rejected upload {FileName} due to unsupported extension", file.FileName);
                        continue;
                    }

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(destDir, fileName);
                    await using var stream = System.IO.File.Create(filePath);
                    await file.CopyToAsync(stream);

                    var publicUrl = $"/tenant-assets/tenant-{tenantId}/studio/{fileName}";
                    results.Add(new { src = publicUrl, name = file.FileName });
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error saving uploaded asset");
                }
            }

            return Ok(results);
        }

        // DELETE api/studio/projects/assets
        [HttpDelete("assets")]
        public IActionResult DeleteAssets([FromBody] List<string> assets) {
            if (assets == null || assets.Count == 0) return BadRequest("No assets specified");

            var tenantId = _tenantContext.TenantId ?? 1;
            var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

            foreach (var asset in assets) {
                try {
                    var relative = asset.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
                    var path = Path.Combine(webroot, relative);
                    if (System.IO.File.Exists(path) && path.Contains($"tenant-{tenantId}{Path.DirectorySeparatorChar}studio")) {
                        System.IO.File.Delete(path);
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error deleting asset {Asset}", asset);
                }
            }

            return NoContent();
        }

        // POST api/studio/projects/save-export - save sanitized HTML and components JSON to studio-exports directory
        public record SaveStudioRequest(string Name, JsonElement Project, string Html);

        [HttpPost("save-export")]
        public async Task<IActionResult> SaveProjectExport([FromBody] SaveStudioRequest req) {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Html))
                return BadRequest("Name and Html are required");

            var tenantId = _tenantContext.TenantId ?? 1;
            var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

            var sanitizer = new HtmlSanitizer();
            var sanitized = sanitizer.Sanitize(req.Html);

            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ' }).ToArray();
            var slug = string.Join('-', req.Name.ToLowerInvariant().Split(invalid))
                .Replace("--", "-")
                .Trim('-');
            if (string.IsNullOrWhiteSpace(slug)) slug = "project";

            var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var dir = Path.Combine(webroot, "studio-exports", $"tenant-{tenantId}", slug);
            Directory.CreateDirectory(dir);

            var htmlPath = Path.Combine(dir, ts + ".html");
            await System.IO.File.WriteAllTextAsync(htmlPath, sanitized);

            if (req.Project.ValueKind != JsonValueKind.Undefined) {
                var jsonPath = Path.Combine(dir, ts + ".json");
                await System.IO.File.WriteAllTextAsync(jsonPath, req.Project.GetRawText());
            }

            var url = $"/studio-exports/tenant-{tenantId}/{slug}/{ts}.html";
            return Ok(new { name = req.Name, url, created = DateTime.UtcNow });
        }

        // POST api/studio/projects/export-template
        [HttpPost("export-template")]
        public async Task<IActionResult> ExportTemplate() {
            var tenantId = _tenantContext.TenantId ?? 0;
            if (tenantId == 0) return BadRequest("Tenant context not set");

            var homepage = await _homepageService.GetByTenantIdAsync(tenantId);
            if (homepage == null) return BadRequest("No draft found for current tenant");

            // Use DraftJson if available, otherwise DraftHtml
            var content = homepage.DraftJson ?? homepage.DraftHtml ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) return BadRequest("No draft content to export");

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var templatesDir = Path.Combine(webRoot, "templates");
            Directory.CreateDirectory(templatesDir);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"studio-template-tenant{tenantId}-{timestamp}.json";
            var filePath = Path.Combine(templatesDir, fileName);

            // Save the raw project JSON (if DraftJson looks like JSON) else save HTML as .html
            try {
                if (homepage.DraftJson != null) {
                    await System.IO.File.WriteAllTextAsync(filePath, homepage.DraftJson, Encoding.UTF8);
                    var publicUrl = $"/templates/{fileName}";
                    return Ok(new { success = true, url = publicUrl });
                } else {
                    // save as html file
                    var htmlName = $"studio-template-tenant{tenantId}-{timestamp}.html";
                    var htmlPath = Path.Combine(templatesDir, htmlName);
                    await System.IO.File.WriteAllTextAsync(htmlPath, homepage.DraftHtml ?? string.Empty, Encoding.UTF8);
                    var publicUrl = $"/templates/{htmlName}";
                    return Ok(new { success = true, url = publicUrl });
                }
            } catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        // GET api/studio/projects/list/{slug}
        [HttpGet("list/{slug}")]
        public IActionResult ListExports(string slug) {
            var tenantId = _tenantContext.TenantId ?? 1;
            var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var dir = Path.Combine(webroot, "studio-exports", $"tenant-{tenantId}", slug);
            if (!Directory.Exists(dir)) return NotFound();

            var files = Directory.GetFiles(dir, "*.html").Select(f => new
            {
                file = Path.GetFileName(f),
                url = $"/studio-exports/tenant-{tenantId}/{slug}/{Path.GetFileName(f)}",
                created = System.IO.File.GetCreationTimeUtc(f)
            }).OrderByDescending(x => x.created);

            return Ok(files);
        }
    }
}
