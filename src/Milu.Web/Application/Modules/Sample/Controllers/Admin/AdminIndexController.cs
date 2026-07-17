using Milu.Web.Application.Modules.Sample.Models;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Application.Modules.Sample.Controllers.Admin;

[Area(SampleModule.ModuleArea)]
[Authorize]
[MiluPermission("sample", PermissionOperation.ModuleView)]
public sealed class AdminIndexController : Controller
{
    [HttpGet]
    [MiluPermission("sample", PermissionOperation.ContentView)]
    public IActionResult Index()
    {
        return View(new AdminSampleViewModel(
            "Sample-Modul",
            User.Identity?.Name ?? "Unbekannt",
            DateTimeOffset.Now));
    }

    [HttpPost]
    [MiluPermission("sample", PermissionOperation.ContentEdit)]
    public IActionResult Save(string message)
    {
        TempData["SuccessMessage"] = string.IsNullOrWhiteSpace(message)
            ? "Die Admin-Aktion wurde ausgeführt."
            : $"Gespeichert: {message.Trim()}";

        return LocalRedirect("/admin/sample/index/index");
    }
}
