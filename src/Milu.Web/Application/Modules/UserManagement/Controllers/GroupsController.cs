using Milu.Web.Application.Modules.UserManagement.Models;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Application.Modules.UserManagement.Controllers;

[Area(UserManagementModule.ModuleArea)]
[Authorize]
[MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ModuleView)]
[Route("admin/groups")]
public sealed class GroupsController(
    RoleManager<MiluRole> roleManager,
    MiluIdentityDbContext database,
    IModuleCatalog moduleCatalog) : Controller
{
    [HttpGet("")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var roles = await database.Roles.AsNoTracking().OrderBy(role => role.Name).ToArrayAsync();
        var counts = await database.UserRoles
            .GroupBy(item => item.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count);
        var model = roles.Select(role => new GroupListItem(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            counts.GetValueOrDefault(role.Id),
            role.IsSystemRole)).ToArray();

        return View("/Application/Modules/UserManagement/Views/Groups/Index.cshtml", model);
    }

    [HttpGet("create")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentCreate)]
    public IActionResult Create()
    {
        return View(
            "/Application/Modules/UserManagement/Views/Groups/Edit.cshtml",
            BuildModel(null, string.Empty, string.Empty, false, []));
    }

    [HttpPost("create")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Create(
        string name,
        string description,
        string[] permissions)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            ModelState.AddModelError(nameof(name), "Bitte gib einen gültigen Gruppennamen ein.");
        }

        if (!ModelState.IsValid)
        {
            return View(
                "/Application/Modules/UserManagement/Views/Groups/Edit.cshtml",
                BuildModel(null, name, description, false, permissions));
        }

        var role = new MiluRole
        {
            Name = name.Trim(),
            Description = description.Trim(),
            IsSystemRole = false
        };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(
                "/Application/Modules/UserManagement/Views/Groups/Edit.cshtml",
                BuildModel(null, name, description, false, permissions));
        }

        await SavePermissionsAsync(role.Id, permissions);
        TempData["SuccessMessage"] = "Die Gruppe wurde erstellt.";
        return LocalRedirect("/admin/groups");
    }

    [HttpGet("edit/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var selected = await database.RolePermissions
            .Where(permission => permission.RoleId == id)
            .Select(permission => $"{permission.ModuleKey}|{permission.Operation}|allow")
            .ToArrayAsync();
        return View(
            "/Application/Modules/UserManagement/Views/Groups/Edit.cshtml",
            BuildModel(role.Id, role.Name!, role.Description, role.IsSystemRole, selected));
    }

    [HttpPost("edit/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(
        string id,
        string name,
        string description,
        string[] permissions)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (!role.IsSystemRole && !string.IsNullOrWhiteSpace(name))
        {
            role.Name = name.Trim();
        }
        role.Description = description.Trim();
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(
                "/Application/Modules/UserManagement/Views/Groups/Edit.cshtml",
                BuildModel(id, name, description, role.IsSystemRole, permissions));
        }

        await SavePermissionsAsync(role.Id, permissions);
        TempData["SuccessMessage"] = "Die Gruppenrechte wurden gespeichert.";
        return LocalRedirect("/admin/groups");
    }

    [HttpPost("delete/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (role.IsSystemRole || await database.UserRoles.AnyAsync(item => item.RoleId == id))
        {
            TempData["ErrorMessage"] = "Systemgruppen und verwendete Gruppen können nicht gelöscht werden.";
            return LocalRedirect("/admin/groups");
        }

        await roleManager.DeleteAsync(role);
        TempData["SuccessMessage"] = "Die Gruppe wurde gelöscht.";
        return LocalRedirect("/admin/groups");
    }

    private GroupEditViewModel BuildModel(
        string? id,
        string name,
        string description,
        bool system,
        IEnumerable<string> permissions)
    {
        var selected = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new GroupEditViewModel(
            id,
            name,
            description,
            system,
            PermissionMatrixFactory.Create(
                moduleCatalog,
                (key, operation) => selected.Contains($"{key}|{operation}|allow") ? "allow" : "inherit"));
    }

    private async Task SavePermissionsAsync(string roleId, IEnumerable<string> values)
    {
        var oldPermissions = await database.RolePermissions
            .Where(permission => permission.RoleId == roleId)
            .ToArrayAsync();
        database.RolePermissions.RemoveRange(oldPermissions);

        var parsed = values.Select(value =>
        {
            var valid = PermissionMatrixFactory.TryParse(value, out var key, out var operation, out var decision);
            return new { valid, key, operation, decision };
        }).Where(item => item.valid && item.decision == "allow")
          .DistinctBy(item => new { item.key, item.operation });

        database.RolePermissions.AddRange(parsed.Select(item => new MiluRolePermission
        {
            RoleId = roleId,
            ModuleKey = item.key,
            Operation = item.operation
        }));
        await database.SaveChangesAsync();
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
