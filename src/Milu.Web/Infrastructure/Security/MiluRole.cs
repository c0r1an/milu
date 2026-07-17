using Microsoft.AspNetCore.Identity;

namespace Milu.Web.Infrastructure.Security;

public sealed class MiluRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;

    public bool IsSystemRole { get; set; }
}
