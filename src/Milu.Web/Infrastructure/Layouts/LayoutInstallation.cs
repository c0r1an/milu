namespace Milu.Web.Infrastructure.Layouts;

public sealed class LayoutInstallation
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InstalledVersion { get; set; } = "1.0.0";
    public string AvailableVersion { get; set; } = "1.0.0";
    public string ViewPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsInstalled { get; set; } = true;
    public bool IsProtected { get; set; }
    public bool IsBuiltIn { get; set; }
}
