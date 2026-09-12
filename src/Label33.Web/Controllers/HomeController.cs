using Label33.Application.Catalog;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Label33.Web.Controllers;

public class HomeController : Controller
{
    private readonly ProductCatalogQuery _catalog;

    public HomeController(ProductCatalogQuery catalog) => _catalog = catalog;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["ShowIntro"] = true;
        ViewData["DocumentTitle"] = "33-home";
        var products = await _catalog.ListPublishedAsync(ct: ct);
        return View(products.Take(3).ToList());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public class CultureController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl)
    {
        culture = culture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "fa";
        var value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        var options = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/" };
        Response.Cookies.Append("label33.culture", value, options);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}

public class ShopController : Controller
{
    private readonly ProductCatalogQuery _catalog;

    public ShopController(ProductCatalogQuery catalog) => _catalog = catalog;

    [HttpGet("/shop")]
    public async Task<IActionResult> Index(string? category, string? q, CancellationToken ct)
    {
        ViewData["Title"] = "Shop";
        ViewData["Category"] = string.IsNullOrWhiteSpace(category) ? "ALL" : category.ToUpperInvariant();
        ViewData["Query"] = q?.Trim() ?? "";
        var products = await _catalog.ListPublishedAsync(q, ct);
        var selected = (string)ViewData["Category"]!;
        if (!string.Equals(selected, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            products = products
                .Where(p => string.Equals(p.ShortDescription, selected, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

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
        ViewData["Title"] = product.Name;
        return View(product);
    }
}

public class CollectionsController : Controller
{
    private readonly ProductCatalogQuery _catalog;
    public CollectionsController(ProductCatalogQuery catalog) => _catalog = catalog;

    [HttpGet("/collections")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Collections";
        ViewData["Categories"] = await _catalog.ListActiveCategoriesAsync(ct);
        return View(await _catalog.ListPublishedAsync(ct: ct));
    }
}

public class AboutController : Controller
{
    [HttpGet("/about")]
    public IActionResult Index()
    {
        ViewData["Title"] = "About 33";
        return View();
    }
}

public class InfoController : Controller
{
    [HttpGet("/shipping")]
    public IActionResult Shipping()
    {
        ViewData["Title"] = "Shipping";
        return View();
    }

    [HttpGet("/returns")]
    public IActionResult Returns()
    {
        ViewData["Title"] = "Returns";
        return View();
    }

    [HttpGet("/contact")]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact";
        return View();
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy";
        return View();
    }
}

public class DiyarController : Controller
{
    [HttpGet("/diyar")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Diyar";
        return View();
    }
}
