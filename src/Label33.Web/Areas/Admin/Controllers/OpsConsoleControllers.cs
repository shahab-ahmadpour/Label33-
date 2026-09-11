using Label33.Application.Catalog;
using Label33.Application.Orders;
using Label33.Domain.Enums;
using Label33.Infrastructure.Identity;
using Label33.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Label33.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("ops-33-console")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AuthController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid credentials.");
            return View();
        }

        var isOps = await _users.IsInRoleAsync(user, Label33Roles.SuperAdmin)
            || await _users.IsInRoleAsync(user, Label33Roles.Admin);
        if (!isOps)
        {
            ModelState.AddModelError(string.Empty, "This account has no console access.");
            return View();
        }

        var result = await _signIn.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.IsLockedOut
                ? "Account locked. Try again later."
                : "Invalid credentials.");
            return View();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/ops-33-console", StringComparison.OrdinalIgnoreCase))
            return Redirect(returnUrl);

        return Redirect("/ops-33-console");
    }

    [HttpPost("logout")]
    [Authorize(Policy = "OpsConsole")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/ops-33-console/login");
    }
}

[Area("Admin")]
[Route("ops-33-console")]
[Authorize(Policy = "OpsConsole")]
public class DashboardController : Controller
{
    private readonly OrderQueryService _orders;
    private readonly ProductCatalogQuery _catalog;

    public DashboardController(OrderQueryService orders, ProductCatalogQuery catalog)
    {
        _orders = orders;
        _catalog = catalog;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _catalog.ListPublishedAsync(ct: ct);
        var recentOrders = await _orders.ListRecentAsync(12, ct);
        ViewData["ProductCount"] = products.Count;
        ViewData["OrderCount"] = recentOrders.Count;
        ViewData["RecentOrders"] = recentOrders;
        return View();
    }
}

[Area("Admin")]
[Route("ops-33-console/products")]
[Authorize(Policy = "OpsConsole")]
public class ProductsController : Controller
{
    private readonly ProductAdminService _admin;
    private readonly ProductCatalogQuery _catalog;

    public ProductsController(ProductAdminService admin, ProductCatalogQuery catalog)
    {
        _admin = admin;
        _catalog = catalog;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var published = await _catalog.ListPublishedAsync(ct: ct);
        return View(published);
    }

    [HttpGet("create")]
    public IActionResult Create() => View();

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string category, string sku, decimal price, int stock, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sku))
        {
            ModelState.AddModelError(string.Empty, "Name and SKU are required.");
            return View();
        }

        var product = await _admin.CreateAsync(
            name.Trim(),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToUpperInvariant(),
            ProductType.Physical,
            [(sku.Trim().ToUpperInvariant(), "Default", price, StockMode.Tracked, Math.Max(0, stock))],
            ct);
        await _admin.PublishAsync(product.Id, ct);
        TempData["Ok"] = "Product created and published.";
        return Redirect("/ops-33-console/products");
    }
}

[Area("Admin")]
[Route("ops-33-console/team")]
[Authorize(Policy = "SuperAdminOnly")]
public class TeamController : Controller
{
    private readonly UserManager<ApplicationUser> _users;

    public TeamController(UserManager<ApplicationUser> users) => _users = users;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var super = await _users.GetUsersInRoleAsync(Label33Roles.SuperAdmin);
        var admins = await _users.GetUsersInRoleAsync(Label33Roles.Admin);
        var rows = super
            .Select(u => new TeamMemberVm(u.Id, u.Email ?? "", u.DisplayName, Label33Roles.SuperAdmin))
            .Concat(admins.Select(u => new TeamMemberVm(u.Id, u.Email ?? "", u.DisplayName, Label33Roles.Admin)))
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Email)
            .ToList();

        return View(rows);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string email, string password, string? displayName, string role)
    {
        role = string.Equals(role, Label33Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            ? Label33Roles.SuperAdmin
            : Label33Roles.Admin;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Email and password are required.";
            return Redirect("/ops-33-console/team");
        }

        var existing = await _users.FindByEmailAsync(email.Trim());
        if (existing is not null)
        {
            TempData["Error"] = "A user with this email already exists.";
            return Redirect("/ops-33-console/team");
        }

        var user = new ApplicationUser
        {
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()
        };

        var create = await _users.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            TempData["Error"] = string.Join(" ", create.Errors.Select(e => e.Description));
            return Redirect("/ops-33-console/team");
        }

        await _users.AddToRoleAsync(user, role);
        TempData["Ok"] = $"Added {role}: {user.Email}";
        return Redirect("/ops-33-console/team");
    }

    [HttpPost("remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id)
    {
        var currentId = _users.GetUserId(User);
        if (string.Equals(currentId, id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "You cannot remove your own account.";
            return Redirect("/ops-33-console/team");
        }

        var user = await _users.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            TempData["Error"] = "User not found.";
            return Redirect("/ops-33-console/team");
        }

        var isSuper = await _users.IsInRoleAsync(user, Label33Roles.SuperAdmin);
        if (isSuper)
        {
            var supers = await _users.GetUsersInRoleAsync(Label33Roles.SuperAdmin);
            if (supers.Count <= 1)
            {
                TempData["Error"] = "Cannot remove the last SuperAdmin.";
                return Redirect("/ops-33-console/team");
            }
        }

        // Strip ops roles instead of deleting identity (safer; keeps audit trail)
        if (await _users.IsInRoleAsync(user, Label33Roles.Admin))
            await _users.RemoveFromRoleAsync(user, Label33Roles.Admin);
        if (await _users.IsInRoleAsync(user, Label33Roles.SuperAdmin))
            await _users.RemoveFromRoleAsync(user, Label33Roles.SuperAdmin);

        TempData["Ok"] = $"Removed console access for {user.Email}.";
        return Redirect("/ops-33-console/team");
    }
}

public record TeamMemberVm(Guid Id, string Email, string? DisplayName, string Role);
