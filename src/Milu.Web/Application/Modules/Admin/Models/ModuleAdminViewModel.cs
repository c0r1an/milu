namespace Milu.Web.Application.Modules.Admin.Models;

public sealed record ModuleAdminViewModel(
    string Key,
    string DisplayName,
    string Description,
    Version Version,
    string FrontendRoute,
    string AdminRoute,
    bool HasAdminArea,
    bool IsCoreModule);

public sealed record ModuleManagementViewModel(
    IReadOnlyCollection<ModuleAdminViewModel> Modules,
    IReadOnlyCollection<ModuleAdminViewModel> StartPageModules,
    string CurrentStartRoute,
    int DefaultPageSize,
    IReadOnlyDictionary<string, int?> ModulePageSizes);
