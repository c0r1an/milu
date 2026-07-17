using Milu.Web.Infrastructure.Updates;

namespace Milu.Web.Application.Modules.Admin.Models;

public sealed record UpdateAdminViewModel(string Repository, MiluReleaseInfo Release);
