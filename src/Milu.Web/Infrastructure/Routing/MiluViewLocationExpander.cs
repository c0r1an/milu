using Microsoft.AspNetCore.Mvc.Razor;

namespace Milu.Web.Infrastructure.Routing;

public sealed class MiluViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
        CopyRouteValue(context, MiluRouteKeys.ModuleFolder);
        CopyRouteValue(context, MiluRouteKeys.IsAdmin);
        CopyRouteValue(context, MiluRouteKeys.ViewController);
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.Values.TryGetValue(MiluRouteKeys.ModuleFolder, out var moduleFolder) &&
            context.Values.TryGetValue(MiluRouteKeys.ViewController, out var controller))
        {
            var isAdmin = context.Values.TryGetValue(MiluRouteKeys.IsAdmin, out var admin) &&
                          bool.TryParse(admin, out var parsedAdmin) &&
                          parsedAdmin;

            yield return isAdmin
                ? $"/Application/Modules/{moduleFolder}/Views/Admin/{controller}/{{0}}.cshtml"
                : $"/Application/Modules/{moduleFolder}/Views/{controller}/{{0}}.cshtml";
        }

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }

    private static void CopyRouteValue(
        ViewLocationExpanderContext context,
        string key)
    {
        context.Values[key] =
            context.ActionContext.RouteData.Values[key]?.ToString() ?? string.Empty;
    }
}
