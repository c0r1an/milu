using Milu.Web.Infrastructure.Authentication;
using Milu.Web.Infrastructure.Data;
using Milu.Web.Infrastructure.Modules;
using Milu.Web.Infrastructure.Routing;
using Milu.Web.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
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
builder.Services.AddTransient<MiluRouteTransformer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<MiluDbContext>();
    database.Database.EnsureCreated();
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
