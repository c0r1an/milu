using Milu.Web.Infrastructure.Modules;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Milu.Web.Infrastructure.Routing;

public sealed class MiluRouteTransformer(
    MiluRouteParser parser,
    IModuleCatalog moduleCatalog) : DynamicRouteValueTransformer
{
    public override ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext,
        RouteValueDictionary values)
    {
        var route = parser.Parse(values["path"]?.ToString());
        if (route is null || !moduleCatalog.TryGet(route.Module, out var module))
        {
            return ValueTask.FromResult<RouteValueDictionary>(null!);
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

        return ValueTask.FromResult(result);
    }

    private static string ToPascalCase(string value)
    {
        return string.Concat(
            value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }
}
