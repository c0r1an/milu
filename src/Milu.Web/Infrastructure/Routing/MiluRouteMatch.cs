namespace Milu.Web.Infrastructure.Routing;

public sealed record MiluRouteMatch(
    bool IsAdmin,
    string Module,
    string Controller,
    string Action,
    IReadOnlyDictionary<string, string> Parameters);
