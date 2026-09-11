using System.Globalization;
using Label33.Application;
using Label33.Infrastructure;
using Label33.Infrastructure.Persistence;
using Label33.Web.Localization;
using Label33.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBrandLocalizer, BrandLocalizer>();
builder.Services.AddLabel33Application();
builder.Services.AddLabel33Infrastructure(builder.Configuration);
builder.Services.AddScoped<CatalogSeedService>();
builder.Services.AddScoped<IdentitySeedService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OpsConsole", policy =>
        policy.RequireRole(Label33Roles.Admin, Label33Roles.SuperAdmin));
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole(Label33Roles.SuperAdmin));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
    options.Events.OnRedirectToLogin = context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/ops-33-console"))
        {
            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect("/ops-33-console/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
        }
        else
        {
            context.Response.Redirect("/Account/Login?returnUrl=" + Uri.EscapeDataString(context.Request.Path + context.Request.QueryString));
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/ops-33-console"))
            context.Response.Redirect("/ops-33-console/login");
        else
            context.Response.Redirect("/Account/Login");
        return Task.CompletedTask;
    };
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Password.RequiredLength = 8;
});

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[] { new CultureInfo("en"), new CultureInfo("fa") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supported;
    options.SupportedUICultures = supported;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider { CookieName = "label33.culture" },
        new QueryStringRequestCultureProvider()
    };
});

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    app.Urls.Add($"http://0.0.0.0:{port}");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment()
    && !app.Environment.IsEnvironment("Local")
    && !app.Environment.IsEnvironment("DockerSql"))
{
    app.UseExceptionHandler("/Home/Error");
}
else if (app.Environment.IsDevelopment())
{
    // Local/DockerSql stay on plain http for easier machine testing
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Obscure ops console only — do not expose /Admin/*
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var provider = app.Configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        try { await db.Database.MigrateAsync(); }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not apply SQL Server migrations.");
        }
    }

    var identitySeed = scope.ServiceProvider.GetRequiredService<IdentitySeedService>();
    await identitySeed.EnsureSeedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<CatalogSeedService>();
    await seeder.EnsureSeedAsync();
}

app.Run();

public partial class Program;
