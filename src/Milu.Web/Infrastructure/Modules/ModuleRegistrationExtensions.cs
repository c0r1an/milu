using System.Reflection;

namespace Milu.Web.Infrastructure.Modules;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddMiluModulesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var moduleTypes = assembly.DefinedTypes
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IMiluModule).IsAssignableFrom(type.AsType()))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var moduleType in moduleTypes)
        {
            services.AddSingleton(typeof(IMiluModule), moduleType.AsType());
        }

        services.AddSingleton<IModuleCatalog, ModuleCatalog>();
        return services;
    }
}
