namespace Milu.Web.Application.Modules.Media.Models;

public sealed class MediaUsage
{
    public int Id { get; set; }
    public int MediaAssetId { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? EditUrl { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;
}
