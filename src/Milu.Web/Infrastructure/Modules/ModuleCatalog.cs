namespace Milu.Web.Infrastructure.Modules;

public sealed class ModuleCatalog : IModuleCatalog
{
    private readonly IReadOnlyDictionary<string, IMiluModule> _modules;

    public ModuleCatalog(IEnumerable<IMiluModule> modules)
    {
        var discoveredModules = modules.ToArray();
        var duplicates = discoveredModules
            .GroupBy(module => module.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Doppelte Milu-Modulschlüssel: {string.Join(", ", duplicates)}");
        }

        discoveredModules = discoveredModules
            .OrderBy(module => module.SortOrder)
            .ThenBy(module => module.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        _modules = discoveredModules.ToDictionary(
            module => module.Key,
            StringComparer.OrdinalIgnoreCase);
        Modules = Array.AsReadOnly(discoveredModules);
    }

    public IReadOnlyCollection<IMiluModule> Modules { get; }

    public bool TryGet(string key, out IMiluModule module)
    {
        return _modules.TryGetValue(key, out module!);
    }
}
