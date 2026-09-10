using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Catalog;

public sealed record ProductListItemDto(Guid Id, string Name, string Slug, decimal? FromPrice, bool IsFeatured, string? PrimaryImage);
public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? FullDescription,
    IReadOnlyList<VariantDto> Variants,
    IReadOnlyList<string> Images);

public sealed record VariantDto(Guid Id, string Sku, string Title, decimal BasePrice, decimal? CompareAtPrice, bool IsActive, StockMode StockMode, int? Available);

public class ProductCatalogQuery
{
    private readonly IAppDbContext _db;

    public ProductCatalogQuery(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductListItemDto>> ListPublishedAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Slug.Contains(term));
        }

        var products = await query.OrderByDescending(p => p.PublishedAtUtc).Take(100).ToListAsync(ct);
        return products.Select(p => new ProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.BasePrice).DefaultIfEmpty().Min(),
            p.IsFeatured,
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.PathOrUrl).FirstOrDefault()
        )).ToList();
    }

    public async Task<ProductDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ProductStatus.Published, ct);

        if (product is null)
            return null;

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.FullDescription,
            product.Variants.Where(v => v.IsActive).Select(v => new VariantDto(
                v.Id, v.Sku, v.Title, v.BasePrice, v.CompareAtPrice, v.IsActive, v.StockMode,
                v.StockMode == StockMode.Unlimited ? null : v.InventoryItem?.Available
            )).ToList(),
            product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.PathOrUrl).ToList()
        );
    }
}

public class ProductAdminService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ProductAdminService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Product> CreateAsync(
        string name,
        string? shortDescription,
        ProductType type,
        IEnumerable<(string Sku, string Title, decimal Price, StockMode StockMode, int? OnHand)> variants,
        CancellationToken ct = default)
    {
        var slug = SlugHelper.ToSlug(name);
        if (await _db.Products.AnyAsync(p => p.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var product = new Product
        {
            Name = name,
            Slug = slug,
            ShortDescription = shortDescription,
            ProductType = type,
            Status = ProductStatus.Draft
        };

        foreach (var v in variants)
        {
            var variant = new ProductVariant
            {
                Sku = v.Sku,
                Title = v.Title,
                BasePrice = v.Price,
                StockMode = v.StockMode,
                IsActive = true
            };
            if (v.StockMode == StockMode.Tracked)
            {
                variant.InventoryItem = new InventoryItem
                {
                    ProductVariantId = variant.Id,
                    QuantityOnHand = v.OnHand ?? 0,
                    QuantityReserved = 0
                };
            }
            product.Variants.Add(variant);
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task PublishAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        if (!product.Variants.Any(v => v.IsActive))
            throw new DomainException("Cannot publish product without active variants.");

        product.Status = ProductStatus.Published;
        product.PublishedAtUtc = _clock.UtcNow;
        product.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
