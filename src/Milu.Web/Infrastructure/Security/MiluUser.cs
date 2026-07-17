using Microsoft.AspNetCore.Identity;

namespace Milu.Web.Infrastructure.Security;

public sealed class MiluUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
