using Milu.Web.Infrastructure.Modules;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Milu.Web.Infrastructure.Routing;

public sealed class MiluRouteTransformer(
    MiluRouteParser parser,
    IModuleCatalog moduleCatalog,
    IStartPageResolver startPageResolver) : DynamicRouteValueTransformer
{
    public override async ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext,
        RouteValueDictionary values)
    {
        var path = values["path"]?.ToString();
        if (string.IsNullOrWhiteSpace(path))
        {
            path = await startPageResolver.ResolveAsync(httpContext.RequestAborted);
        }
        var route = parser.Parse(path);
        if (route is null || !moduleCatalog.TryGet(route.Module, out var module))
        {
            return null!;
        }

        var controllerName = ToPascalCase(route.Controller);
        var result = new RouteValueDictionary
        {
            ["area"] = module.AreaName,
            ["controller"] = route.IsAdmin
                ? $"Admin{controllerName}"
                : controllerName,
            ["action"] = ToPascalCase(route.Action),
            [MiluRouteKeys.Module] = module.Key,
            [MiluRouteKeys.ModuleFolder] = module.FolderName,
            [MiluRouteKeys.IsAdmin] = route.IsAdmin.ToString(),
            [MiluRouteKeys.ViewController] = controllerName
        };

        foreach (var parameter in route.Parameters)
        {
            result[parameter.Key] = parameter.Value;
        }

        return result;
    }

    private static string ToPascalCase(string value)
    {
        return string.Concat(
            value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
