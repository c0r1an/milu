namespace Milu.Web.Application.Modules.Media.Services;

public interface IMediaLibrary
{
    Task SetUsageAsync(int? mediaId, string moduleKey, string entityType, string entityId,
        string fieldName, string displayName, string? editUrl = null);
    Task SyncUsagesAsync(IEnumerable<int> mediaIds, string moduleKey, string entityType, string entityId,
        string fieldName, string displayName, string? editUrl = null);
}
