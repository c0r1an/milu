using Milu.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Pagination;

public sealed class PaginationSettings(MiluDbContext database) : IPaginationSettings
{
    private const string DefaultKey = "Pagination.DefaultPageSize";
    private const string ModulePrefix = "Pagination.Module.";
    public const int BuiltInDefault = 5;

    public async Task<int> GetDefaultPageSizeAsync(CancellationToken cancellationToken = default)
    {
        var value = await database.Settings.AsNoTracking().Where(item => item.Key == DefaultKey)
            .Select(item => item.Value).SingleOrDefaultAsync(cancellationToken);
        return Parse(value) ?? BuiltInDefault;
    }

    public async Task<int> GetPageSizeAsync(string moduleKey, CancellationToken cancellationToken = default)
    {
        var key = ModulePrefix + moduleKey.ToLowerInvariant();
        var value = await database.Settings.AsNoTracking().Where(item => item.Key == key)
            .Select(item => item.Value).SingleOrDefaultAsync(cancellationToken);
        return Parse(value) ?? await GetDefaultPageSizeAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int?>> GetModuleOverridesAsync(CancellationToken cancellationToken = default) =>
        (await database.Settings.AsNoTracking().Where(item => item.Key.StartsWith(ModulePrefix))
            .ToListAsync(cancellationToken)).ToDictionary(
                item => item.Key[ModulePrefix.Length..], item => Parse(item.Value), StringComparer.OrdinalIgnoreCase);

    public async Task SaveAsync(int defaultPageSize, IReadOnlyDictionary<string, int?> moduleOverrides,
        CancellationToken cancellationToken = default)
    {
        await SetAsync(DefaultKey, Math.Clamp(defaultPageSize, 1, 100).ToString(), cancellationToken);
        foreach (var (module, value) in moduleOverrides)
        {
            var key = ModulePrefix + module.ToLowerInvariant();
            var existing = await database.Settings.FindAsync([key], cancellationToken);
            if (value.HasValue)
            {
                var normalized = Math.Clamp(value.Value, 1, 100).ToString();
                if (existing is null) database.Settings.Add(new MiluSetting { Key = key, Value = normalized });
                else existing.Value = normalized;
            }
            else if (existing is not null) database.Settings.Remove(existing);
        }
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await database.Settings.FindAsync([key], cancellationToken);
        if (setting is null) database.Settings.Add(new MiluSetting { Key = key, Value = value });
        else setting.Value = value;
    }

    private static int? Parse(string? value) => int.TryParse(value, out var parsed) && parsed is >= 1 and <= 100 ? parsed : null;
}
