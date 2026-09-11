using Label33.Application.Abstractions;
using Label33.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Orders;

public class OrderQueryService
{
    private readonly IAppDbContext _db;

    public OrderQueryService(IAppDbContext db) => _db = db;

    public Task<Order?> GetAsync(Guid orderId, CancellationToken ct = default)
        => _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Events)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Payments)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public async Task<IReadOnlyList<Order>> ListForUserAsync(Guid userId, CancellationToken ct = default)
        => await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> ListRecentAsync(int take = 20, CancellationToken ct = default)
        => await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);
}
