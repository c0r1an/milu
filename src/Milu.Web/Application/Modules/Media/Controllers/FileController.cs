using Milu.Web.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.Media.Controllers;

[Area(MediaModule.ModuleArea)]
public sealed class FileController(MiluDbContext database, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int id)
    {
        var asset = await database.MediaAssets.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (asset is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "media", asset.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        var safeInline = asset.ContentType.StartsWith("image/") || asset.ContentType.StartsWith("video/") ||
                         asset.ContentType.StartsWith("audio/") || asset.ContentType == "application/pdf";
        return safeInline ? PhysicalFile(path, asset.ContentType) : PhysicalFile(path, asset.ContentType, asset.FileName);
    }
}
