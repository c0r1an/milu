using System.ComponentModel.DataAnnotations;

namespace Milu.Web.Infrastructure.Security;

public enum PermissionOperation
{
    [Display(Name = "Modul sehen")]
    ModuleView = 1,

    [Display(Name = "Inhalte sehen")]
    ContentView = 2,

    [Display(Name = "Inhalte erstellen")]
    ContentCreate = 3,

    [Display(Name = "Inhalte bearbeiten")]
    ContentEdit = 4,

    [Display(Name = "Inhalte löschen")]
    ContentDelete = 5
}
