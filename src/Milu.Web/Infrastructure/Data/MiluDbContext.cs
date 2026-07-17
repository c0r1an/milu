using Milu.Web.Application.Modules.Guestbook.Models;
using Milu.Web.Application.Modules.News.Models;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Data;

public sealed class MiluDbContext(DbContextOptions<MiluDbContext> options)
    : DbContext(options)
{
    public DbSet<GuestbookEntry> GuestbookEntries => Set<GuestbookEntry>();

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GuestbookEntry>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(item => item.CreatedAt);
        });

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Summary).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Content).HasMaxLength(10000).IsRequired();
            entity.HasIndex(item => new { item.IsPublished, item.PublishedAt });
        });
    }
}
