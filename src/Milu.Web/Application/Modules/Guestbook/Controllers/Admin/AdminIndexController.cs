using Milu.Web.Application.Modules.Guestbook.Models;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Infrastructure.Pagination;

namespace Milu.Web.Application.Modules.Guestbook.Controllers.Admin;

[Area(GuestbookModule.ModuleArea)]
[Authorize]
[MiluPermission("guestbook", PermissionOperation.ModuleView)]
public sealed class AdminIndexController(MiluDbContext database, IPaginationSettings pagination) : Controller
{
    [HttpGet]
    [MiluPermission("guestbook", PermissionOperation.ContentView)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var entries = await database.GuestbookEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .ToPagedResultAsync(page, await pagination.GetPageSizeAsync("guestbook"));

        return View(entries);
    }

    [HttpGet]
    [MiluPermission("guestbook", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var entry = await database.GuestbookEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (entry is null)
        {
            return NotFound();
        }

        return View(new GuestbookEditViewModel(
            entry.Id,
            new GuestbookInputModel
            {
                Name = entry.Name,
                Message = entry.Message
            },
            entry.CreatedAt));
    }

    [HttpPost]
    [MiluPermission("guestbook", PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(int id, GuestbookInputModel input)
    {
        var entry = await database.GuestbookEntries.FindAsync(id);
        if (entry is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(new GuestbookEditViewModel(id, input, entry.CreatedAt));
        }

        entry.Name = input.Name.Trim();
        entry.Message = input.Message.Trim();
        await database.SaveChangesAsync();

        TempData["SuccessMessage"] = "Der Gästebucheintrag wurde aktualisiert.";
        return LocalRedirect("/admin/guestbook/index/index");
    }

    [HttpPost]
    [MiluPermission("guestbook", PermissionOperation.ContentDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await database.GuestbookEntries.FindAsync(id);
        if (entry is not null)
        {
            database.GuestbookEntries.Remove(entry);
            await database.SaveChangesAsync();
            TempData["SuccessMessage"] = "Der Gästebucheintrag wurde gelöscht.";
        }

        return LocalRedirect("/admin/guestbook/index/index");
    }
}
