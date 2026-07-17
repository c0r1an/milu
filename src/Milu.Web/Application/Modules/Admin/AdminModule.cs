using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.Admin;

public sealed class AdminModule : IMiluModule
{
    public const string ModuleArea = "MiluAdmin";

    public string Key => "admin";

    public string DisplayName => "Administration";

    public string AreaName => ModuleArea;

    public string FolderName => "Admin";

    public Version Version => new(1, 0, 0);

    public string Description => "Zentrale Verwaltung und Übersicht aller installierten Module.";

    public string FrontendRoute => "/admin";

    public string AdminRoute => "/admin";

    public bool ShowInFrontendNavigation => false;

    public int SortOrder => 0;
}
