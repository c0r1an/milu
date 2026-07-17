using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Milu.Web.Tests.Integration;

public sealed partial class MiluApplicationTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MiluApplicationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
        });
    }

    [Theory]
    [InlineData("/", "Sample-Modul")]
    [InlineData("/sample", "Sample-Modul")]
    [InlineData("/sample/index/index", "Sample-Modul")]
    [InlineData("/sample/index/hello/name/Ada", "Hallo, Ada!")]
    [InlineData("/guestbook", "Eintrag schreiben")]
    [InlineData("/news", "Milu ist modular")]
    public async Task FrontendRoutes_ReturnExpectedViews(
        string path,
        string expectedText)
    {
        using var client = CreateClient();

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, html);
        Assert.Contains(expectedText, html);
    }

    [Fact]
    public async Task UnknownModule_ReturnsNotFound()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/unknown/index/index");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ModuleStaticFile_IsServedFromModuleFolder()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/modules/sample/sample.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("sample-hero", css);
    }

    [Fact]
    public async Task AdminRoute_RedirectsAnonymousUserToLogin()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/admin/sample/index/index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/account/login", response.Headers.Location.AbsolutePath);
    }

    [Fact]
    public async Task AdminDashboard_RedirectsAnonymousUserToLogin()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task AdminModule_ListsAutomaticallyDiscoveredModules()
    {
        using var client = CreateClient();
        await LoginAsync(client, "/admin/modules");

        var response = await client.GetAsync("/admin/modules");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Administration", html);
        Assert.Contains("<code>guestbook</code>", html);
        Assert.Contains("<code>news</code>", html);
        Assert.Contains("<code>users</code>", html);
        Assert.Contains("Sample-Modul", html);
    }

    [Fact]
    public async Task Registration_AssignsBasicRightsButNoAdminRights()
    {
        using var client = CreateClient();
        var registerHtml = await client.GetStringAsync("/account/register");
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var userName = $"user-{Guid.NewGuid():N}";
        var displayName = $"Testbenutzer {Guid.NewGuid():N}";

        var response = await client.PostAsync(
            "/account/register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["DisplayName"] = displayName,
                ["Email"] = email,
                ["Password"] = "testpass",
                ["ConfirmPassword"] = "testpass",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(registerHtml)
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/news")).StatusCode);

        var adminResponse = await client.GetAsync("/admin");
        Assert.Equal(HttpStatusCode.Redirect, adminResponse.StatusCode);
        Assert.Equal("/account/access-denied", adminResponse.Headers.Location?.AbsolutePath);

        var authenticatedHtml = await client.GetStringAsync("/");
        var logoutResponse = await client.PostAsync(
            "/account/logout",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = GetAntiforgeryToken(authenticatedHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);

        var loginHtml = await client.GetStringAsync("/account/login?returnUrl=%2Fnews");
        var loginResponse = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = displayName,
                ["Password"] = "testpass",
                ["RememberMe"] = "false",
                ["ReturnUrl"] = "/news",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(loginHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        Assert.Equal("/news", loginResponse.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/news")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanManageUsersGroupsAndIndividualDenyOverridesGroup()
    {
        using var userClient = CreateClient();
        var email = $"rights-{Guid.NewGuid():N}@example.test";
        var userName = $"rights-{Guid.NewGuid():N}";
        var registerHtml = await userClient.GetStringAsync("/account/register");
        await userClient.PostAsync(
            "/account/register",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["DisplayName"] = "Rechtetest",
                ["Email"] = email,
                ["Password"] = "testpass",
                ["ConfirmPassword"] = "testpass",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(registerHtml)
            }));
        Assert.Equal(HttpStatusCode.OK, (await userClient.GetAsync("/news")).StatusCode);

        using var adminClient = CreateClient();
        await LoginAsync(adminClient, "/admin/users");
        var usersHtml = await adminClient.GetStringAsync("/admin/users");
        var userIdMatch = Regex.Match(
            usersHtml,
            $"{Regex.Escape(email)}.*?/admin/users/edit/([^\"]+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(userIdMatch.Success);
        var userId = userIdMatch.Groups[1].Value;

        var editHtml = await adminClient.GetStringAsync($"/admin/users/edit/{userId}");
        Assert.Contains("Individuelle Rechte", editHtml);
        var registeredRoleMatch = Regex.Match(
            editHtml,
            "name=\"roleIds\" value=\"([^\"]+)\"[^>]*checked[^>]*>.*?Registered",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(registeredRoleMatch.Success);

        var saveResponse = await adminClient.PostAsync(
            $"/admin/users/edit/{userId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["displayName"] = "Rechtetest",
                ["isActive"] = "true",
                ["roleIds"] = registeredRoleMatch.Groups[1].Value,
                ["permissionAssignments"] = "news|ModuleView|deny",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(editHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);

        var deniedResponse = await userClient.GetAsync("/news");
        Assert.Equal(HttpStatusCode.Redirect, deniedResponse.StatusCode);
        Assert.Equal("/account/access-denied", deniedResponse.Headers.Location?.AbsolutePath);

        var groupsResponse = await adminClient.GetAsync("/admin/groups");
        Assert.Equal(HttpStatusCode.OK, groupsResponse.StatusCode);
        var groupsHtml = await groupsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Gruppenverwaltung", groupsHtml);
        var groupIdMatch = Regex.Match(groupsHtml, "/admin/groups/edit/([^\"]+)");
        Assert.True(groupIdMatch.Success);
        var groupEditResponse = await adminClient.GetAsync(
            $"/admin/groups/edit/{groupIdMatch.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, groupEditResponse.StatusCode);
        Assert.Contains("Gruppenrechte", await groupEditResponse.Content.ReadAsStringAsync());

        var createUserResponse = await adminClient.GetAsync("/admin/users/create");
        Assert.Equal(HttpStatusCode.OK, createUserResponse.StatusCode);
        Assert.Contains("Benutzer anlegen", await createUserResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_CanCreateConfigureAndDeleteGroup()
    {
        using var client = CreateClient();
        await LoginAsync(client, "/admin/groups/create");
        var createHtml = await client.GetStringAsync("/admin/groups/create");
        var groupName = $"Redaktion-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsync(
            "/admin/groups/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = groupName,
                ["description"] = "Automatisch getestete Redaktionsgruppe",
                ["permissions"] = "news|ContentEdit|allow",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(createHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        var groupsHtml = await client.GetStringAsync("/admin/groups");
        Assert.Contains(groupName, groupsHtml);
        var groupIdMatch = Regex.Match(
            groupsHtml,
            $"{Regex.Escape(groupName)}.*?/admin/groups/edit/([^\"]+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(groupIdMatch.Success);
        var groupId = groupIdMatch.Groups[1].Value;

        var editHtml = await client.GetStringAsync($"/admin/groups/edit/{groupId}");
        Assert.Contains("news|ContentEdit|allow", editHtml);

        var deleteResponse = await client.PostAsync(
            $"/admin/groups/delete/{groupId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = GetAntiforgeryToken(groupsHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        Assert.DoesNotContain(groupName, await client.GetStringAsync("/admin/groups"));
    }

    [Fact]
    public async Task GuestbookEntry_CanBeCreatedAndManaged()
    {
        using var client = CreateClient();
        var createPage = await client.GetStringAsync("/guestbook/index/create");
        var createToken = GetAntiforgeryToken(createPage);
        var uniqueName = $"Testgast-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsync(
            "/guestbook/index/create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = uniqueName,
                ["Message"] = "Ein automatisierter Gästebucheintrag.",
                ["__RequestVerificationToken"] = createToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.Contains(uniqueName, await client.GetStringAsync("/guestbook"));

        await LoginAsync(client, "/admin/guestbook/index/index");
        var adminHtml = await client.GetStringAsync("/admin/guestbook/index/index");
        Assert.Contains(uniqueName, adminHtml);
        Assert.Contains("Bearbeiten", adminHtml);
        Assert.Contains("Löschen", adminHtml);

        var entryIdMatch = Regex.Match(
            adminHtml,
            $"{Regex.Escape(uniqueName)}.*?/admin/guestbook/index/edit/id/(\\d+)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        Assert.True(entryIdMatch.Success);
        var entryId = entryIdMatch.Groups[1].Value;

        var editResponse = await client.PostAsync(
            $"/admin/guestbook/index/edit/id/{entryId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = uniqueName + "-bearbeitet",
                ["Message"] = "Der Eintrag wurde erfolgreich bearbeitet.",
                ["__RequestVerificationToken"] = GetAntiforgeryToken(adminHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Assert.Contains(
            uniqueName + "-bearbeitet",
            await client.GetStringAsync("/guestbook"));

        adminHtml = await client.GetStringAsync("/admin/guestbook/index/index");
        var deleteResponse = await client.PostAsync(
            $"/admin/guestbook/index/delete/id/{entryId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = GetAntiforgeryToken(adminHtml)
            }));
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        Assert.DoesNotContain(
            uniqueName + "-bearbeitet",
            await client.GetStringAsync("/guestbook"));
    }

    [Fact]
    public async Task Login_AllowsAccessToAdminModule()
    {
        using var client = CreateClient();
        var adminPath = "/admin/sample/index/index";
        await LoginAsync(client, adminPath);

        var adminResponse = await client.GetAsync(adminPath);
        var adminHtml = await adminResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Contains("Sample-Administration", adminHtml);
        Assert.Contains("Angemeldet als admin", adminHtml);

        var adminTokenMatch = AntiforgeryTokenRegex().Match(adminHtml);
        Assert.True(adminTokenMatch.Success);

        var saveResponse = await client.PostAsync(
            "/admin/sample/index/save",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["message"] = "Integrationstest erfolgreich",
                ["__RequestVerificationToken"] = adminTokenMatch.Groups[1].Value
            }));

        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);
        Assert.Equal(
            adminPath,
            saveResponse.Headers.Location?.OriginalString);

        var savedPage = await client.GetStringAsync(adminPath);
        Assert.Contains("Gespeichert: Integrationstest erfolgreich", savedPage);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task LoginAsync(HttpClient client, string returnUrl)
    {
        var loginHtml = await client.GetStringAsync(
            $"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        var token = GetAntiforgeryToken(loginHtml);

        var response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = "admin",
                ["Password"] = "milu-demo",
                ["RememberMe"] = "false",
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = token
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);
    }

    private static string GetAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success);
        return match.Groups[1].Value;
    }

    [GeneratedRegex(
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"",
        RegexOptions.CultureInvariant)]
    private static partial Regex AntiforgeryTokenRegex();
}
