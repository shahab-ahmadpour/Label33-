using Label33.Application.Catalog;
using Label33.Application.Common;
using Label33.Application.Fulfillment;
using Label33.Application.Orders;
using Label33.Domain.Enums;
using Label33.Infrastructure.Identity;
using Label33.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
[Route("ops-33-console/account")]
[Authorize(Policy = "OpsConsole")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpGet("password")]
    public IActionResult Password() => View();

    [HttpPost("password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(string currentPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ModelState.AddModelError(string.Empty, "Current and new password are required.");
            return View();
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, "New password confirmation does not match.");
            return View();
        }

        var user = await _users.GetUserAsync(User);
        if (user is null)
            return Redirect("/ops-33-console/login");

        var result = await _users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View();
        }

        await _signIn.RefreshSignInAsync(user);
        TempData["Ok"] = "Password updated.";
        return Redirect("/ops-33-console/account/password");
    }
}

[Area("Admin")]
[Route("ops-33-console")]
[Authorize(Policy = "OpsConsole")]
public class DashboardController : Controller
{
    private readonly OrderQueryService _orders;
    private readonly ProductAdminService _products;

    public DashboardController(OrderQueryService orders, ProductAdminService products)
    {
        _orders = orders;
        _products = products;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _products.ListAllAsync(ct: ct);
        var recentOrders = await _orders.ListRecentAsync(12, ct);
        ViewData["ProductCount"] = products.Count(p => p.Status == ProductStatus.Published);
        ViewData["OrderCount"] = recentOrders.Count;
        ViewData["RecentOrders"] = recentOrders;
        return View();
    }
}

[Area("Admin")]
[Route("ops-33-console/orders")]
[Authorize(Policy = "OpsConsole")]
public class OrdersController : Controller
{
    private readonly OrderQueryService _query;
    private readonly OrderAdminService _admin;
    private readonly FulfillmentService _fulfillment;

    public OrdersController(OrderQueryService query, OrderAdminService admin, FulfillmentService fulfillment)
    {
        _query = query;
        _admin = admin;
        _fulfillment = fulfillment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(OrderStatus? status, CancellationToken ct)
    {
        var orders = await _query.ListAsync(status, 100, ct);
        ViewData["Status"] = status;
        return View(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var order = await _query.GetAsync(id, ct);
        if (order is null) return NotFound();
        ViewData["Allowed"] = _admin.GetAllowedTransitions(order.Status);
        return View(order);
    }

    [HttpPost("{id:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatus to, string? note, CancellationToken ct)
    {
        try
        {
            var actor = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
            await _admin.TransitionAsync(id, to, note, actor, ct);
            TempData["Ok"] = $"Order moved to {to}.";
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/orders/{id}");
    }

    [HttpPost("shipments/{shipmentId:guid}/ship")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(Guid shipmentId, Guid orderId, string? carrier, string? trackingCode, CancellationToken ct)
    {
        try
        {
            await _fulfillment.MarkShippedAsync(shipmentId, carrier, trackingCode, ct);
            TempData["Ok"] = "Shipment marked as shipped.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/orders/{orderId}");
    }

    [HttpPost("shipments/{shipmentId:guid}/deliver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(Guid shipmentId, Guid orderId, CancellationToken ct)
    {
        try
        {
            await _fulfillment.MarkDeliveredAsync(shipmentId, ct);
            TempData["Ok"] = "Shipment marked as delivered.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/orders/{orderId}");
    }
}

[Area("Admin")]
[Route("ops-33-console/products")]
[Authorize(Policy = "OpsConsole")]
public class ProductsController : Controller
{
    private readonly ProductAdminService _admin;

    public ProductsController(ProductAdminService admin) => _admin = admin;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? q,
        Guid? categoryId,
        ProductStatus? status,
        AdminProductSort sort = AdminProductSort.UpdatedDesc,
        CancellationToken ct = default)
    {
        ViewData["Categories"] = await _admin.ListCategoriesAsync(ct);
        ViewData["Stats"] = await _admin.GetWarehouseStatsAsync(ct);
        ViewData["Q"] = q;
        ViewData["CategoryId"] = categoryId;
        ViewData["Status"] = status;
        ViewData["Sort"] = sort;
        var items = await _admin.ListAllAsync(q, categoryId, status, sort, ct);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewData["Categories"] = await _admin.ListCategoriesAsync(ct);
        ViewData["Sizes"] = CatalogDefaults.Sizes;
        return View();
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Create(
        string name,
        Guid categoryId,
        string? fullDescription,
        string skuPrefix,
        decimal price,
        bool isFeatured,
        bool publish,
        string[]? sizeCodes,
        IFormCollection form,
        List<IFormFile>? images,
        CancellationToken ct)
    {
        ViewData["Categories"] = await _admin.ListCategoriesAsync(ct);
        ViewData["Sizes"] = CatalogDefaults.Sizes;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(skuPrefix))
        {
            ModelState.AddModelError(string.Empty, "Name and SKU prefix are required.");
            return View();
        }

        if (price < 0)
        {
            ModelState.AddModelError(string.Empty, "Default price (Toman) must be zero or greater.");
            return View();
        }

        var defaultPriceRials = Money.ToRials(price);
        var sizes = new List<SizeStockInput>();
        foreach (var code in sizeCodes ?? Array.Empty<string>())
        {
            var stock = 0;
            _ = int.TryParse(form[$"stock_{code}"], out stock);

            var sizePriceToman = price;
            if (decimal.TryParse(form[$"price_{code}"], out var parsedSizePrice) && parsedSizePrice >= 0)
                sizePriceToman = parsedSizePrice;

            sizes.Add(new SizeStockInput(code, Math.Max(0, stock), Money.ToRials(sizePriceToman)));
        }

        if (sizes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Enable at least one size.");
            return View();
        }

        var basePriceRials = sizes.Min(s => s.Price ?? defaultPriceRials);

        try
        {
            var product = await _admin.CreateRichAsync(
                name,
                null,
                fullDescription,
                categoryId,
                isFeatured,
                ProductType.Physical,
                skuPrefix,
                basePriceRials,
                sizes,
                publish,
                ct);

            if (images is not null)
            {
                var first = true;
                foreach (var file in images.Where(f => f.Length > 0))
                {
                    await using var stream = file.OpenReadStream();
                    await _admin.AddImageAsync(product.Id, stream, file.FileName, file.ContentType, first, null, ct);
                    first = false;
                }
            }

            TempData["Ok"] = publish ? "Product created and published." : "Product draft created.";
            return Redirect($"/ops-33-console/products/{product.Id}");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var product = await _admin.GetAdminAsync(id, ct);
        if (product is null) return NotFound();
        ViewData["Categories"] = await _admin.ListCategoriesAsync(ct);
        ViewData["Sizes"] = CatalogDefaults.Sizes;
        return View(product);
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        string name,
        string? fullDescription,
        Guid categoryId,
        bool isFeatured,
        CancellationToken ct)
    {
        try
        {
            await _admin.UpdateDetailsAsync(id, name, fullDescription, isFeatured, categoryId, ct);
            TempData["Ok"] = "Product details saved.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{id}");
    }

    [HttpPost("{id:guid}/sizes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSizes(
        Guid id,
        decimal price,
        string[]? sizeCodes,
        IFormCollection form,
        CancellationToken ct)
    {
        if (price < 0)
        {
            TempData["Error"] = "Default price (Toman) must be zero or greater.";
            return Redirect($"/ops-33-console/products/{id}");
        }

        var defaultPriceRials = Money.ToRials(price);
        var sizes = new List<SizeStockInput>();
        foreach (var code in CatalogDefaults.Sizes)
        {
            var enabled = sizeCodes?.Contains(code, StringComparer.OrdinalIgnoreCase) == true;
            var stock = 0;
            _ = int.TryParse(form[$"stock_{code}"], out stock);

            var sizePriceToman = price;
            if (decimal.TryParse(form[$"price_{code}"], out var parsedSizePrice) && parsedSizePrice >= 0)
                sizePriceToman = parsedSizePrice;

            sizes.Add(new SizeStockInput(code, Math.Max(0, stock), Money.ToRials(sizePriceToman), enabled));
        }

        try
        {
            await _admin.SyncSizeMatrixAsync(id, defaultPriceRials, sizes, ct);
            TempData["Ok"] = "Sizes and inventory saved.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{id}");
    }

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        try
        {
            await _admin.PublishAsync(id, ct);
            TempData["Ok"] = "Product published.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{id}");
    }

    [HttpPost("{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        try
        {
            await _admin.ArchiveAsync(id, ct);
            TempData["Ok"] = "Product archived.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{id}");
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _admin.DeleteAsync(id, ct);
            TempData["Ok"] = "Product deleted.";
            return Redirect("/ops-33-console/products");
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return Redirect($"/ops-33-console/products/{id}");
        }
    }

    [HttpPost("{id:guid}/images")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadImage(Guid id, List<IFormFile>? files, IFormFile? file, bool makePrimary, string? altText, CancellationToken ct)
    {
        var uploads = new List<IFormFile>();
        if (files is not null) uploads.AddRange(files.Where(f => f.Length > 0));
        if (file is not null && file.Length > 0) uploads.Add(file);

        if (uploads.Count == 0)
        {
            TempData["Error"] = "Choose an image file.";
            return Redirect($"/ops-33-console/products/{id}");
        }

        try
        {
            var first = true;
            foreach (var img in uploads)
            {
                await using var stream = img.OpenReadStream();
                await _admin.AddImageAsync(id, stream, img.FileName, img.ContentType, makePrimary && first, altText, ct);
                first = false;
            }
            TempData["Ok"] = uploads.Count == 1 ? "Image uploaded." : $"{uploads.Count} images uploaded.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{id}");
    }

    [HttpPost("images/{imageId:guid}/primary")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryImage(Guid imageId, Guid productId, CancellationToken ct)
    {
        try
        {
            await _admin.SetPrimaryImageAsync(imageId, ct);
            TempData["Ok"] = "Primary image updated.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{productId}");
    }

    [HttpPost("images/{imageId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid imageId, Guid productId, CancellationToken ct)
    {
        try
        {
            await _admin.RemoveImageAsync(imageId, ct);
            TempData["Ok"] = "Image removed.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Redirect($"/ops-33-console/products/{productId}");
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

        if (await _users.IsInRoleAsync(user, Label33Roles.Admin))
            await _users.RemoveFromRoleAsync(user, Label33Roles.Admin);
        if (await _users.IsInRoleAsync(user, Label33Roles.SuperAdmin))
            await _users.RemoveFromRoleAsync(user, Label33Roles.SuperAdmin);

        TempData["Ok"] = $"Removed console access for {user.Email}.";
        return Redirect("/ops-33-console/team");
    }
}

public record TeamMemberVm(Guid Id, string Email, string? DisplayName, string Role);
