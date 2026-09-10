using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Wishlist;

public class WishlistService
{
    private readonly IAppDbContext _db;

    public WishlistService(IAppDbContext db) => _db = db;

    public async Task<bool> ToggleAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        var existing = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);
        if (existing is not null)
        {
            _db.WishlistItems.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return false;
        }

        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new DomainException("Product not found.");

        _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
