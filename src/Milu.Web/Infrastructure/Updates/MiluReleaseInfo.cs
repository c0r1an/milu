namespace Milu.Web.Infrastructure.Updates;

public sealed record MiluReleaseInfo(
    string CurrentVersion, string? LatestVersion, string? Name, string? Notes,
    string? ReleaseUrl, DateTimeOffset? PublishedAt, bool IsConfigured, string? Error,
    string? PackageUrl = null, string? ChecksumUrl = null)
{
    public bool UpdateAvailable => Parse(LatestVersion) is { } latest && Parse(CurrentVersion) is { } current && latest > current;
    private static Version? Parse(string? value) => Version.TryParse(value?.Trim().TrimStart('v', 'V').Split('-')[0], out var version) ? version : null;
}
