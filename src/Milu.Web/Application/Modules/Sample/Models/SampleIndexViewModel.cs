namespace Milu.Web.Application.Modules.Sample.Models;

public sealed record SampleIndexViewModel(
    string ModuleName,
    Version Version,
    IReadOnlyCollection<ModuleSummary> RegisteredModules);

public sealed record ModuleSummary(
    string Key,
    string DisplayName,
    Version Version);
