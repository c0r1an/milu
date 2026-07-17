namespace Milu.Web.Infrastructure.Authentication;

public sealed class DemoAuthenticationOptions
{
    public const string SectionName = "DemoAuthentication";

    public string UserName { get; set; } = "admin";

    public string Password { get; set; } = string.Empty;
}
