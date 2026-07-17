namespace Milu.Web.Application.Modules.Media.Models;

public sealed record MediaLibraryViewModel(
    IReadOnlyList<MediaAsset> Assets,
    IReadOnlyDictionary<int, int> UsageCounts,
    string Query,
    bool Picker,
    int Page,
    int TotalPages);

public sealed record MediaDetailsViewModel(MediaAsset Asset, IReadOnlyList<MediaUsage> Usages);
