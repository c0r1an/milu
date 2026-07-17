using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.Guestbook;

public sealed class GuestbookModule : IMiluModule
{
    public const string ModuleArea = "Guestbook";

    public string Key => "guestbook";

    public string DisplayName => "Gästebuch";

    public string AreaName => ModuleArea;

    public string FolderName => "Guestbook";

    public Version Version => new(1, 0, 0);

    public string Description => "Öffentliche Gästebucheinträge mit geschützter Verwaltung.";

    public int SortOrder => 20;
}
