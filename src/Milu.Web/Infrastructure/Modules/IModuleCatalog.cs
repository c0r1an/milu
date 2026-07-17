namespace Milu.Web.Infrastructure.Modules;

public interface IModuleCatalog
{
    IReadOnlyCollection<IMiluModule> Modules { get; }

    bool TryGet(string key, out IMiluModule module);
}
