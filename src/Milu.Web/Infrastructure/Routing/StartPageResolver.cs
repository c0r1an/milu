using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Routing;

public sealed class StartPageResolver(MiluDbContext database, IModuleCatalog modules) : IStartPageResolver
{
    public const string SettingKey = "StartPageRoute";
    public const string FallbackRoute = "sample";

    public async Task<string> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var route = await database.Settings.AsNoTracking().Where(item => item.Key == SettingKey)
            .Select(item => item.Value).SingleOrDefaultAsync(cancellationToken);
        return IsAllowed(route) ? Normalize(route!) : FallbackRoute;
    }

    public async Task SetAsync(string route, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(route);
        if (!IsAllowed(normalized))
            throw new ArgumentException("Die Startseite muss zu einem installierten Nicht-Core-Modul gehören.", nameof(route));
        var setting = await database.Settings.FindAsync([SettingKey], cancellationToken);
        if (setting is null) database.Settings.Add(new MiluSetting { Key = SettingKey, Value = normalized });
        else setting.Value = normalized;
        await database.SaveChangesAsync(cancellationToken);
    }

    private bool IsAllowed(string? route)
    {
        var key = Normalize(route ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return key is not null && modules.TryGet(key, out var module) && !module.IsCoreModule;
    }

    private static string Normalize(string route) => route.Trim().Trim('/');
}
