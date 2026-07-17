using Milu.Web.Application.Modules.Guestbook.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.Guestbook.Controllers;

[Area(GuestbookModule.ModuleArea)]
[MiluPermission("guestbook", PermissionOperation.ModuleView)]
public sealed class IndexController(MiluDbContext database) : Controller
{
    [HttpGet]
    [MiluPermission("guestbook", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var entries = await database.GuestbookEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .ToArrayAsync();

        return View(entries);
    }

    [HttpGet]
    [MiluPermission("guestbook", PermissionOperation.ContentCreate)]
    public IActionResult Create()
    {
        return View(new GuestbookInputModel());
    }

    [HttpPost]
    [MiluPermission("guestbook", PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Create(GuestbookInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        database.GuestbookEntries.Add(new GuestbookEntry
        {
            Name = input.Name.Trim(),
            Message = input.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "Vielen Dank für deinen Gästebucheintrag.";
        return LocalRedirect("/guestbook/index/index");
    }
}
