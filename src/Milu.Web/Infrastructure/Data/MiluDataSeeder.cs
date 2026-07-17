using Milu.Web.Application.Modules.Guestbook.Models;
using Milu.Web.Application.Modules.News.Models;

namespace Milu.Web.Infrastructure.Data;

public static class MiluDataSeeder
{
    public static void Seed(MiluDbContext database)
    {
        if (!database.GuestbookEntries.Any())
        {
            database.GuestbookEntries.Add(new GuestbookEntry
            {
                Name = "Milu-Team",
                Message = "Willkommen im modularen Milu-Gästebuch!",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!database.NewsArticles.Any())
        {
            database.NewsArticles.Add(new NewsArticle
            {
                Title = "Milu ist modular",
                Summary = "News und Gästebuch wurden automatisch als Module registriert.",
                Content = "Jedes Modul bringt seine Controller, Views und Verwaltungsseiten selbst mit.",
                PublishedAt = DateTime.UtcNow,
                IsPublished = true
            });
        }

        database.SaveChanges();
    }
}
