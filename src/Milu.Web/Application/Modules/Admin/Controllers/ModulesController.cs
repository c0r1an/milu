using Milu.Web.Application.Modules.Admin.Models;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Application.Modules.Admin.Controllers;

[Area(AdminModule.ModuleArea)]
[Authorize]
[MiluPermission("admin", PermissionOperation.ModuleView)]
[Route("admin/modules")]
public sealed class ModulesController(IModuleCatalog moduleCatalog) : Controller
{
    [HttpGet("")]
    [MiluPermission("admin", PermissionOperation.ContentView)]
    public IActionResult Index()
    {
        var modules = moduleCatalog.Modules
            .Select(module => new ModuleAdminViewModel(
                module.Key,
                module.DisplayName,
                module.Description,
                module.Version,
                module.FrontendRoute,
                module.AdminRoute,
                module.HasAdminArea))
            .ToArray();

        return View(
            "/Application/Modules/Admin/Views/Modules/Index.cshtml",
            modules);
    }
}
