using Milu.Web.Application.Modules.Guestbook.Models;
using Milu.Web.Application.Modules.News.Models;
using Milu.Web.Application.Modules.Media.Models;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Infrastructure.Layouts;

namespace Milu.Web.Infrastructure.Data;

public sealed class MiluDbContext(DbContextOptions<MiluDbContext> options)
    : DbContext(options)
{
    public DbSet<GuestbookEntry> GuestbookEntries => Set<GuestbookEntry>();

    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaUsage> MediaUsages => Set<MediaUsage>();
    public DbSet<MiluSetting> Settings => Set<MiluSetting>();
    public DbSet<LayoutInstallation> LayoutInstallations => Set<LayoutInstallation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MiluSetting>(entity =>
        {
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).HasMaxLength(120);
            entity.Property(item => item.Value).HasMaxLength(1000).IsRequired();
        });
        modelBuilder.Entity<LayoutInstallation>(entity =>
        {
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).HasMaxLength(80);
            entity.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500).IsRequired();
            entity.Property(item => item.ViewPath).HasMaxLength(500).IsRequired();
        });
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

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.Property(item => item.FileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(255).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(160).IsRequired();
            entity.Property(item => item.AltText).HasMaxLength(500).IsRequired();
            entity.HasIndex(item => item.CreatedAt);
        });

        modelBuilder.Entity<MediaUsage>(entity =>
        {
            entity.Property(item => item.ModuleKey).HasMaxLength(80).IsRequired();
            entity.Property(item => item.EntityType).HasMaxLength(120).IsRequired();
            entity.Property(item => item.EntityId).HasMaxLength(120).IsRequired();
            entity.Property(item => item.FieldName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(255).IsRequired();
            entity.Property(item => item.EditUrl).HasMaxLength(500);
            entity.HasIndex(item => new { item.MediaAssetId, item.ModuleKey, item.EntityType, item.EntityId, item.FieldName }).IsUnique();
            entity.HasOne(item => item.MediaAsset).WithMany().HasForeignKey(item => item.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
