using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Horizon.Services.Tenant;

namespace Horizon.Controllers {
 [ApiController]
 [Route("api/tenant/{tenantId:int}/media")]
 public class MediaController : ControllerBase
 {
 private readonly IWebHostEnvironment _env;
 private readonly ITenantContext _tenantContext;
 private readonly ILogger<MediaController> _logger;

 public MediaController(IWebHostEnvironment env, ITenantContext tenantContext, ILogger<MediaController> logger)
 {
 _env = env;
 _tenantContext = tenantContext;
 _logger = logger;
 }

 [HttpPost]
 [Authorize]
 public async Task<IActionResult> UploadMedia(int tenantIdFromRoute, [FromRoute] int tenantId, [FromForm] List<IFormFile> files)
 {
 // Ensure route tenant matches resolved tenant
 var resolved = _tenantContext.TenantId ?? tenantIdFromRoute;
 if (tenantIdFromRoute != resolved) return Forbid();

 if (files == null || files.Count==0) return BadRequest("No files");
 var webroot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
 var dest = Path.Combine(webroot, "tenant-assets", $"tenant-{tenantIdFromRoute}", "media");
 Directory.CreateDirectory(dest);
 var urls = new List<string>();
 foreach (var f in files)
 {
 var ext = Path.GetExtension(f.FileName);
 var name = $"{Guid.NewGuid()}{ext}";
 var path = Path.Combine(dest, name);
 await using var s = System.IO.File.Create(path);
 await f.CopyToAsync(s);
 urls.Add($"/tenant-assets/tenant-{tenantIdFromRoute}/media/{name}");
 }
 return Ok(urls);
 }
 }
}
