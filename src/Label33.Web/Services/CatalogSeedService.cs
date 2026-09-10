using Label33.Application.Catalog;
using Label33.Domain.Enums;
using Label33.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Label33.Web.Services;

public class CatalogSeedService
{
    private readonly AppDbContext _db;
    private readonly ProductAdminService _admin;

    public CatalogSeedService(AppDbContext db, ProductAdminService admin)
    {
        _db = db;
        _admin = admin;
    }

    public async Task EnsureSeedAsync(CancellationToken ct = default)
    {
        if (await _db.Products.AnyAsync(ct))
            return;

        var samples = new (string Name, string Category, decimal Price)[]
        {
            ("Everyday Tee 33", "T-SHIRTS", 890000),
            ("Cream Core Tee", "T-SHIRTS", 920000),
            ("Charcoal Hoodie", "HOODIES", 1890000),
            ("Mustard Hoodie", "HOODIES", 1950000),
            ("Sage Sweatshirt", "SWEATSHIRTS", 1650000),
            ("Editorial Sweat", "SWEATSHIRTS", 1720000),
            ("Warm Jogger", "JOGGERS", 1480000),
            ("Olive Jogger", "JOGGERS", 1480000)
        };

        foreach (var sample in samples)
        {
            var sku = $"33-{sample.Category[..3]}-{Guid.NewGuid().ToString("N")[..4]}".ToUpperInvariant();
            var product = await _admin.CreateAsync(
                sample.Name,
                sample.Category,
                ProductType.Physical,
                [
                    (sku + "-M", "M", sample.Price, StockMode.Tracked, 25),
                    (sku + "-L", "L", sample.Price, StockMode.Tracked, 25),
                    (sku + "-XL", "XL", sample.Price, StockMode.Tracked, 15)
                ],
                ct);
            await _admin.PublishAsync(product.Id, ct);
        }
    }
}
