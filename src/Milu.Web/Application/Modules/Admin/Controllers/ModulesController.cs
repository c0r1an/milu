using Milu.Web.Application.Modules.Admin.Models;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Milu.Web.Infrastructure.Routing;
using Milu.Web.Infrastructure.Pagination;

namespace Milu.Web.Application.Modules.Admin.Controllers;

[Area(AdminModule.ModuleArea)]
[Authorize]
[MiluPermission("admin", PermissionOperation.ModuleView)]
[Route("admin/modules")]
public sealed class ModulesController(
    IModuleCatalog moduleCatalog,
    IStartPageResolver startPageResolver,
    MiluRouteParser routeParser,
    IPaginationSettings paginationSettings) : Controller
{
    [HttpGet("")]
    [MiluPermission("admin", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var modules = moduleCatalog.Modules
            .Select(module => new ModuleAdminViewModel(
                module.Key,
                module.DisplayName,
                module.Description,
                module.Version,
                module.FrontendRoute,
                module.AdminRoute,
                module.HasAdminArea,
                module.IsCoreModule))
            .ToArray();

        var overrides = await paginationSettings.GetModuleOverridesAsync(HttpContext.RequestAborted);
        return View(
            "/Application/Modules/Admin/Views/Modules/Index.cshtml",
            new ModuleManagementViewModel(
                modules,
                modules.Where(module => !module.IsCoreModule).ToArray(),
                await startPageResolver.ResolveAsync(HttpContext.RequestAborted),
                await paginationSettings.GetDefaultPageSizeAsync(HttpContext.RequestAborted),
                overrides));
    }

    [HttpPost("start-page")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> SetStartPage(string moduleKey, string? subPath)
    {
        var route = string.Join('/', new[] { moduleKey, subPath?.Trim().Trim('/') }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        var parsed = routeParser.Parse(route);
        if (parsed is null || parsed.IsAdmin || !parsed.Module.Equals(moduleKey, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Die angegebene Unterseite ist keine gültige Milu-Route.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await startPageResolver.SetAsync(route, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Die Startseite wurde gespeichert.";
        }
        catch (ArgumentException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("pagination")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> SetPagination(int defaultPageSize, Dictionary<string, string?> modulePageSizes)
    {
        if (defaultPageSize is < 1 or > 100)
        {
            TempData["ErrorMessage"] = "Der globale Seitenumfang muss zwischen 1 und 100 liegen.";
            return RedirectToAction(nameof(Index));
        }
        var overrides = moduleCatalog.Modules.ToDictionary(
            module => module.Key,
            module => modulePageSizes.TryGetValue(module.Key, out var raw) && int.TryParse(raw, out var value)
                ? (int?)Math.Clamp(value, 1, 100) : null,
            StringComparer.OrdinalIgnoreCase);
        await paginationSettings.SaveAsync(defaultPageSize, overrides, HttpContext.RequestAborted);
        TempData["SuccessMessage"] = "Die Paginationseinstellungen wurden gespeichert.";
        return RedirectToAction(nameof(Index));
    }
}
