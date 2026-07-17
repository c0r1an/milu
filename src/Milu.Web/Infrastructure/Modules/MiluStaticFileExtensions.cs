using Microsoft.Extensions.FileProviders;

namespace Milu.Web.Infrastructure.Modules;

public static class MiluStaticFileExtensions
{
    public static IApplicationBuilder UseMiluModuleStaticFiles(
        this WebApplication app)
    {
        var catalog = app.Services.GetRequiredService<IModuleCatalog>();

        foreach (var module in catalog.Modules)
        {
            var staticPath = Path.Combine(
                app.Environment.ContentRootPath,
                "Application",
                "Modules",
                module.FolderName,
                "Static");

            if (!Directory.Exists(staticPath))
            {
                continue;
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(staticPath),
                RequestPath = $"/modules/{module.Key}"
            });
        }

        return app;
    }
}
