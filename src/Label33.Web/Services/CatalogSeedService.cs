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
        await _admin.EnsureCatalogDefaultsAsync(ct);

        if (await _db.Products.AnyAsync(ct))
            return;

        var categories = await _admin.ListCategoriesAsync(ct);
        var byName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

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
            byName.TryGetValue(sample.Category, out var categoryId);
            var sku = $"33-{sample.Category[..3]}-{Guid.NewGuid().ToString("N")[..4]}".ToUpperInvariant();
            await _admin.CreateRichAsync(
                sample.Name,
                sample.Category,
                null,
                categoryId == Guid.Empty ? null : categoryId,
                isFeatured: false,
                ProductType.Physical,
                sku,
                sample.Price,
                [
                    new SizeStockInput("M", 25, sample.Price),
                    new SizeStockInput("L", 25, sample.Price),
                    new SizeStockInput("XL", 15, sample.Price)
                ],
                publish: true,
                ct);
        }
    }
}
