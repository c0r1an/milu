namespace Milu.Web.Infrastructure.Security;

public sealed class MiluUserPermission
{
    public string UserId { get; set; } = string.Empty;

    public string ModuleKey { get; set; } = string.Empty;

    public PermissionOperation Operation { get; set; }

    public bool IsAllowed { get; set; }
}
