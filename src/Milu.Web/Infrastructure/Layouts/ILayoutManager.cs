namespace Milu.Web.Infrastructure.Layouts;

public interface ILayoutManager
{
    Task<string> GetActiveViewPathAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LayoutInfo>> GetLayoutsAsync(CancellationToken cancellationToken = default);
    Task ActivateAsync(string key, CancellationToken cancellationToken = default);
    Task SetEnabledAsync(string key, bool enabled, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task UpdateAsync(string key, CancellationToken cancellationToken = default);
}
