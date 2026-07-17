using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Application.Modules.UserManagement;

public sealed class UserManagementModule : IMiluModule
{
    public const string ModuleArea = "UserManagement";
    public const string ModuleKey = "users";

    public string Key => ModuleKey;
    public string DisplayName => "Benutzerverwaltung";
    public string AreaName => ModuleArea;
    public string FolderName => "UserManagement";
    public Version Version => new(1, 0, 0);
    public string Description => "Benutzer, Gruppen und individuelle Modulrechte verwalten.";
    public string FrontendRoute => "/account/register";
    public string AdminRoute => "/admin/users";
    public bool ShowInFrontendNavigation => false;
    public bool IsCoreModule => true;
    public int SortOrder => 5;
}
