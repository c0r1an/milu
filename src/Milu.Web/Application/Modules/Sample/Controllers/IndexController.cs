using Milu.Web.Application.Modules.Sample.Models;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Milu.Web.Application.Modules.Sample.Controllers;

[Area(SampleModule.ModuleArea)]
[MiluPermission("sample", PermissionOperation.ModuleView)]
public sealed class IndexController(IModuleCatalog moduleCatalog) : Controller
{
    [HttpGet]
    [MiluPermission("sample", PermissionOperation.ContentView)]
    public IActionResult Index()
    {
        var module = moduleCatalog.Modules.Single(item => item.Key == "sample");
        var registeredModules = moduleCatalog.Modules
            .OrderBy(item => item.DisplayName)
            .Select(item => new ModuleSummary(
                item.Key,
                item.DisplayName,
                item.Version))
            .ToArray();

        return View(new SampleIndexViewModel(
            module.DisplayName,
            module.Version,
            registeredModules));
    }

    [HttpGet]
    [MiluPermission("sample", PermissionOperation.ContentView)]
    public IActionResult Hello(string name = "Welt")
    {
        var safeName = name.Length > 40 ? name[..40] : name;
        return View(new HelloViewModel(
            safeName,
            HttpContext.Request.Path));
    }
}
