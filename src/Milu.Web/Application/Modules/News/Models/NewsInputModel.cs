using System.ComponentModel.DataAnnotations;

namespace Milu.Web.Application.Modules.News.Models;

public sealed class NewsInputModel
{
    [Required(ErrorMessage = "Bitte gib einen Titel ein.")]
    [StringLength(160, ErrorMessage = "Der Titel darf höchstens 160 Zeichen enthalten.")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib eine Zusammenfassung ein.")]
    [StringLength(500, ErrorMessage = "Die Zusammenfassung darf höchstens 500 Zeichen enthalten.")]
    [Display(Name = "Zusammenfassung")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib einen Inhalt ein.")]
    [StringLength(10000, MinimumLength = 3, ErrorMessage = "Der Inhalt muss zwischen 3 und 10000 Zeichen lang sein.")]
    [Display(Name = "Inhalt")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Veröffentlicht")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Titelmedium")]
    public int? FeaturedMediaId { get; set; }
}
