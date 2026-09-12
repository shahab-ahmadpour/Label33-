using Label33.Application.Abstractions;
using Label33.Application.Carts;
using Label33.Application.Catalog;
using Label33.Application.Checkout;
using Label33.Application.Coupons;
using Label33.Application.Fulfillment;
using Label33.Application.Inventory;
using Label33.Application.Payments;
using Label33.Domain.Enums;
using Label33.Infrastructure.Identity;
using Label33.Infrastructure.Payments;
using Label33.Infrastructure.Persistence;
using Label33.Infrastructure.Storage;
using Label33.Infrastructure.Time;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Label33.Tests.Application;

public class CheckoutPaymentFlowTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private InventoryService _inventory = null!;
    private CartService _carts = null!;
    private CheckoutService _checkout = null!;
    private PaymentOrchestrator _payments = null!;
    private ProductAdminService _admin = null!;
    private readonly IClock _clock = new SystemClock();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new AppDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _inventory = new InventoryService(_db, _clock);
        _carts = new CartService(_db, _clock, _inventory);
        var coupons = new CouponService(_db, _clock);
        _checkout = new CheckoutService(_db, _clock, new OrderNumberGenerator(_db), _inventory, coupons);
        var fulfillment = new FulfillmentService(_db, _clock);
        _payments = new PaymentOrchestrator(_db, new MockPaymentGateway(), _clock, _inventory, fulfillment, new NullEmailSender());
        _admin = new ProductAdminService(_db, _clock, new StubFileStorage());
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class StubFileStorage : IFileStorage
    {
        public Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
            => Task.FromResult($"test/{Guid.NewGuid():N}_{fileName}");

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream>(new MemoryStream());
    }

    [Fact]
    public async Task Physical_checkout_payment_consumes_stock()
    {
        var product = await _admin.CreateAsync(
            "Tee Shirt",
            "Soft tee",
            ProductType.Physical,
            [("TEE-BLK-M", "Black / M", 450000, StockMode.Tracked, 5)]);
        await _admin.PublishAsync(product.Id);

        var variantId = product.Variants.First().Id;
        var userId = Guid.NewGuid();
        var cart = await _carts.GetOrCreateAsync(userId, null);
        await _carts.AddItemAsync(cart.Id, variantId, 2);

        var checkout = await _checkout.CheckoutAsync(
            cart.Id,
            userId,
            new CheckoutAddressDto("Ali", "09120000000", "Tehran", "Tehran", "1234567890", "Street 1", null),
            couponCode: null,
            shippingTotal: 50000);

        Assert.True(checkout.GrandTotal > 0);

        var start = await _payments.StartAsync(checkout.OrderId, "https://localhost/Payments/Callback");
        await _payments.VerifyAndCompleteAsync(start.ProviderRef, null);

        var inventory = await _db.InventoryItems.SingleAsync(i => i.ProductVariantId == variantId);
        Assert.Equal(3, inventory.QuantityOnHand);
        Assert.Equal(0, inventory.QuantityReserved);

        var order = await _db.Orders.Include(o => o.Shipments).SingleAsync(o => o.Id == checkout.OrderId);
        Assert.Equal(OrderStatus.Fulfilling, order.Status);
        Assert.Single(order.Shipments);
    }
}
