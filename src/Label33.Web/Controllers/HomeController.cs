using Label33.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Label33.Web.Controllers;

public class HomeController : Controller
{
    private readonly ProductCatalogQuery _catalog;

    public HomeController(ProductCatalogQuery catalog) => _catalog = catalog;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await _catalog.ListPublishedAsync(ct: ct);
        return View(products);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
