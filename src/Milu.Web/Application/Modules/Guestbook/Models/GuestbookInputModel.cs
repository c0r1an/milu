using System.ComponentModel.DataAnnotations;

namespace Milu.Web.Application.Modules.Guestbook.Models;

public sealed class GuestbookInputModel
{
    [Required(ErrorMessage = "Bitte gib einen Namen ein.")]
    [StringLength(80, ErrorMessage = "Der Name darf höchstens 80 Zeichen enthalten.")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib eine Nachricht ein.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Die Nachricht muss zwischen 3 und 1000 Zeichen lang sein.")]
    [Display(Name = "Nachricht")]
    public string Message { get; set; } = string.Empty;
}
