namespace Milu.Web.Infrastructure.Routing;

public interface IStartPageResolver
{
    Task<string> ResolveAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string route, CancellationToken cancellationToken = default);
}
