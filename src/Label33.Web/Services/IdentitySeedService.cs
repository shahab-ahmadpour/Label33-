using Label33.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Label33.Web.Services;

public static class Label33Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
}

public class IdentitySeedService
{
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<IdentitySeedService> _logger;

    public IdentitySeedService(
        RoleManager<IdentityRole<Guid>> roles,
        UserManager<ApplicationUser> users,
        IConfiguration config,
        ILogger<IdentitySeedService> logger)
    {
        _roles = roles;
        _users = users;
        _config = config;
        _logger = logger;
    }

    public async Task EnsureSeedAsync(CancellationToken ct = default)
    {
        await EnsureRoleAsync(Label33Roles.SuperAdmin);
        await EnsureRoleAsync(Label33Roles.Admin);

        var email = _config["OpsConsole:SuperAdminEmail"] ?? "superadmin@33label.local";
        var password = _config["OpsConsole:SuperAdminPassword"] ?? "ChangeMe_33Label!";
        var displayName = _config["OpsConsole:SuperAdminDisplayName"] ?? "Super Admin";

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            var create = await _users.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                _logger.LogError("Failed to create SuperAdmin: {Errors}",
                    string.Join("; ", create.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Seeded SuperAdmin user {Email}", email);
        }

        if (!await _users.IsInRoleAsync(user, Label33Roles.SuperAdmin))
            await _users.AddToRoleAsync(user, Label33Roles.SuperAdmin);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roles.RoleExistsAsync(roleName))
            return;

        var result = await _roles.CreateAsync(new IdentityRole<Guid>(roleName));
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create role {Role}: {Errors}",
                roleName, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
