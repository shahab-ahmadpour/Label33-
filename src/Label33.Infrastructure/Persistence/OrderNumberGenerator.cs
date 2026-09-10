using Label33.Application.Abstractions;
using Label33.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Label33.Infrastructure.Persistence;

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly AppDbContext _db;

    public OrderNumberGenerator(AppDbContext db) => _db = db;

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var day = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"33L-{day}-";
        var count = await _db.Orders.CountAsync(o => o.OrderNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
