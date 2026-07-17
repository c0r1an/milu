using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Milu.Web.Infrastructure.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class MiluPermissionAttribute(
    string moduleKey,
    PermissionOperation operation) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var service = context.HttpContext.RequestServices
            .GetRequiredService<IMiluPermissionService>();
        var allowed = await service.HasPermissionAsync(
            context.HttpContext.User,
            moduleKey,
            operation,
            context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? new ForbidResult()
                : new ChallengeResult();
        }
    }
}
