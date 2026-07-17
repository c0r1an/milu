namespace Milu.Web.Infrastructure.Modules;

public interface IMiluModule
{
    string Key { get; }

    string DisplayName { get; }

    string AreaName { get; }

    string FolderName { get; }

    Version Version { get; }

    string Description => DisplayName;

    string FrontendRoute => $"/{Key}";

    string AdminRoute => $"/admin/{Key}/index/index";

    bool HasAdminArea => true;

    bool ShowInFrontendNavigation => true;

    int SortOrder => 100;
}
