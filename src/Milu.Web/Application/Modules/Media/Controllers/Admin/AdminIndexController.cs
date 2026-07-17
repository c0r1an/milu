using Milu.Web.Application.Modules.Media.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Infrastructure.Pagination;

namespace Milu.Web.Application.Modules.Media.Controllers.Admin;

[Area(MediaModule.ModuleArea)]
[Authorize]
[MiluPermission("media", PermissionOperation.ModuleView)]
public sealed class AdminIndexController(MiluDbContext database, IWebHostEnvironment environment,
    IPaginationSettings pagination) : Controller
{
    private string MediaDirectory => Path.Combine(environment.ContentRootPath, "App_Data", "media");

    [HttpGet]
    [MiluPermission("media", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index(string? q, bool picker = false, int page = 1)
    {
        var query = database.MediaAssets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(item => item.FileName.Contains(term) || item.Title.Contains(term) || item.AltText.Contains(term));
        }

        var assets = await query.OrderByDescending(item => item.CreatedAt)
            .ToPagedResultAsync(page, await pagination.GetPageSizeAsync("media"));
        var assetIds = assets.Items.Select(item => item.Id).ToArray();
        var counts = await database.MediaUsages.AsNoTracking().Where(item => assetIds.Contains(item.MediaAssetId)).GroupBy(item => item.MediaAssetId)
            .ToDictionaryAsync(group => group.Key, group => group.Count());
        return View(new MediaLibraryViewModel(assets.Items, counts, q?.Trim() ?? string.Empty, picker, assets.Page, assets.TotalPages));
    }

    [HttpPost]
    [RequestSizeLimit(104_857_600)]
    [MiluPermission("media", PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Upload(IFormFileCollection files)
    {
        if (files.Count == 0) return BadRequest(new { message = "Keine Dateien ausgewählt." });
        Directory.CreateDirectory(MediaDirectory);
        var added = new List<object>();
        foreach (var file in files.Where(item => item.Length > 0))
        {
            var originalName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalName);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            await using (var stream = System.IO.File.Create(Path.Combine(MediaDirectory, storedName)))
            {
                await file.CopyToAsync(stream);
            }

            var asset = new MediaAsset
            {
                FileName = originalName, StoredFileName = storedName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                Size = file.Length, Title = Path.GetFileNameWithoutExtension(originalName),
                CreatedAt = DateTime.UtcNow
            };
            database.MediaAssets.Add(asset);
            await database.SaveChangesAsync();
            added.Add(ToDto(asset));
        }
        return Ok(added);
    }

    [HttpGet]
    [MiluPermission("media", PermissionOperation.ContentView)]
    public async Task<IActionResult> Details(int id)
    {
        var asset = await database.MediaAssets.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (asset is null) return NotFound();
        var usages = await database.MediaUsages.AsNoTracking().Where(item => item.MediaAssetId == id)
            .OrderBy(item => item.ModuleKey).ThenBy(item => item.DisplayName).ToArrayAsync();
        return View(new MediaDetailsViewModel(asset, usages));
    }

    [HttpPost]
    [MiluPermission("media", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id, string title, string altText)
    {
        var asset = await database.MediaAssets.FindAsync(id);
        if (asset is null) return NotFound();
        asset.Title = (title ?? string.Empty).Trim();
        asset.AltText = (altText ?? string.Empty).Trim();
        await database.SaveChangesAsync();
        TempData["SuccessMessage"] = "Das Medium wurde aktualisiert.";
        return LocalRedirect($"/admin/media/index/details/id/{id}");
    }

    [HttpPost]
    [MiluPermission("media", PermissionOperation.ContentDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await database.MediaAssets.SingleOrDefaultAsync(item => item.Id == id);
        if (asset is null) return NotFound();
        if (await database.MediaUsages.AnyAsync(item => item.MediaAssetId == id))
            return Conflict(new { message = "Das Medium wird noch verwendet und kann nicht gelöscht werden." });
        database.MediaAssets.Remove(asset);
        await database.SaveChangesAsync();
        var path = Path.Combine(MediaDirectory, asset.StoredFileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return Ok();
    }

    private static object ToDto(MediaAsset asset) => new
    {
        asset.Id, asset.FileName, asset.Title, asset.AltText, asset.ContentType, asset.Size,
        url = $"/media/file/{asset.Id}", isImage = asset.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
    };
}
