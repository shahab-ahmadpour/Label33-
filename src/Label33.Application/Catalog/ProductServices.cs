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

public sealed record CategoryDto(Guid Id, string Name, string Slug);

public sealed record AdminProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    ProductStatus Status,
    string? Category,
    Guid? CategoryId,
    decimal? FromPrice,
    int VariantCount,
    int TotalOnHand,
    int TotalReserved,
    int Available,
    int OutOfStockSizes,
    string? PrimaryImage,
    DateTime UpdatedAtUtc);

public sealed record AdminProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? FullDescription,
    ProductStatus Status,
    ProductType ProductType,
    bool IsFeatured,
    Guid? CategoryId,
    string? CategoryName,
    string? SkuPrefix,
    decimal BasePrice,
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

public sealed record WarehouseStatsDto(
    int ProductCount,
    int PublishedCount,
    int DraftCount,
    int ArchivedCount,
    int VariantCount,
    int TotalOnHand,
    int TotalReserved,
    int Available,
    int LowStockVariants,
    int OutOfStockVariants);

public enum AdminProductSort
{
    UpdatedDesc = 0,
    UpdatedAsc = 1,
    PriceAsc = 2,
    PriceDesc = 3,
    StockDesc = 4,
    StockAsc = 5,
    NameAsc = 6
}

public sealed record SizeStockInput(string Size, int Stock, decimal? Price = null, bool Enabled = true);

public static class CatalogDefaults
{
    public static readonly string[] Categories = ["T-SHIRTS", "HOODIES", "SWEATSHIRTS", "JOGGERS", "ACCESSORIES"];
    public static readonly string[] Sizes = ["XS", "S", "M", "L", "XL", "XXL"];
    public const int LowStockThreshold = 5;
}

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

    public async Task<IReadOnlyList<CategoryDto>> ListActiveCategoriesAsync(CancellationToken ct = default)
        => await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
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

    public async Task EnsureCatalogDefaultsAsync(CancellationToken ct = default)
    {
        foreach (var name in CatalogDefaults.Categories)
        {
            var slug = SlugHelper.ToSlug(name);
            if (await _db.Categories.AnyAsync(c => c.Slug == slug, ct))
                continue;

            _db.Categories.Add(new Category
            {
                Name = name,
                Slug = slug,
                SortOrder = Array.IndexOf(CatalogDefaults.Categories, name),
                IsActive = true
            });
        }

        await _db.SaveChangesAsync(ct);
        await BackfillProductCategoriesAsync(ct);
    }

    private async Task BackfillProductCategoriesAsync(CancellationToken ct)
    {
        var orphans = await _db.Products
            .Include(p => p.ProductCategories)
            .Where(p => !p.ProductCategories.Any() && p.ShortDescription != null)
            .ToListAsync(ct);
        if (orphans.Count == 0)
            return;

        var cats = await _db.Categories.ToListAsync(ct);
        foreach (var product in orphans)
        {
            var cat = cats.FirstOrDefault(c =>
                string.Equals(c.Name, product.ShortDescription, StringComparison.OrdinalIgnoreCase));
            if (cat is null)
                continue;
            product.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = cat.Id
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken ct = default)
    {
        await EnsureCatalogDefaultsAsync(ct);
        return await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
    }

    public async Task<WarehouseStatsDto> GetWarehouseStatsAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .ToListAsync(ct);

        var variants = products.SelectMany(p => p.Variants).Where(v => v.IsActive).ToList();
        var tracked = variants.Where(v => v.StockMode == StockMode.Tracked).ToList();
        var onHand = tracked.Sum(v => v.InventoryItem?.QuantityOnHand ?? 0);
        var reserved = tracked.Sum(v => v.InventoryItem?.QuantityReserved ?? 0);
        var available = onHand - reserved;

        return new WarehouseStatsDto(
            products.Count,
            products.Count(p => p.Status == ProductStatus.Published),
            products.Count(p => p.Status == ProductStatus.Draft),
            products.Count(p => p.Status == ProductStatus.Archived),
            variants.Count,
            onHand,
            reserved,
            available,
            tracked.Count(v => (v.InventoryItem?.Available ?? 0) > 0 && (v.InventoryItem?.Available ?? 0) <= CatalogDefaults.LowStockThreshold),
            tracked.Count(v => (v.InventoryItem?.Available ?? 0) <= 0));
    }

    public async Task<IReadOnlyList<AdminProductListItemDto>> ListAllAsync(
        string? search = null,
        Guid? categoryId = null,
        ProductStatus? status = null,
        AdminProductSort sort = AdminProductSort.UpdatedDesc,
        CancellationToken ct = default)
    {
        await EnsureCatalogDefaultsAsync(ct);

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.ShortDescription != null && p.ShortDescription.Contains(term)) ||
                p.Variants.Any(v => v.Sku.Contains(term)));
        }

        if (categoryId is not null)
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId));

        if (status is not null)
            query = query.Where(p => p.Status == status);

        var products = await query.ToListAsync(ct);
        var items = products.Select(ToListItem).ToList();

        return sort switch
        {
            AdminProductSort.UpdatedAsc => items.OrderBy(i => i.UpdatedAtUtc).ToList(),
            AdminProductSort.PriceAsc => items.OrderBy(i => i.FromPrice ?? decimal.MaxValue).ToList(),
            AdminProductSort.PriceDesc => items.OrderByDescending(i => i.FromPrice ?? 0).ToList(),
            AdminProductSort.StockDesc => items.OrderByDescending(i => i.Available).ToList(),
            AdminProductSort.StockAsc => items.OrderBy(i => i.Available).ToList(),
            AdminProductSort.NameAsc => items.OrderBy(i => i.Name).ToList(),
            _ => items.OrderByDescending(i => i.UpdatedAtUtc).ToList()
        };
    }

    public async Task<AdminProductDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Images)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
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
        var list = variants.ToList();
        if (list.Count == 0)
            throw new DomainException("At least one variant is required.");

        var skuPrefix = list[0].Sku.Contains('-')
            ? list[0].Sku[..list[0].Sku.LastIndexOf('-')]
            : list[0].Sku;

        return await CreateRichAsync(
            name,
            shortDescription,
            null,
            categoryId: null,
            isFeatured: false,
            type,
            skuPrefix,
            list[0].Price,
            list.Select(v => new SizeStockInput(v.Title, v.OnHand ?? 0, v.Price)).ToList(),
            publish: false,
            ct);
    }

    public async Task<Product> CreateRichAsync(
        string name,
        string? shortDescription,
        string? fullDescription,
        Guid? categoryId,
        bool isFeatured,
        ProductType type,
        string skuPrefix,
        decimal basePrice,
        IReadOnlyList<SizeStockInput> sizes,
        bool publish,
        CancellationToken ct = default)
    {
        await EnsureCatalogDefaultsAsync(ct);

        var enabled = sizes.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Size)).ToList();
        if (enabled.Count == 0)
            throw new DomainException("Select at least one size with stock.");

        var slug = SlugHelper.ToSlug(name);
        if (await _db.Products.AnyAsync(p => p.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        Category? category = null;
        if (categoryId is not null)
            category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct)
                ?? throw new DomainException("Category not found.");

        var categoryLabel = category?.Name
            ?? (string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim().ToUpperInvariant());

        var product = new Product
        {
            Name = name.Trim(),
            Slug = slug,
            ShortDescription = categoryLabel,
            FullDescription = string.IsNullOrWhiteSpace(fullDescription) ? null : fullDescription.Trim(),
            ProductType = type,
            IsFeatured = isFeatured,
            Status = ProductStatus.Draft
        };

        var sizeAttr = new ProductAttribute { Name = "Size", SortOrder = 0 };
        product.Attributes.Add(sizeAttr);

        var prefix = string.IsNullOrWhiteSpace(skuPrefix)
            ? $"33-{Guid.NewGuid().ToString("N")[..6]}"
            : skuPrefix.Trim().ToUpperInvariant();

        foreach (var size in enabled.OrderBy(s =>
                     Array.IndexOf(CatalogDefaults.Sizes, s.Size.Trim().ToUpperInvariant()) is var i and >= 0 ? i : 99))
        {
            var code = size.Size.Trim().ToUpperInvariant();
            var attrValue = new ProductAttributeValue { Value = code, SortOrder = Array.IndexOf(CatalogDefaults.Sizes, code) };
            sizeAttr.Values.Add(attrValue);

            var variant = new ProductVariant
            {
                Sku = $"{prefix}-{code}",
                Title = code,
                BasePrice = size.Price ?? basePrice,
                StockMode = StockMode.Tracked,
                IsActive = true,
                InventoryItem = new InventoryItem
                {
                    QuantityOnHand = Math.Max(0, size.Stock),
                    QuantityReserved = 0
                }
            };
            variant.AttributeValues.Add(new VariantAttributeValue { ProductAttributeValue = attrValue });
            product.Variants.Add(variant);
        }

        if (category is not null)
            product.ProductCategories.Add(new ProductCategory { CategoryId = category.Id });

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        if (publish)
            await PublishAsync(product.Id, ct);

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
        await UpdateDetailsAsync(productId, name, fullDescription, isFeatured, null, ct);
    }

    public async Task UpdateDetailsAsync(
        Guid productId,
        string name,
        string? fullDescription,
        bool isFeatured,
        Guid? categoryId,
        CancellationToken ct = default)
    {
        await EnsureCatalogDefaultsAsync(ct);
        var product = await _db.Products
            .Include(p => p.ProductCategories)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        product.Name = name.Trim();
        product.FullDescription = string.IsNullOrWhiteSpace(fullDescription) ? null : fullDescription.Trim();
        product.IsFeatured = isFeatured;
        product.UpdatedAtUtc = _clock.UtcNow;

        product.ProductCategories.Clear();
        if (categoryId is not null)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct)
                ?? throw new DomainException("Category not found.");
            product.ProductCategories.Add(new ProductCategory { ProductId = product.Id, CategoryId = category.Id });
            product.ShortDescription = category.Name;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SyncSizeMatrixAsync(
        Guid productId,
        decimal basePrice,
        IReadOnlyList<SizeStockInput> sizes,
        CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.Variants).ThenInclude(v => v.InventoryItem)
            .Include(p => p.Attributes).ThenInclude(a => a.Values)
            .FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new DomainException("Product not found.");

        var sizeAttr = product.Attributes.FirstOrDefault(a => a.Name == "Size");
        if (sizeAttr is null)
        {
            sizeAttr = new ProductAttribute { ProductId = product.Id, Name = "Size", SortOrder = 0 };
            _db.ProductAttributes.Add(sizeAttr);
            product.Attributes.Add(sizeAttr);
        }

        var prefix = InferSkuPrefix(product);
        var enabled = sizes.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Size)).ToList();
        if (enabled.Count == 0)
            throw new DomainException("Keep at least one size enabled.");

        var enabledCodes = enabled.Select(s => s.Size.Trim().ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var variant in product.Variants)
        {
            var code = variant.Title.Trim().ToUpperInvariant();
            if (!enabledCodes.Contains(code))
            {
                variant.IsActive = false;
                variant.UpdatedAtUtc = _clock.UtcNow;
            }
        }

        foreach (var size in enabled)
        {
            var code = size.Size.Trim().ToUpperInvariant();
            var price = size.Price ?? basePrice;
            var variant = product.Variants.FirstOrDefault(v => string.Equals(v.Title, code, StringComparison.OrdinalIgnoreCase));
            if (variant is null)
            {
                var attrValue = sizeAttr.Values.FirstOrDefault(v => v.Value == code);
                if (attrValue is null)
                {
                    attrValue = new ProductAttributeValue
                    {
                        ProductAttributeId = sizeAttr.Id,
                        Value = code,
                        SortOrder = Array.IndexOf(CatalogDefaults.Sizes, code)
                    };
                    sizeAttr.Values.Add(attrValue);
                }

                variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Sku = $"{prefix}-{code}",
                    Title = code,
                    BasePrice = price,
                    StockMode = StockMode.Tracked,
                    IsActive = true,
                    InventoryItem = new InventoryItem
                    {
                        QuantityOnHand = Math.Max(0, size.Stock),
                        QuantityReserved = 0
                    }
                };
                variant.AttributeValues.Add(new VariantAttributeValue { ProductAttributeValue = attrValue });
                product.Variants.Add(variant);
            }
            else
            {
                variant.IsActive = true;
                variant.BasePrice = price;
                variant.UpdatedAtUtc = _clock.UtcNow;
                if (variant.StockMode == StockMode.Tracked)
                {
                    if (variant.InventoryItem is null)
                    {
                        variant.InventoryItem = new InventoryItem
                        {
                            ProductVariantId = variant.Id,
                            QuantityOnHand = Math.Max(0, size.Stock),
                            QuantityReserved = 0
                        };
                        _db.InventoryItems.Add(variant.InventoryItem);
                    }
                    else
                    {
                        if (size.Stock < variant.InventoryItem.QuantityReserved)
                            throw new DomainException($"{code}: on-hand cannot be below reserved ({variant.InventoryItem.QuantityReserved}).");
                        variant.InventoryItem.QuantityOnHand = Math.Max(0, size.Stock);
                        variant.InventoryItem.UpdatedAtUtc = _clock.UtcNow;
                    }
                }
            }
        }

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
            throw new DomainException("Archive the product before deleting.");

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

    private static string InferSkuPrefix(Product product)
    {
        var sku = product.Variants.Select(v => v.Sku).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(sku))
            return $"33-{Guid.NewGuid().ToString("N")[..6]}".ToUpperInvariant();
        var idx = sku.LastIndexOf('-');
        return idx > 0 ? sku[..idx] : sku;
    }

    private static AdminProductListItemDto ToListItem(Product p)
    {
        var active = p.Variants.Where(v => v.IsActive).ToList();
        var tracked = active.Where(v => v.StockMode == StockMode.Tracked).ToList();
        var onHand = tracked.Sum(v => v.InventoryItem?.QuantityOnHand ?? 0);
        var reserved = tracked.Sum(v => v.InventoryItem?.QuantityReserved ?? 0);
        var category = p.ProductCategories.Select(pc => pc.Category?.Name).FirstOrDefault()
            ?? p.ShortDescription;

        return new AdminProductListItemDto(
            p.Id,
            p.Name,
            p.Slug,
            p.Status,
            category,
            p.ProductCategories.Select(pc => (Guid?)pc.CategoryId).FirstOrDefault(),
            active.Select(v => (decimal?)v.BasePrice).DefaultIfEmpty().Min(),
            active.Count,
            onHand,
            reserved,
            onHand - reserved,
            tracked.Count(v => (v.InventoryItem?.Available ?? 0) <= 0),
            p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).Select(i => i.PathOrUrl).FirstOrDefault(),
            p.UpdatedAtUtc ?? p.CreatedAtUtc
        );
    }

    private static AdminProductDetailDto ToDetail(Product p)
    {
        var active = p.Variants.Where(v => v.IsActive).ToList();
        var category = p.ProductCategories.Select(pc => pc.Category).FirstOrDefault();
        return new(
            p.Id,
            p.Name,
            p.Slug,
            p.ShortDescription,
            p.FullDescription,
            p.Status,
            p.ProductType,
            p.IsFeatured,
            category?.Id,
            category?.Name ?? p.ShortDescription,
            InferSkuPrefix(p),
            active.Select(v => v.BasePrice).DefaultIfEmpty(0).Min(),
            p.Variants.OrderBy(v => Array.IndexOf(CatalogDefaults.Sizes, v.Title.ToUpperInvariant()))
                .Select(v => new AdminVariantDto(
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
}
