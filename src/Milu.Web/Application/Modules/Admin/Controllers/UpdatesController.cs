using Milu.Web.Application.Modules.Admin.Models;
using Milu.Web.Infrastructure.Security;
using Milu.Web.Infrastructure.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Application.Modules.Admin.Controllers;

[Area(AdminModule.ModuleArea)]
[Authorize]
[MiluPermission("admin", PermissionOperation.ModuleView)]
[Route("admin/updates")]
public sealed class UpdatesController(IMiluUpdateService updates, IMiluUpdateInstaller installer) : Controller
{
    [HttpGet("")]
    [MiluPermission("admin", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index(bool refresh = false) => View(
        "/Application/Modules/Admin/Views/Updates/Index.cshtml",
        new UpdateAdminViewModel(
            await updates.GetRepositoryAsync(HttpContext.RequestAborted) ?? string.Empty,
            await updates.CheckAsync(refresh, HttpContext.RequestAborted)));

    [HttpPost("install")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Install(string version, bool confirmInstallation)
    {
        if (!confirmInstallation)
        {
            TempData["ErrorMessage"] = "Die Installation wurde nicht ausdrücklich bestätigt.";
            return RedirectToAction(nameof(Index));
        }
        var release = await updates.CheckAsync(true, HttpContext.RequestAborted);
        if (!string.Equals(release.LatestVersion?.TrimStart('v', 'V'), version.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Das bestätigte Release stimmt nicht mehr mit dem neuesten Release überein. Bitte erneut prüfen.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await installer.PrepareAndStartAsync(release, HttpContext.RequestAborted);
            return Content("Update wurde geprüft. Milu wird beendet, aktualisiert und neu gestartet.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or PlatformNotSupportedException or HttpRequestException)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

}
