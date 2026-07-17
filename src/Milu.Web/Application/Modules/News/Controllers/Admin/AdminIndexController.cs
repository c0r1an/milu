using Milu.Web.Application.Modules.News.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.News.Controllers.Admin;

[Area(NewsModule.ModuleArea)]
[Authorize]
[MiluPermission("news", PermissionOperation.ModuleView)]
public sealed class AdminIndexController(MiluDbContext database) : Controller
{
    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var articles = await database.NewsArticles
            .AsNoTracking()
            .OrderByDescending(article => article.PublishedAt)
            .ToArrayAsync();

        return View(articles);
    }

    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentCreate)]
    public IActionResult Create()
    {
        return View(new NewsInputModel());
    }

    [HttpPost]
    [MiluPermission("news", PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Create(NewsInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        database.NewsArticles.Add(new NewsArticle
        {
            Title = input.Title.Trim(),
            Summary = input.Summary.Trim(),
            Content = input.Content.Trim(),
            IsPublished = input.IsPublished,
            PublishedAt = DateTime.UtcNow
        });
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "Der Newsbeitrag wurde erstellt.";
        return LocalRedirect("/admin/news/index/index");
    }

    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await database.NewsArticles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (article is null)
        {
            return NotFound();
        }

        return View(new NewsEditViewModel(
            article.Id,
            new NewsInputModel
            {
                Title = article.Title,
                Summary = article.Summary,
                Content = article.Content,
                IsPublished = article.IsPublished
            },
            article.PublishedAt));
    }

    [HttpPost]
    [MiluPermission("news", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id, NewsInputModel input)
    {
        var article = await database.NewsArticles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(new NewsEditViewModel(id, input, article.PublishedAt));
        }

        article.Title = input.Title.Trim();
        article.Summary = input.Summary.Trim();
        article.Content = input.Content.Trim();
        article.IsPublished = input.IsPublished;
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "Der Newsbeitrag wurde aktualisiert.";
        return LocalRedirect("/admin/news/index/index");
    }

    [HttpPost]
    [MiluPermission("news", PermissionOperation.ContentDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await database.NewsArticles.FindAsync(id);
        if (article is not null)
        {
            database.NewsArticles.Remove(article);
            await database.SaveChangesAsync();
            TempData["SuccessMessage"] = "Der Newsbeitrag wurde gelöscht.";
        }

        return LocalRedirect("/admin/news/index/index");
    }
}
