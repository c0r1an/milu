using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Infrastructure.Pagination;

namespace Milu.Web.Application.Modules.News.Controllers;

[Area(NewsModule.ModuleArea)]
[MiluPermission("news", PermissionOperation.ModuleView)]
public sealed class IndexController(MiluDbContext database, IPaginationSettings pagination) : Controller
{
    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await database.NewsArticles
            .AsNoTracking()
            .Where(article => article.IsPublished)
            .OrderByDescending(article => article.PublishedAt)
            .ToPagedResultAsync(page, await pagination.GetPageSizeAsync("news"));

        return View(articles);
    }

    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentView)]
    public async Task<IActionResult> Details(int id)
    {
        var article = await database.NewsArticles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.IsPublished);

        return article is null ? NotFound() : View(article);
    }
}
