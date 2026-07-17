namespace Milu.Web.Application.Modules.UserManagement.Models;

public sealed record UserListItem(
    string Id,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Groups);

public sealed record RoleSelectionItem(string Id, string Name, bool Selected);

public sealed record UserEditViewModel(
    string Id,
    string DisplayName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<RoleSelectionItem> Roles,
    IReadOnlyCollection<PermissionMatrixItem> Permissions);

public sealed record UserCreateViewModel(
    string DisplayName,
    string Email,
    IReadOnlyCollection<RoleSelectionItem> Roles);
