namespace Milu.Web.Application.Modules.UserManagement.Models;

public sealed record GroupListItem(
    string Id,
    string Name,
    string Description,
    int UserCount,
    bool IsSystemRole);

public sealed record GroupEditViewModel(
    string? Id,
    string Name,
    string Description,
    bool IsSystemRole,
    IReadOnlyCollection<PermissionMatrixItem> Permissions);
