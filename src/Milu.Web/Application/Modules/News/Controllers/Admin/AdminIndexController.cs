using Milu.Web.Application.Modules.News.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Application.Modules.Media.Services;
using Ganss.Xss;
using System.Text.RegularExpressions;
using Milu.Web.Infrastructure.Pagination;

namespace Milu.Web.Application.Modules.News.Controllers.Admin;

[Area(NewsModule.ModuleArea)]
[Authorize]
[MiluPermission("news", PermissionOperation.ModuleView)]
public sealed class AdminIndexController(MiluDbContext database, IMediaLibrary mediaLibrary, HtmlSanitizer htmlSanitizer,
    IPaginationSettings pagination) : Controller
{
    [HttpGet]
    [MiluPermission("news", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var articles = await database.NewsArticles
            .AsNoTracking()
            .OrderByDescending(article => article.PublishedAt)
            .ToPagedResultAsync(page, await pagination.GetPageSizeAsync("news"));

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
    public async Task<IActionResult> Create([FromForm] NewsInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var article = new NewsArticle
        {
            Title = input.Title.Trim(),
            Summary = input.Summary.Trim(),
            Content = htmlSanitizer.Sanitize(input.Content.Trim()),
            IsPublished = input.IsPublished,
            PublishedAt = DateTime.UtcNow,
            FeaturedMediaId = input.FeaturedMediaId
        };
        database.NewsArticles.Add(article);
        await database.SaveChangesAsync();
        await mediaLibrary.SetUsageAsync(article.FeaturedMediaId, "news", nameof(NewsArticle), article.Id.ToString(),
            nameof(NewsArticle.FeaturedMediaId), article.Title, $"/admin/news/index/edit/id/{article.Id}");
        await mediaLibrary.SyncUsagesAsync(FindEmbeddedMedia(article.Content), "news", nameof(NewsArticle), article.Id.ToString(),
            nameof(NewsArticle.Content), article.Title, $"/admin/news/index/edit/id/{article.Id}");

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
                IsPublished = article.IsPublished,
                FeaturedMediaId = article.FeaturedMediaId
            },
            article.PublishedAt));
    }

    [HttpPost]
    [MiluPermission("news", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] NewsInputModel input)
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
        article.Content = htmlSanitizer.Sanitize(input.Content.Trim());
        article.IsPublished = input.IsPublished;
        article.FeaturedMediaId = input.FeaturedMediaId;
        await database.SaveChangesAsync();
        await mediaLibrary.SetUsageAsync(article.FeaturedMediaId, "news", nameof(NewsArticle), article.Id.ToString(),
            nameof(NewsArticle.FeaturedMediaId), article.Title, $"/admin/news/index/edit/id/{article.Id}");
        await mediaLibrary.SyncUsagesAsync(FindEmbeddedMedia(article.Content), "news", nameof(NewsArticle), article.Id.ToString(),
            nameof(NewsArticle.Content), article.Title, $"/admin/news/index/edit/id/{article.Id}");

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
            await mediaLibrary.SetUsageAsync(null, "news", nameof(NewsArticle), article.Id.ToString(), nameof(NewsArticle.FeaturedMediaId), article.Title);
            await mediaLibrary.SyncUsagesAsync([], "news", nameof(NewsArticle), article.Id.ToString(), nameof(NewsArticle.Content), article.Title);
            database.NewsArticles.Remove(article);
            await database.SaveChangesAsync();
            TempData["SuccessMessage"] = "Der Newsbeitrag wurde gelöscht.";
        }

        return LocalRedirect("/admin/news/index/index");
    }

    private static IEnumerable<int> FindEmbeddedMedia(string html) =>
        Regex.Matches(html, "data-milu-media-id=[\\\"'](?<id>\\d+)[\\\"']", RegexOptions.IgnoreCase)
            .Select(match => int.Parse(match.Groups["id"].Value));
}
