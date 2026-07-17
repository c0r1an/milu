namespace Milu.Web.Application.Modules.Sample.Models;

public sealed record AdminSampleViewModel(
    string ModuleName,
    string CurrentUser,
    DateTimeOffset ServerTime);
