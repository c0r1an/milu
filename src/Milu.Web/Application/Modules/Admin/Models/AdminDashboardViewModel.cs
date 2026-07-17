namespace Milu.Web.Application.Modules.Admin.Models;

public sealed record AdminDashboardViewModel(
    int ModuleCount,
    int GuestbookEntryCount,
    int NewsArticleCount,
    int UserCount,
    int GroupCount);
