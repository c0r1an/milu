using Milu.Web.Infrastructure.Authentication;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Routing;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Milu.Web.Application.Modules.Media.Services;
using Microsoft.AspNetCore.DataProtection;
using Milu.Web.Infrastructure.Pagination;
using Milu.Web.Infrastructure.Layouts;
using Milu.Web.Infrastructure.Updates;
using System.Net.Http.Headers;
using Ganss.Xss;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var keyDirectory = Path.Combine(dataDirectory, "keys");
Directory.CreateDirectory(keyDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("Milu");
var contentConnectionString = ResolveConnectionString("Milu", "milu.db");
var identityConnectionString = ResolveConnectionString("MiluIdentity", "identity.db");

builder.Services.Configure<MiluOptions>(
    builder.Configuration.GetSection(MiluOptions.SectionName));
builder.Services.Configure<DemoAuthenticationOptions>(
    builder.Configuration.GetSection(DemoAuthenticationOptions.SectionName));

builder.Services.AddDbContext<MiluDbContext>(options =>
    options.UseSqlite(contentConnectionString));
builder.Services.AddDbContext<MiluIdentityDbContext>(options =>
    options.UseSqlite(identityConnectionString));

builder.Services
    .AddIdentity<MiluUser, MiluRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<MiluIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Milu.Authentication";
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
    options.SlidingExpiration = true;
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMiluPermissionService, MiluPermissionService>();
builder.Services.AddScoped<IMediaLibrary, MediaLibrary>();
var htmlSanitizer = new HtmlSanitizer();
htmlSanitizer.AllowedAttributes.Add("data-milu-media-id");
htmlSanitizer.AllowedTags.Add("video");
htmlSanitizer.AllowedTags.Add("source");
htmlSanitizer.AllowedAttributes.Add("controls");
htmlSanitizer.AllowedAttributes.Add("preload");
htmlSanitizer.AllowedAttributes.Add("poster");
htmlSanitizer.AllowedAttributes.Add("type");
htmlSanitizer.AllowedAttributes.Add("width");
htmlSanitizer.AllowedAttributes.Add("height");
htmlSanitizer.AllowedCssProperties.Add("width");
htmlSanitizer.AllowedCssProperties.Add("height");
htmlSanitizer.AllowedCssProperties.Add("max-width");
builder.Services.AddSingleton(htmlSanitizer);

builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddRazorOptions(options =>
    {
        options.ViewLocationExpanders.Add(new MiluViewLocationExpander());
    });

builder.Services.AddMiluModulesFromAssembly(typeof(Program).Assembly);
builder.Services.AddSingleton<MiluRouteParser>();
builder.Services.AddScoped<IStartPageResolver, StartPageResolver>();
builder.Services.AddScoped<IPaginationSettings, PaginationSettings>();
builder.Services.AddScoped<ILayoutManager, LayoutManager>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("MiluGitHubUpdates", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Milu", "1.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
    client.Timeout = TimeSpan.FromSeconds(10);
    var githubToken = builder.Configuration["Updates:GitHubToken"];
    if (!string.IsNullOrWhiteSpace(githubToken))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    }
});
builder.Services.AddScoped<IMiluUpdateService, GitHubMiluUpdateService>();
builder.Services.AddHttpClient("MiluReleaseAssets", client => client.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddScoped<IMiluUpdateInstaller, WindowsMiluUpdateInstaller>();
builder.Services.AddTransient<MiluRouteTransformer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<MiluDbContext>();
    database.Database.EnsureCreated();
    MiluMediaSchema.EnsureCreated(database);
    MiluDataSeeder.Seed(database);
    await MiluIdentitySeeder.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiluModuleStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapDynamicControllerRoute<MiluRouteTransformer>("{**path}");

app.Run();

string ResolveConnectionString(string name, string fileName)
{
    var configured = builder.Configuration.GetConnectionString(name);
    return string.IsNullOrWhiteSpace(configured)
        ? $"Data Source={Path.Combine(dataDirectory, fileName)}"
        : configured.Replace(
            "{ContentRoot}",
            builder.Environment.ContentRootPath,
            StringComparison.Ordinal);
}

public partial class Program;
