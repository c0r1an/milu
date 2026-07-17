using System.ComponentModel.DataAnnotations;

namespace Milu.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Bitte den Benutzernamen eingeben.")]
    [Display(Name = "Benutzername, Anzeigename oder E-Mail-Adresse")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte das Passwort eingeben.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Angemeldet bleiben")]
    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; } = "/";
}
