using Milu.Web.Infrastructure.Security;

namespace Milu.Web.Application.Modules.UserManagement.Models;

public sealed record PermissionMatrixItem(
    string ModuleKey,
    string ModuleName,
    PermissionOperation Operation,
    string OperationName,
    string Decision);
