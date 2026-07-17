namespace Milu.Web.Infrastructure.Security;

public sealed class MiluRolePermission
{
    public string RoleId { get; set; } = string.Empty;

    public string ModuleKey { get; set; } = string.Empty;

    public PermissionOperation Operation { get; set; }
}
