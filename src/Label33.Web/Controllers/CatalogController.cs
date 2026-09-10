using Label33.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Label33.Web.Controllers;

public class CatalogController : Controller
{
    private readonly ProductCatalogQuery _catalog;

    public CatalogController(ProductCatalogQuery catalog) => _catalog = catalog;

    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var products = await _catalog.ListPublishedAsync(q, ct);
        return View(products);
    }
}

public class ProductsController : Controller
{
    private readonly ProductCatalogQuery _catalog;

    public ProductsController(ProductCatalogQuery catalog) => _catalog = catalog;

    [HttpGet("/products/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var product = await _catalog.GetBySlugAsync(slug, ct);
        if (product is null) return NotFound();
        return View(product);
    }
}
