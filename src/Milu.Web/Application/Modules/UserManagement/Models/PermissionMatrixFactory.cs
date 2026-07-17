using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;

namespace Milu.Web.Application.Modules.UserManagement.Models;

public static class PermissionMatrixFactory
{
    public static IReadOnlyCollection<PermissionMatrixItem> Create(
        IModuleCatalog catalog,
        Func<string, PermissionOperation, string> getDecision)
    {
        return catalog.Modules
            .SelectMany(module => Enum.GetValues<PermissionOperation>()
                .Select(operation => new PermissionMatrixItem(
                    module.Key,
                    module.DisplayName,
                    operation,
                    GetDisplayName(operation),
                    getDecision(module.Key, operation))))
            .ToArray();
    }

    public static bool TryParse(
        string value,
        out string moduleKey,
        out PermissionOperation operation,
        out string decision)
    {
        var parts = value.Split('|', StringSplitOptions.TrimEntries);
        moduleKey = parts.ElementAtOrDefault(0) ?? string.Empty;
        decision = parts.ElementAtOrDefault(2) ?? string.Empty;
        operation = default;
        return parts.Length == 3 &&
               Enum.TryParse(parts[1], out operation) &&
               moduleKey.Length is > 0 and <= 64;
    }

    private static string GetDisplayName(PermissionOperation operation)
    {
        return typeof(PermissionOperation)
            .GetMember(operation.ToString())
            .Single()
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName() ?? operation.ToString();
    }
}
