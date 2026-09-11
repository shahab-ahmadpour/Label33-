using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Catalog;

public sealed record ProductListItemDto(Guid Id, string Name, string Slug, string? ShortDescription, decimal? FromPrice, bool IsFeatured, string? PrimaryImage);
public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? FullDescription,
    IReadOnlyList<VariantDto> Variants,
    IReadOnlyList<string> Images);

public sealed record VariantDto(Guid Id, string Sku, string Title, decimal BasePrice, decimal? CompareAtPrice, bool IsActive, StockMode StockMode, int? Available);

public sealed record AdminProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    ProductStatus Status,
    string? ShortDescription,
    decimal? FromPrice,
    int VariantCount,
    int? TotalOnHand,
    string? PrimaryImage);

public sealed record AdminProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? FullDescription,
    ProductStatus Status,
    ProductType ProductType,
    bool IsFeatured,
    IReadOnlyList<AdminVariantDto> Variants,
    IReadOnlyList<AdminImageDto> Images);

public sealed record AdminVariantDto(
    Guid Id,
    string Sku,
    string Title,
    decimal BasePrice,
    bool IsActive,
    StockMode StockMode,
    int QuantityOnHand,
    int QuantityReserved,
    int Available);

public sealed record AdminImageDto(Guid Id, string PathOrUrl, bool IsPrimary, int SortOrder, string? AltText);

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
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.Contains(term)));
        }

        var products = await query.OrderByDescending(p => p.PublishedAtUtc).Take(100).ToListAsync(ct);
        return products.Select(p => new ProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.ShortDescription,
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
    private readonly IFileStorage _files;

    public ProductAdminService(IAppDbContext db, IClock clock, IFileStorage files)
    {
        _db = db;
        _clock = clock;
        _files = files;
    }

    public async Task<IReadOnlyList<AdminProductListItemDto>> ListAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.Contains(term)));
        }

        var products = await query.OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc).Take(200).ToListAsync(ct);
        return products.Select(ToListItem).ToList();
    }

    public async Task<AdminProductDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return product is null ? null : ToDetail(product);
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

    public async Task UpdateAsync(
        Guid productId,
        string name,
        string? shortDescription,
        string? fullDescription,
        bool isFeatured,
        CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        product.Name = name.Trim();
        product.ShortDescription = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        product.FullDescription = string.IsNullOrWhiteSpace(fullDescription) ? null : fullDescription.Trim();
        product.IsFeatured = isFeatured;
        product.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateVariantAsync(
        Guid variantId,
        string title,
        decimal price,
        bool isActive,
        CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new DomainException("Variant not found.");

        variant.Title = string.IsNullOrWhiteSpace(title) ? variant.Title : title.Trim();
        variant.BasePrice = price;
        variant.IsActive = isActive;
        variant.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetInventoryAsync(Guid variantId, int quantityOnHand, CancellationToken ct = default)
    {
        if (quantityOnHand < 0)
            throw new DomainException("Stock cannot be negative.");

        var variant = await _db.ProductVariants
            .Include(v => v.InventoryItem)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct)
            ?? throw new DomainException("Variant not found.");

        if (variant.StockMode != StockMode.Tracked)
            throw new DomainException("Variant is not inventory-tracked.");

        if (variant.InventoryItem is null)
        {
            variant.InventoryItem = new InventoryItem
            {
                ProductVariantId = variant.Id,
                QuantityOnHand = quantityOnHand,
                QuantityReserved = 0
            };
            _db.InventoryItems.Add(variant.InventoryItem);
        }
        else
        {
            if (quantityOnHand < variant.InventoryItem.QuantityReserved)
                throw new DomainException($"On-hand cannot be below reserved ({variant.InventoryItem.QuantityReserved}).");

            variant.InventoryItem.QuantityOnHand = quantityOnHand;
            variant.InventoryItem.UpdatedAtUtc = _clock.UtcNow;
        }

        variant.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        product.Status = ProductStatus.Archived;
        product.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        if (product.Status == ProductStatus.Published)
            throw new DomainException("Archive the product before deleting, or unpublish first by archiving.");

        var referenced = await _db.OrderItems.AnyAsync(i => product.Variants.Select(v => v.Id).Contains(i.ProductVariantId), ct);
        if (referenced)
            throw new DomainException("Product has order history. Archive it instead of deleting.");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductImage> AddImageAsync(
        Guid productId,
        Stream content,
        string fileName,
        string contentType,
        bool makePrimary,
        string? altText,
        CancellationToken ct = default)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new DomainException("Only JPEG, PNG, WebP, or GIF images are allowed.");

        var key = await _files.SaveAsync(content, fileName, contentType, ct);
        var path = "/media/" + key.Replace('\\', '/');

        if (makePrimary || product.Images.Count == 0)
        {
            foreach (var img in product.Images)
                img.IsPrimary = false;
        }

        var image = new ProductImage
        {
            ProductId = product.Id,
            PathOrUrl = path,
            SortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            IsPrimary = makePrimary || product.Images.Count == 0
        };

        _db.ProductImages.Add(image);
        product.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return image;
    }

    public async Task SetPrimaryImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId, ct)
            ?? throw new DomainException("Image not found.");

        var siblings = await _db.ProductImages.Where(i => i.ProductId == image.ProductId).ToListAsync(ct);
        foreach (var img in siblings)
            img.IsPrimary = img.Id == imageId;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveImageAsync(Guid imageId, CancellationToken ct = default)
    {
        var image = await _db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId, ct)
            ?? throw new DomainException("Image not found.");

        var productId = image.ProductId;
        var wasPrimary = image.IsPrimary;
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync(ct);

        if (wasPrimary)
        {
            var next = await _db.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefaultAsync(ct);
            if (next is not null)
            {
                next.IsPrimary = true;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private static AdminProductListItemDto ToListItem(Product p)
    {
        var tracked = p.Variants.Where(v => v.StockMode == StockMode.Tracked && v.InventoryItem is not null).ToList();
        return new AdminProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Status,
            p.ShortDescription,
            p.Variants.Where(v => v.IsActive).Select(v => (decimal?)v.BasePrice).DefaultIfEmpty().Min(),
            p.Variants.Count,
            tracked.Count == 0 ? null : tracked.Sum(v => v.InventoryItem!.QuantityOnHand),
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.PathOrUrl).FirstOrDefault()
        );
    }

    private static AdminProductDetailDto ToDetail(Product p) => new(
        p.Id,
        p.Name,
        p.Slug,
        p.ShortDescription,
        p.FullDescription,
        p.Status,
        p.ProductType,
        p.IsFeatured,
        p.Variants.Select(v => new AdminVariantDto(
            v.Id,
            v.Sku,
            v.Title,
            v.BasePrice,
            v.IsActive,
            v.StockMode,
            v.InventoryItem?.QuantityOnHand ?? 0,
            v.InventoryItem?.QuantityReserved ?? 0,
            v.StockMode == StockMode.Unlimited ? int.MaxValue : (v.InventoryItem?.Available ?? 0)
        )).ToList(),
        p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
            .Select(i => new AdminImageDto(i.Id, i.PathOrUrl, i.IsPrimary, i.SortOrder, i.AltText)).ToList()
    );
}
