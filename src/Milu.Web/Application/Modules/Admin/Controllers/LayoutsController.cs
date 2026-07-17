using Milu.Web.Infrastructure.Layouts;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Application.Modules.Admin.Controllers;

[Area(AdminModule.ModuleArea)]
[Authorize]
[MiluPermission("admin", PermissionOperation.ModuleView)]
[Route("admin/layouts")]
public sealed class LayoutsController(ILayoutManager layouts) : Controller
{
    [HttpGet("")]
    [MiluPermission("admin", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index() => View(
        "/Application/Modules/Admin/Views/Layouts/Index.cshtml",
        (await layouts.GetLayoutsAsync(HttpContext.RequestAborted)).Where(item => item.IsInstalled).ToArray());

    [HttpPost("activate/{key}")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public Task<IActionResult> Activate(string key) => ExecuteAsync(() => layouts.ActivateAsync(key, HttpContext.RequestAborted), "Layout aktiviert.");

    [HttpPost("enable/{key}")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public Task<IActionResult> Enable(string key) => ExecuteAsync(() => layouts.SetEnabledAsync(key, true, HttpContext.RequestAborted), "Layout aktiviert.");

    [HttpPost("disable/{key}")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public Task<IActionResult> Disable(string key) => ExecuteAsync(() => layouts.SetEnabledAsync(key, false, HttpContext.RequestAborted), "Layout deaktiviert.");

    [HttpPost("update/{key}")]
    [MiluPermission("admin", PermissionOperation.ContentEdit)]
    public Task<IActionResult> Update(string key) => ExecuteAsync(() => layouts.UpdateAsync(key, HttpContext.RequestAborted), "Layout aktualisiert.");

    [HttpPost("delete/{key}")]
    [MiluPermission("admin", PermissionOperation.ContentDelete)]
    public Task<IActionResult> Delete(string key) => ExecuteAsync(() => layouts.DeleteAsync(key, HttpContext.RequestAborted), "Layout gelöscht.");

    private async Task<IActionResult> ExecuteAsync(Func<Task> action, string success)
    {
        try { await action(); TempData["SuccessMessage"] = success; }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        { TempData["ErrorMessage"] = exception.Message; }
        return RedirectToAction(nameof(Index));
    }
}
