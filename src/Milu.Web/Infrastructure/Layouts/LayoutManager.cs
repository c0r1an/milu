using Milu.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Layouts;

public sealed class LayoutManager(MiluDbContext database) : ILayoutManager
{
    private const string ActiveKey = "Layout.Active";
    private const string FallbackKey = "classic";

    public async Task<string> GetActiveViewPathAsync(CancellationToken cancellationToken = default)
    {
        var active = await ActiveKeyAsync(cancellationToken);
        var layout = await database.LayoutInstallations.AsNoTracking().SingleOrDefaultAsync(
            item => item.Key == active && item.IsInstalled && item.IsEnabled, cancellationToken);
        if (layout is not null) return layout.ViewPath;
        return "/Views/Shared/Layouts/_Classic.cshtml";
    }

    public async Task<IReadOnlyList<LayoutInfo>> GetLayoutsAsync(CancellationToken cancellationToken = default)
    {
        var active = await ActiveKeyAsync(cancellationToken);
        return await database.LayoutInstallations.AsNoTracking().OrderBy(item => item.DisplayName)
            .Select(item => new LayoutInfo(item.Key, item.DisplayName, item.Description,
                item.InstalledVersion, item.AvailableVersion, item.IsEnabled, item.IsInstalled,
                item.IsProtected, item.IsBuiltIn, item.Key == active && item.IsEnabled && item.IsInstalled))
            .ToListAsync(cancellationToken);
    }

    public async Task ActivateAsync(string key, CancellationToken cancellationToken = default)
    {
        var layout = await FindAsync(key, cancellationToken);
        if (!layout.IsInstalled || !layout.IsEnabled) throw new InvalidOperationException("Das Layout ist nicht aktiviert.");
        await SetSettingAsync(ActiveKey, key, cancellationToken);
    }

    public async Task SetEnabledAsync(string key, bool enabled, CancellationToken cancellationToken = default)
    {
        var layout = await FindAsync(key, cancellationToken);
        if (!enabled && layout.IsProtected) throw new InvalidOperationException("Das geschützte Fallback-Layout kann nicht deaktiviert werden.");
        layout.IsEnabled = enabled;
        await database.SaveChangesAsync(cancellationToken);
        if (!enabled && await ActiveKeyAsync(cancellationToken) == key) await SetSettingAsync(ActiveKey, FallbackKey, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var layout = await FindAsync(key, cancellationToken);
        if (layout.IsProtected) throw new InvalidOperationException("Das geschützte Fallback-Layout kann nicht gelöscht werden.");
        layout.IsInstalled = false;
        layout.IsEnabled = false;
        await database.SaveChangesAsync(cancellationToken);
        if (await ActiveKeyAsync(cancellationToken) == key) await SetSettingAsync(ActiveKey, FallbackKey, cancellationToken);
    }

    public async Task UpdateAsync(string key, CancellationToken cancellationToken = default)
    {
        var layout = await FindAsync(key, cancellationToken);
        if (!layout.IsInstalled) throw new InvalidOperationException("Das Layout ist nicht installiert.");
        layout.InstalledVersion = layout.AvailableVersion;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<LayoutInstallation> FindAsync(string key, CancellationToken token) =>
        await database.LayoutInstallations.SingleOrDefaultAsync(item => item.Key == key, token)
        ?? throw new KeyNotFoundException("Layout nicht gefunden.");

    private async Task<string> ActiveKeyAsync(CancellationToken token) =>
        await database.Settings.AsNoTracking().Where(item => item.Key == ActiveKey).Select(item => item.Value)
            .SingleOrDefaultAsync(token) ?? FallbackKey;

    private async Task SetSettingAsync(string key, string value, CancellationToken token)
    {
        var setting = await database.Settings.FindAsync([key], token);
        if (setting is null) database.Settings.Add(new MiluSetting { Key = key, Value = value });
        else setting.Value = value;
        await database.SaveChangesAsync(token);
    }
}
