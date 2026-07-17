namespace Milu.Web.Application.Modules.News.Models;

public sealed class NewsArticle
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public bool IsPublished { get; set; }
}
