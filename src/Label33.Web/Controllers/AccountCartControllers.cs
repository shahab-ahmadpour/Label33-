using Label33.Application.Carts;
using Label33.Application.Catalog;
using Label33.Application.Checkout;
using Label33.Application.Common;
using Label33.Application.Orders;
using Label33.Application.Payments;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Label33.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Label33.Web.Controllers;

public class CartController : Controller
{
    private readonly CartService _carts;
    private readonly ProductCatalogQuery _catalog;
    public const string AnonCookie = "label33_cart";

    public CartController(CartService carts, ProductCatalogQuery catalog)
    {
        _carts = carts;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Cart";
        var cart = await GetCartAsync(ct);
        var view = await _carts.GetViewAsync(cart.Id, ct);
        return View(view);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid variantId, int quantity = 1, string? returnUrl = null, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(ct);
        await _carts.AddItemAsync(cart.Id, variantId, quantity, ct);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(Guid variantId, int quantity, CancellationToken ct)
    {
        var cart = await GetCartAsync(ct);
        await _carts.UpdateQuantityAsync(cart.Id, variantId, quantity, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid variantId, CancellationToken ct)
    {
        var cart = await GetCartAsync(ct);
        await _carts.RemoveItemAsync(cart.Id, variantId, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdd(string slug, CancellationToken ct)
    {
        var product = await _catalog.GetBySlugAsync(slug, ct);
        if (product is null) return NotFound();

        var variant = product.Variants.FirstOrDefault(v =>
            v.StockMode == StockMode.Unlimited || (v.Available ?? 0) > 0)
            ?? product.Variants.FirstOrDefault();

        if (variant is null) return RedirectToAction("Details", "Products", new { slug });
        if (variant.StockMode == StockMode.Tracked && (variant.Available ?? 0) <= 0)
            return RedirectToAction("Details", "Products", new { slug });

        var cart = await GetCartAsync(ct);
        await _carts.AddItemAsync(cart.Id, variant.Id, 1, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<Cart> GetCartAsync(CancellationToken ct)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true
            && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed))
            userId = parsed;

        string? anon = Request.Cookies[AnonCookie];
        if (string.IsNullOrWhiteSpace(anon) && userId is null)
        {
            anon = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(AnonCookie, anon, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        return await _carts.GetOrCreateAsync(userId, anon, ct);
    }
}

public class CheckoutController : Controller
{
    public const decimal FlatShippingRials = 500_000m; // 50,000 Toman

    private readonly CartService _carts;
    private readonly CheckoutService _checkout;
    private readonly PaymentOrchestrator _payments;

    public CheckoutController(CartService carts, CheckoutService checkout, PaymentOrchestrator payments)
    {
        _carts = carts;
        _checkout = checkout;
        _payments = payments;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Checkout";
        ViewData["ShippingRials"] = FlatShippingRials;
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(
        string fullName,
        string phone,
        string province,
        string city,
        string postalCode,
        string line1,
        string? line2,
        string? couponCode,
        CancellationToken ct)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var cart = await _carts.GetOrCreateAsync(userId, null, ct);
            var result = await _checkout.CheckoutAsync(
                cart.Id,
                userId,
                new CheckoutAddressDto(fullName, phone, province, city, postalCode, line1, line2),
                couponCode,
                shippingTotal: FlatShippingRials,
                ct);

            var callback = Url.Action("Callback", "Payments", null, Request.Scheme)!;
            var payment = await _payments.StartAsync(result.OrderId, callback, ct);
            if (!string.IsNullOrWhiteSpace(payment.RedirectUrl))
                return Redirect(payment.RedirectUrl);

            TempData["Ok"] = result.OrderNumber;
            return RedirectToAction("Callback", "Payments", new { providerRef = payment.ProviderRef });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

public class PaymentsController : Controller
{
    private readonly PaymentOrchestrator _payments;
    private readonly OrderQueryService _orders;

    public PaymentsController(PaymentOrchestrator payments, OrderQueryService orders)
    {
        _payments = payments;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Callback(string providerRef, CancellationToken ct)
    {
        try
        {
            await _payments.VerifyAndCompleteAsync(providerRef, Request.QueryString.Value, ct);
            var orderNumber = await _orders.GetOrderNumberByProviderRefAsync(providerRef, ct);
            ViewData["Title"] = "Paid";
            return View("Callback", new PaymentCallbackVm(true, orderNumber, providerRef));
        }
        catch (DomainException ex)
        {
            ViewData["Title"] = "Payment";
            return View("Callback", new PaymentCallbackVm(false, null, providerRef, ex.Message));
        }
    }
}

public sealed record PaymentCallbackVm(bool Succeeded, string? OrderNumber, string ProviderRef, string? Error = null);

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly CartService _carts;
    private readonly OrderQueryService _orders;

    public AccountController(
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        CartService carts,
        OrderQueryService orders)
    {
        _signIn = signIn;
        _users = users;
        _carts = carts;
        _orders = orders;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "Login";
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var user = await _users.FindByEmailAsync(email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login.");
            return View();
        }

        var result = await _signIn.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid login.");
            return View();
        }

        var anon = Request.Cookies[CartController.AnonCookie];
        if (!string.IsNullOrWhiteSpace(anon))
            await _carts.MergeAnonymousIntoUserAsync(anon, user.Id);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        ViewData["Title"] = "Register";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string? displayName)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };
        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }

        await _signIn.SignInAsync(user, isPersistent: true);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Account";
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var orders = await _orders.ListForUserAsync(userId, ct);
        return View(orders);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Order(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _orders.GetForUserAsync(id, userId, ct);
        if (order is null) return NotFound();
        ViewData["Title"] = order.OrderNumber;
        return View(order);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
