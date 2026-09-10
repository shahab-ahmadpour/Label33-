using Label33.Application.Carts;
using Label33.Application.Checkout;
using Label33.Application.Payments;
using Label33.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Label33.Web.Controllers;

public class CartController : Controller
{
    private readonly CartService _carts;
    public const string AnonCookie = "label33_cart";

    public CartController(CartService carts) => _carts = carts;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await GetCartAsync(ct);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid variantId, int quantity = 1, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(ct);
        await _carts.AddItemAsync(cart.Id, variantId, quantity, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<Cart> GetCartAsync(CancellationToken ct)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(id, out var parsed))
                userId = parsed;
        }

        string? anon = Request.Cookies[AnonCookie];
        if (string.IsNullOrWhiteSpace(anon) && userId is null)
        {
            anon = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(AnonCookie, anon, new CookieOptions { HttpOnly = true, IsEssential = true, Expires = DateTimeOffset.UtcNow.AddDays(30) });
        }

        return await _carts.GetOrCreateAsync(userId, anon, ct);
    }
}

[Authorize]
public class CheckoutController : Controller
{
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
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(
        string fullName, string phone, string province, string city, string postalCode, string line1, string? line2, string? couponCode,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cart = await _carts.GetOrCreateAsync(userId, null, ct);
        var result = await _checkout.CheckoutAsync(
            cart.Id,
            userId,
            new CheckoutAddressDto(fullName, phone, province, city, postalCode, line1, line2),
            couponCode,
            shippingTotal: 0,
            ct);

        var callback = Url.Action("Callback", "Payments", null, Request.Scheme)!;
        var payment = await _payments.StartAsync(result.OrderId, callback, ct);
        if (!string.IsNullOrWhiteSpace(payment.RedirectUrl))
            return Redirect(payment.RedirectUrl);

        return RedirectToAction("Details", "Orders", new { id = result.OrderId });
    }
}

public class PaymentsController : Controller
{
    private readonly PaymentOrchestrator _payments;

    public PaymentsController(PaymentOrchestrator payments) => _payments = payments;

    [HttpGet]
    public async Task<IActionResult> Callback(string providerRef, CancellationToken ct)
    {
        await _payments.VerifyAndCompleteAsync(providerRef, Request.QueryString.Value, ct);
        return Content($"Payment verified for {providerRef}. Backend OK — UI pending.");
    }
}
