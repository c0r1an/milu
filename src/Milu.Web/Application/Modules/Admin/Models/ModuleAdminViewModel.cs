namespace Milu.Web.Application.Modules.Admin.Models;

public sealed record ModuleAdminViewModel(
    string Key,
    string DisplayName,
    string Description,
    Version Version,
    string FrontendRoute,
    string AdminRoute,
    bool HasAdminArea);
