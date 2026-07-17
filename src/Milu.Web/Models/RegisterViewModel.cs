using System.ComponentModel.DataAnnotations;

namespace Milu.Web.Models;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Bitte gib einen Benutzernamen ein.")]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(
        "^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Der Benutzername darf nur Buchstaben, Zahlen, Punkt, Bindestrich und Unterstrich enthalten.")]
    [Display(Name = "Benutzername")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib einen Anzeigenamen ein.")]
    [StringLength(120)]
    [Display(Name = "Anzeigename")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib eine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Bitte gib eine gültige E-Mail-Adresse ein.")]
    [Display(Name = "E-Mail-Adresse")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib ein Passwort ein.")]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Passwort")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
    [Display(Name = "Passwort wiederholen")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
