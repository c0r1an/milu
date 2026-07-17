using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.Media;

public sealed class MediaModule : IMiluModule
{
    public const string ModuleArea = "Media";
    public string Key => "media";
    public string DisplayName => "Medien";
    public string AreaName => ModuleArea;
    public string FolderName => "Media";
    public Version Version => new(1, 0, 0);
    public string Description => "Zentrale Medienbibliothek für Upload, Verwaltung und Wiederverwendung.";
    public bool ShowInFrontendNavigation => false;
    public bool IsCoreModule => true;
    public int SortOrder => 5;
}
