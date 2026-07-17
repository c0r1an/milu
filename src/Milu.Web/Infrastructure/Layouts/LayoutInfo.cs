namespace Milu.Web.Infrastructure.Layouts;

public sealed record LayoutInfo(
    string Key, string DisplayName, string Description, string InstalledVersion,
    string AvailableVersion, bool IsEnabled, bool IsInstalled, bool IsProtected,
    bool IsBuiltIn, bool IsActive)
{
    public bool HasUpdate => Version.TryParse(AvailableVersion, out var available) &&
                             Version.TryParse(InstalledVersion, out var installed) && available > installed;
}
