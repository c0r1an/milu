namespace Milu.Web.Application.Modules.News.Models;

public sealed record NewsEditViewModel(
    int Id,
    NewsInputModel Input,
    DateTime PublishedAt);
