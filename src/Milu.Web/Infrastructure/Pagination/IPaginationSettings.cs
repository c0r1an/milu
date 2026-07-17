namespace Milu.Web.Infrastructure.Pagination;

public interface IPaginationSettings
{
    Task<int> GetDefaultPageSizeAsync(CancellationToken cancellationToken = default);
    Task<int> GetPageSizeAsync(string moduleKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, int?>> GetModuleOverridesAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(int defaultPageSize, IReadOnlyDictionary<string, int?> moduleOverrides,
        CancellationToken cancellationToken = default);
}
