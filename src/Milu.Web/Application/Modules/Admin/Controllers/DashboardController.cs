using Milu.Web.Application.Modules.Admin.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.Admin.Controllers;

[Area(AdminModule.ModuleArea)]
[Authorize]
[MiluPermission("admin", PermissionOperation.ModuleView)]
[Route("admin")]
public sealed class DashboardController(
    IModuleCatalog moduleCatalog,
    MiluDbContext database,
    MiluIdentityDbContext identityDatabase) : Controller
{
    [HttpGet("")]
    [MiluPermission("admin", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel(
            moduleCatalog.Modules.Count,
            await database.GuestbookEntries.CountAsync(),
            await database.NewsArticles.CountAsync(),
            await identityDatabase.Users.CountAsync(),
            await identityDatabase.Roles.CountAsync());

        return View(
            "/Application/Modules/Admin/Views/Dashboard/Index.cshtml",
            model);
    }
}
