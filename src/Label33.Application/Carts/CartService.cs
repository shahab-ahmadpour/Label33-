using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Application.Inventory;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Carts;

public class CartService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly InventoryService _inventory;

    public CartService(IAppDbContext db, IClock clock, InventoryService inventory)
    {
        _db = db;
        _clock = clock;
        _inventory = inventory;
    }

    public async Task<Domain.Entities.Cart> GetOrCreateAsync(Guid? userId, string? anonymousToken, CancellationToken ct = default)
    {
        Domain.Entities.Cart? cart = null;

        if (userId.HasValue)
        {
            cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Open, ct);
        }
        else if (!string.IsNullOrWhiteSpace(anonymousToken))
        {
            cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.AnonymousToken == anonymousToken && c.Status == CartStatus.Open, ct);
        }

        if (cart is not null)
            return cart;

        cart = new Domain.Entities.Cart
        {
            UserId = userId,
            AnonymousToken = userId.HasValue ? null : anonymousToken ?? Guid.NewGuid().ToString("N"),
            Currency = "IRR",
            Status = CartStatus.Open
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }

    public async Task AddItemAsync(Guid cartId, Guid variantId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.IsActive, ct)
            ?? throw new DomainException("Variant not found or inactive.");

        if (variant.Product.Status != ProductStatus.Published)
            throw new DomainException("Product is not published.");

        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct)
            ?? throw new DomainException("Cart not found.");

        if (cart.Status != CartStatus.Open)
            throw new DomainException("Cart is not open.");

        var existing = cart.Items.FirstOrDefault(i => i.ProductVariantId == variantId);
        var desiredQty = (existing?.Quantity ?? 0) + quantity;
        await _inventory.EnsureAvailableAsync(variantId, desiredQty, ct);

        if (existing is null)
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductVariantId = variantId,
                Quantity = quantity,
                UnitPriceSnapshot = variant.BasePrice,
                AddedAtUtc = _clock.UtcNow
            });
        }
        else
        {
            existing.Quantity = desiredQty;
            existing.UnitPriceSnapshot = variant.BasePrice;
            existing.UpdatedAtUtc = _clock.UtcNow;
        }

        cart.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CartViewDto> GetViewAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct)
            ?? throw new DomainException("Cart not found.");

        var lines = cart.Items
            .OrderBy(i => i.AddedAtUtc)
            .Select(i =>
            {
                var product = i.ProductVariant.Product;
                var image = product.Images
                    .OrderByDescending(img => img.IsPrimary)
                    .ThenBy(img => img.SortOrder)
                    .Select(img => img.PathOrUrl)
                    .FirstOrDefault();

                return new CartLineDto(
                    i.ProductVariantId,
                    product.Name,
                    product.Slug,
                    i.ProductVariant.Title,
                    image,
                    i.Quantity,
                    i.UnitPriceSnapshot,
                    i.UnitPriceSnapshot * i.Quantity);
            })
            .ToList();

        return new CartViewDto(cart.Id, lines, lines.Sum(l => l.LineTotalRials));
    }

    public Task RemoveItemAsync(Guid cartId, Guid variantId, CancellationToken ct = default)
        => UpdateQuantityAsync(cartId, variantId, 0, ct);

    public async Task UpdateQuantityAsync(Guid cartId, Guid variantId, int quantity, CancellationToken ct = default)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct)
            ?? throw new DomainException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.ProductVariantId == variantId)
            ?? throw new DomainException("Cart item not found.");

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            await _inventory.EnsureAvailableAsync(variantId, quantity, ct);
            item.Quantity = quantity;
            item.UpdatedAtUtc = _clock.UtcNow;
        }

        cart.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MergeAnonymousIntoUserAsync(string anonymousToken, Guid userId, CancellationToken ct = default)
    {
        var guest = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.AnonymousToken == anonymousToken && c.Status == CartStatus.Open, ct);
        if (guest is null)
            return;

        var userCart = await GetOrCreateAsync(userId, null, ct);

        foreach (var item in guest.Items.ToList())
        {
            var existing = userCart.Items.FirstOrDefault(i => i.ProductVariantId == item.ProductVariantId);
            if (existing is null)
            {
                userCart.Items.Add(new CartItem
                {
                    CartId = userCart.Id,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    UnitPriceSnapshot = item.UnitPriceSnapshot,
                    AddedAtUtc = _clock.UtcNow
                });
            }
            else
            {
                existing.Quantity += item.Quantity;
                existing.UpdatedAtUtc = _clock.UtcNow;
            }
        }

        guest.Status = CartStatus.Abandoned;
        guest.UpdatedAtUtc = _clock.UtcNow;
        userCart.UpdatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
