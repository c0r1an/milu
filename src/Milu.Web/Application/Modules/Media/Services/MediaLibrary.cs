using Milu.Web.Application.Modules.Media.Models;
using Milu.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.Media.Services;

public sealed class MediaLibrary(MiluDbContext database) : IMediaLibrary
{
    public async Task SetUsageAsync(int? mediaId, string moduleKey, string entityType, string entityId,
        string fieldName, string displayName, string? editUrl = null)
    {
        var existing = await database.MediaUsages.Where(item =>
            item.ModuleKey == moduleKey && item.EntityType == entityType &&
            item.EntityId == entityId && item.FieldName == fieldName).ToListAsync();
        database.MediaUsages.RemoveRange(existing);

        if (mediaId.HasValue && await database.MediaAssets.AnyAsync(item => item.Id == mediaId.Value))
        {
            database.MediaUsages.Add(new MediaUsage
            {
                MediaAssetId = mediaId.Value, ModuleKey = moduleKey, EntityType = entityType,
                EntityId = entityId, FieldName = fieldName, DisplayName = displayName, EditUrl = editUrl
            });
        }

        await database.SaveChangesAsync();
    }

    public async Task SyncUsagesAsync(IEnumerable<int> mediaIds, string moduleKey, string entityType,
        string entityId, string fieldName, string displayName, string? editUrl = null)
    {
        var existing = await database.MediaUsages.Where(item => item.ModuleKey == moduleKey &&
            item.EntityType == entityType && item.EntityId == entityId && item.FieldName == fieldName).ToListAsync();
        database.MediaUsages.RemoveRange(existing);
        var ids = mediaIds.Distinct().ToArray();
        var validIds = await database.MediaAssets.Where(item => ids.Contains(item.Id)).Select(item => item.Id).ToArrayAsync();
        foreach (var id in validIds)
        {
            database.MediaUsages.Add(new MediaUsage
            {
                MediaAssetId = id, ModuleKey = moduleKey, EntityType = entityType, EntityId = entityId,
                FieldName = fieldName, DisplayName = displayName, EditUrl = editUrl
            });
        }
        await database.SaveChangesAsync();
    }
}
