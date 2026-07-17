using Milu.Web.Infrastructure.Security;
using Milu.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Controllers;

[Route("account")]
public sealed class AccountController(
    UserManager<MiluUser> userManager,
    SignInManager<MiluUser> signInManager) : Controller
{
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(GetSafeReturnUrl(returnUrl));
        }

        return View(new LoginViewModel { ReturnUrl = GetSafeReturnUrl(returnUrl) });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var login = input.UserName.Trim();
        var user = await userManager.FindByNameAsync(login)
            ?? await userManager.FindByEmailAsync(login);
        if (user is null)
        {
            var displayNameMatches = await userManager.Users
                .Where(item => EF.Functions.Collate(item.DisplayName, "NOCASE") == login)
                .OrderBy(item => item.Id)
                .Take(2)
                .ToArrayAsync();
            user = displayNameMatches.Length == 1 ? displayNameMatches[0] : null;
        }
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Benutzername oder Passwort ist falsch.");
            return View(input);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            input.Password,
            input.RememberMe,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                result.IsLockedOut
                    ? "Das Konto ist vorübergehend gesperrt."
                    : "Benutzername oder Passwort ist falsch.");
            return View(input);
        }

        return LocalRedirect(GetSafeReturnUrl(input.ReturnUrl));
    }

    [AllowAnonymous]
    [HttpGet("register")]
    public IActionResult Register()
    {
        return User.Identity?.IsAuthenticated == true
            ? LocalRedirect("/")
            : View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = new MiluUser
        {
            UserName = input.UserName.Trim(),
            Email = input.Email.Trim(),
            DisplayName = input.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(input);
        }

        result = await userManager.AddToRoleAsync(user, MiluRoleNames.Registered);
        if (!result.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddIdentityErrors(result);
            return View(input);
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        TempData["SuccessMessage"] = "Dein Benutzerkonto wurde erstellt.";
        return LocalRedirect("/");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return LocalRedirect("/");
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();

    private string GetSafeReturnUrl(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
