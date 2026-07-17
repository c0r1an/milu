namespace Milu.Web.Application.Modules.Media.Models;

public sealed class MediaAsset
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long Size { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
