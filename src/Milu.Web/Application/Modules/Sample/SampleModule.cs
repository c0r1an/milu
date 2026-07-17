using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.Sample;

public sealed class SampleModule : IMiluModule
{
    public const string ModuleArea = "Sample";

    public string Key => "sample";

    public string DisplayName => "Sample-Modul";

    public string AreaName => ModuleArea;

    public string FolderName => "Sample";

    public Version Version => new(1, 0, 0);

    public string Description => "Beispiel für Routing, Views und Modulregistrierung.";

    public bool IsCoreModule => true;

    public int SortOrder => 900;
}
