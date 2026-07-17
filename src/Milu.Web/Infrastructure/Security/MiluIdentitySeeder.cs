using Milu.Web.Infrastructure.Authentication;
using Milu.Web.Infrastructure.Modules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Milu.Web.Infrastructure.Security;

public static class MiluIdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var database = services.GetRequiredService<MiluIdentityDbContext>();
        await database.Database.EnsureCreatedAsync();

        var roleManager = services.GetRequiredService<RoleManager<MiluRole>>();
        var userManager = services.GetRequiredService<UserManager<MiluUser>>();
        var modules = services.GetRequiredService<IModuleCatalog>();

        var administrator = await EnsureRoleAsync(
            roleManager,
            MiluRoleNames.Administrator,
            "Vollzugriff auf Milu.",
            true);
        var registered = await EnsureRoleAsync(
            roleManager,
            MiluRoleNames.Registered,
            "Grundrechte für registrierte Benutzer.",
            true);
        var guest = await EnsureRoleAsync(
            roleManager,
            MiluRoleNames.Guest,
            "Rechte für nicht angemeldete Besucher.",
            true);

        await SeedBasicPermissionsAsync(database, guest.Id, modules);
        await SeedBasicPermissionsAsync(database, registered.Id, modules);

        var configured = services
            .GetRequiredService<IOptions<DemoAuthenticationOptions>>()
            .Value;
        if (!string.IsNullOrWhiteSpace(configured.Password))
        {
            var adminUser = await userManager.FindByNameAsync(configured.UserName);
            if (adminUser is null)
            {
                adminUser = new MiluUser
                {
                    UserName = configured.UserName,
                    Email = "admin@milu.local",
                    EmailConfirmed = true,
                    DisplayName = "Milu Administrator",
                    IsActive = true
                };
                var result = await userManager.CreateAsync(adminUser, configured.Password);
                ThrowIfFailed(result, "Administratorkonto konnte nicht angelegt werden");
            }

            if (!await userManager.IsInRoleAsync(adminUser, administrator.Name!))
            {
                var result = await userManager.AddToRoleAsync(adminUser, administrator.Name!);
                ThrowIfFailed(result, "Administratorrolle konnte nicht zugewiesen werden");
            }
        }
    }

    private static async Task<MiluRole> EnsureRoleAsync(
        RoleManager<MiluRole> roleManager,
        string name,
        string description,
        bool isSystemRole)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role is not null)
        {
            return role;
        }

        role = new MiluRole
        {
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole
        };
        var result = await roleManager.CreateAsync(role);
        ThrowIfFailed(result, $"Gruppe {name} konnte nicht angelegt werden");
        return role;
    }

    private static async Task SeedBasicPermissionsAsync(
        MiluIdentityDbContext database,
        string roleId,
        IModuleCatalog modules)
    {
        var existingPermissions = (await database.RolePermissions
                .AsNoTracking()
                .Where(permission => permission.RoleId == roleId)
                .Select(permission => new
                {
                    permission.ModuleKey,
                    permission.Operation
                })
                .ToListAsync())
            .Select(permission => (permission.ModuleKey, permission.Operation))
            .ToHashSet();

        var publicModules = modules.Modules
            .Where(module => module.Key is "sample" or "news" or "guestbook")
            .ToArray();
        foreach (var module in publicModules)
        {
            var operations = new List<PermissionOperation>
            {
                PermissionOperation.ModuleView,
                PermissionOperation.ContentView
            };

            if (module.Key == "guestbook")
            {
                operations.Add(PermissionOperation.ContentCreate);
            }

            foreach (var operation in operations)
            {
                if (!existingPermissions.Add((module.Key, operation)))
                {
                    continue;
                }

                database.RolePermissions.Add(new MiluRolePermission
                {
                    RoleId = roleId,
                    ModuleKey = module.Key,
                    Operation = operation
                });
            }
        }

        await database.SaveChangesAsync();
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"{message}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }
}
