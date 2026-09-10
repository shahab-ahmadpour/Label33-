using Label33.Application.Catalog;
using Label33.Application.Orders;
using Label33.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Label33.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController : Controller
{
    private readonly OrderQueryService _orders;

    public DashboardController(OrderQueryService orders) => _orders = orders;

    public IActionResult Index() => View();
}

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ProductsController : Controller
{
    private readonly ProductAdminService _admin;
    private readonly ProductCatalogQuery _catalog;

    public ProductsController(ProductAdminService admin, ProductCatalogQuery catalog)
    {
        _admin = admin;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Admin list reuses published+draft via admin create for now; catalog query is published-only.
        // Placeholder until UI docs arrive.
        var published = await _catalog.ListPublishedAsync(ct: ct);
        return View(published);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string sku, decimal price, int stock, CancellationToken ct)
    {
        var product = await _admin.CreateAsync(
            name,
            null,
            ProductType.Physical,
            [(sku, "Default", price, StockMode.Tracked, stock)],
            ct);
        await _admin.PublishAsync(product.Id, ct);
        return RedirectToAction(nameof(Index));
    }
}
