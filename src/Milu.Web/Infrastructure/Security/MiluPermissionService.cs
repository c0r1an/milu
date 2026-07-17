using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Security;

public sealed class MiluPermissionService(
    MiluIdentityDbContext database,
    UserManager<MiluUser> userManager,
    IHttpContextAccessor httpContextAccessor) : IMiluPermissionService
{
    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string moduleKey,
        PermissionOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole(MiluRoleNames.Administrator))
        {
            return true;
        }

        var cacheKey = $"Milu.Permission:{principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "guest"}:{moduleKey}:{operation}";
        if (httpContextAccessor.HttpContext?.Items[cacheKey] is bool cached)
        {
            return cached;
        }

        var allowed = await EvaluateAsync(principal, moduleKey, operation, cancellationToken);
        if (httpContextAccessor.HttpContext is { } context)
        {
            context.Items[cacheKey] = allowed;
        }

        return allowed;
    }

    private async Task<bool> EvaluateAsync(
        ClaimsPrincipal principal,
        string moduleKey,
        PermissionOperation operation,
        CancellationToken cancellationToken)
    {
        var roleNames = new List<string>();
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || !user.IsActive)
            {
                return false;
            }

            var userOverride = await database.UserPermissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    permission => permission.UserId == userId &&
                                  permission.ModuleKey == moduleKey &&
                                  permission.Operation == operation,
                    cancellationToken);
            if (userOverride is not null)
            {
                return userOverride.IsAllowed;
            }

            roleNames.AddRange(await userManager.GetRolesAsync(user));
        }
        else
        {
            roleNames.Add(MiluRoleNames.Guest);
        }

        var normalizedNames = roleNames
            .Select(name => name.ToUpperInvariant())
            .ToArray();

        return await database.RolePermissions
            .AsNoTracking()
            .AnyAsync(
                permission =>
                    permission.ModuleKey == moduleKey &&
                    permission.Operation == operation &&
                    database.Roles.Any(role =>
                        role.Id == permission.RoleId &&
                        normalizedNames.Contains(role.NormalizedName!)),
                cancellationToken);
    }
}
