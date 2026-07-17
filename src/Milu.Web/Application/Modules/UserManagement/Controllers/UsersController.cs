using System.Security.Claims;
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
[Route("admin/users")]
public sealed class UsersController(
    UserManager<MiluUser> userManager,
    MiluIdentityDbContext database,
    IModuleCatalog moduleCatalog) : Controller
{
    [HttpGet("")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentView)]
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ToArrayAsync();
        var model = new List<UserListItem>();
        foreach (var user in users)
        {
            model.Add(new UserListItem(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.IsActive,
                user.CreatedAt,
                (await userManager.GetRolesAsync(user)).ToArray()));
        }

        return View("/Application/Modules/UserManagement/Views/Users/Index.cshtml", model);
    }

    [HttpGet("create")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Create()
    {
        return View(
            "/Application/Modules/UserManagement/Views/Users/Create.cshtml",
            new UserCreateViewModel(string.Empty, string.Empty, await BuildRolesAsync([])));
    }

    [HttpPost("create")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentCreate)]
    public async Task<IActionResult> Create(
        string displayName,
        string email,
        string password,
        string[] roleIds)
    {
        var user = new MiluUser
        {
            DisplayName = displayName.Trim(),
            Email = email.Trim(),
            UserName = email.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(
                "/Application/Modules/UserManagement/Views/Users/Create.cshtml",
                new UserCreateViewModel(displayName, email, await BuildRolesAsync(roleIds)));
        }

        var roleNames = await GetAssignableRoleNamesAsync(roleIds);
        if (roleNames.Length == 0)
        {
            roleNames = [MiluRoleNames.Registered];
        }
        result = await userManager.AddToRolesAsync(user, roleNames);
        if (!result.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(result);
            return View(
                "/Application/Modules/UserManagement/Views/Users/Create.cshtml",
                new UserCreateViewModel(displayName, email, await BuildRolesAsync(roleIds)));
        }

        TempData["SuccessMessage"] = "Der Benutzer wurde erstellt.";
        return LocalRedirect("/admin/users");
    }

    [HttpGet("edit/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        return user is null ? NotFound() : View(
            "/Application/Modules/UserManagement/Views/Users/Edit.cshtml",
            await BuildEditModelAsync(user));
    }

    [HttpPost("edit/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentEdit)]
    public async Task<IActionResult> Edit(
        string id,
        string displayName,
        bool isActive,
        string[] roleIds,
        string[] permissionAssignments)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var isCurrentUser = User.FindFirstValue(ClaimTypes.NameIdentifier) == id;
        user.DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? user.DisplayName
            : displayName.Trim();
        user.IsActive = isCurrentUser || isActive;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(
                "/Application/Modules/UserManagement/Views/Users/Edit.cshtml",
                await BuildEditModelAsync(user));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var selectedRoles = (await GetAssignableRoleNamesAsync(roleIds)).ToList();
        if (isCurrentUser && currentRoles.Contains(MiluRoleNames.Administrator))
        {
            selectedRoles.Add(MiluRoleNames.Administrator);
        }
        selectedRoles = selectedRoles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await userManager.RemoveFromRolesAsync(user, currentRoles.Except(selectedRoles));
        await userManager.AddToRolesAsync(user, selectedRoles.Except(currentRoles));
        await SaveUserPermissionsAsync(user.Id, permissionAssignments);
        await userManager.UpdateSecurityStampAsync(user);

        TempData["SuccessMessage"] = "Benutzer, Gruppen und Einzelrechte wurden gespeichert.";
        return LocalRedirect("/admin/users");
    }

    [HttpPost("delete/{id}")]
    [MiluPermission(UserManagementModule.ModuleKey, PermissionOperation.ContentDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id)
        {
            TempData["ErrorMessage"] = "Das eigene Benutzerkonto kann hier nicht gelöscht werden.";
            return LocalRedirect("/admin/users");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = "Der Benutzer wurde gelöscht.";
        }
        return LocalRedirect("/admin/users");
    }

    private async Task<UserEditViewModel> BuildEditModelAsync(MiluUser user)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        var overrides = await database.UserPermissions
            .AsNoTracking()
            .Where(permission => permission.UserId == user.Id)
            .ToArrayAsync();
        var matrix = PermissionMatrixFactory.Create(moduleCatalog, (key, operation) =>
        {
            var item = overrides.SingleOrDefault(permission =>
                permission.ModuleKey == key && permission.Operation == operation);
            return item is null ? "inherit" : item.IsAllowed ? "allow" : "deny";
        });

        return new UserEditViewModel(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.IsActive,
            await BuildRolesAsync(roleNames),
            matrix);
    }

    private async Task<IReadOnlyCollection<RoleSelectionItem>> BuildRolesAsync(
        IEnumerable<string> selectedValues)
    {
        var selected = selectedValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = await database.Roles.AsNoTracking()
            .Where(role => role.Name != MiluRoleNames.Guest)
            .OrderBy(role => role.Name)
            .ToArrayAsync();
        return roles.Select(role => new RoleSelectionItem(
            role.Id,
            role.Name ?? string.Empty,
            selected.Contains(role.Id) || selected.Contains(role.Name ?? string.Empty))).ToArray();
    }

    private async Task<string[]> GetAssignableRoleNamesAsync(IEnumerable<string> roleIds)
    {
        var ids = roleIds.Distinct().ToArray();
        return await database.Roles.AsNoTracking()
            .Where(role => ids.Contains(role.Id) && role.Name != MiluRoleNames.Guest)
            .Select(role => role.Name!)
            .ToArrayAsync();
    }

    private async Task SaveUserPermissionsAsync(string userId, IEnumerable<string> values)
    {
        var old = await database.UserPermissions
            .Where(permission => permission.UserId == userId)
            .ToArrayAsync();
        database.UserPermissions.RemoveRange(old);
        var parsed = values.Select(value =>
        {
            var valid = PermissionMatrixFactory.TryParse(value, out var key, out var operation, out var decision);
            return new { valid, key, operation, decision };
        }).Where(item => item.valid && item.decision is "allow" or "deny")
          .DistinctBy(item => new { item.key, item.operation });
        database.UserPermissions.AddRange(parsed.Select(item => new MiluUserPermission
        {
            UserId = userId,
            ModuleKey = item.key,
            Operation = item.operation,
            IsAllowed = item.decision == "allow"
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
