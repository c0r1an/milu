namespace Milu.Web.Infrastructure.Updates;

public interface IMiluUpdateService
{
    Task<MiluReleaseInfo> CheckAsync(bool force = false, CancellationToken cancellationToken = default);
    Task<string?> GetRepositoryAsync(CancellationToken cancellationToken = default);
}
