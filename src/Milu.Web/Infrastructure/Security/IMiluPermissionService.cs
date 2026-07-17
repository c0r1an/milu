using System.Security.Claims;

namespace Milu.Web.Infrastructure.Security;

public interface IMiluPermissionService
{
    Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string moduleKey,
        PermissionOperation operation,
        CancellationToken cancellationToken = default);
}
