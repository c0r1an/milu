namespace Milu.Web.Infrastructure.Updates;

public interface IMiluUpdateInstaller
{
    Task PrepareAndStartAsync(MiluReleaseInfo release, CancellationToken cancellationToken = default);
}
