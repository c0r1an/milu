using Microsoft.Extensions.Options;

namespace Milu.Web.Infrastructure.Routing;

public sealed class MiluRouteParser(IOptions<MiluOptions> options)
{
    private static readonly HashSet<string> ReservedParameterNames = new(
        ["admin", "area", "action", "controller", "module", "path"],
        StringComparer.OrdinalIgnoreCase);

    private readonly string _startModule = options.Value.StartModule;

    public MiluRouteMatch? Parse(string? path)
    {
        var segments = (path ?? string.Empty)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            segments = [_startModule];
        }

        if (segments.Length > 36 || segments.Any(segment => !IsValidSegment(segment)))
        {
            return null;
        }

        var position = 0;
        var isAdmin = segments[0].Equals("admin", StringComparison.OrdinalIgnoreCase);
        if (isAdmin)
        {
            position++;
        }

        if (position >= segments.Length)
        {
            return null;
        }

        var module = segments[position++];
        var controller = position < segments.Length ? segments[position++] : "index";
        var action = position < segments.Length ? segments[position++] : "index";
        var remainingCount = segments.Length - position;

        if (remainingCount % 2 != 0)
        {
            return null;
        }

        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (position < segments.Length)
        {
            var key = segments[position++];
            var value = segments[position++];

            if (ReservedParameterNames.Contains(key) ||
                !parameters.TryAdd(key, value))
            {
                return null;
            }
        }

        return new MiluRouteMatch(
            isAdmin,
            module,
            controller,
            action,
            parameters);
    }

    private static bool IsValidSegment(string segment)
    {
        return segment.Length is > 0 and <= 64 &&
               segment.All(character =>
                   character is >= 'a' and <= 'z' or
                   >= 'A' and <= 'Z' or
                   >= '0' and <= '9' or '_');
    }
}
