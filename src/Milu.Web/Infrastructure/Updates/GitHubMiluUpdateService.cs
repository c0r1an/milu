using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Milu.Web.Infrastructure.Updates;

public sealed class GitHubMiluUpdateService(
    IHttpClientFactory clients, IMemoryCache cache) : IMiluUpdateService
{
    private const string DefaultRepository = "c0r1an/milu";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public async Task<MiluReleaseInfo> CheckAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var repository = await GetRepositoryAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(repository)) return new(CurrentVersion, null, null, null, null, null, false, null);
        var cacheKey = "github-release:" + repository.ToLowerInvariant();
        if (!force && cache.TryGetValue(cacheKey, out MiluReleaseInfo? cached) && cached is not null) return cached;
        try
        {
            var client = clients.CreateClient("MiluGitHubUpdates");
            using var response = await client.GetAsync($"repos/{repository}/releases/latest", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                using var listResponse = await client.GetAsync($"repos/{repository}/releases?per_page=1", cancellationToken);
                if (listResponse.IsSuccessStatusCode)
                {
                    await using var listStream = await listResponse.Content.ReadAsStreamAsync(cancellationToken);
                    var releases = await JsonSerializer.DeserializeAsync<GitHubRelease[]>(listStream, cancellationToken: cancellationToken);
                    if (releases is null || releases.Length == 0)
                        return new(CurrentVersion, null, null, null, $"https://github.com/{repository}/releases", null, true,
                            "Für dieses Repository wurde noch kein öffentliches Release veröffentlicht.");
                }
                return new(CurrentVersion, null, null, null, null, null, true,
                    "Repository nicht öffentlich gefunden. Prüfe owner/repository oder konfiguriere für ein privates Repository den GitHub-Token.");
            }
            if (!response.IsSuccessStatusCode)
            {
                var failed = new MiluReleaseInfo(CurrentVersion, null, null, null, null, null, true,
                    $"GitHub antwortete mit {(int)response.StatusCode} {response.ReasonPhrase}.");
                cache.Set(cacheKey, failed, TimeSpan.FromMinutes(5));
                return failed;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken);
            var result = new MiluReleaseInfo(CurrentVersion, release?.TagName, release?.Name, release?.Body,
                release?.HtmlUrl, release?.PublishedAt, true, null,
                release?.Assets?.FirstOrDefault(item => item.Name == "milu-win-x64.zip")?.DownloadUrl,
                release?.Assets?.FirstOrDefault(item => item.Name == "milu-win-x64.zip.sha256")?.DownloadUrl);
            cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(CurrentVersion, null, null, null, null, null, true, $"Updateprüfung fehlgeschlagen: {exception.Message}");
        }
    }

    public Task<string?> GetRepositoryAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(DefaultRepository);

    private sealed class GitHubRelease
    {
        [System.Text.Json.Serialization.JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("body")] public string? Body { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("assets")] public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")] public string? Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("browser_download_url")] public string? DownloadUrl { get; set; }
    }
}
