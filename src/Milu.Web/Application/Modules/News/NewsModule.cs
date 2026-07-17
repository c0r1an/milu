using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.News;

public sealed class NewsModule : IMiluModule
{
    public const string ModuleArea = "News";

    public string Key => "news";

    public string DisplayName => "News";

    public string AreaName => ModuleArea;

    public string FolderName => "News";

    public Version Version => new(1, 0, 0);

    public string Description => "Newsbeiträge veröffentlichen und im Adminbereich verwalten.";

    public int SortOrder => 10;
}
