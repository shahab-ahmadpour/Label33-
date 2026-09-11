using Label33.Application.Carts;
using Label33.Application.Catalog;
using Label33.Application.Checkout;
using Label33.Application.Coupons;
using Label33.Application.Fulfillment;
using Label33.Application.Inventory;
using Label33.Application.Orders;
using Label33.Application.Payments;
using Label33.Application.Reviews;
using Label33.Application.Wishlist;
using Microsoft.Extensions.DependencyInjection;

namespace Label33.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLabel33Application(this IServiceCollection services)
    {
        services.AddScoped<InventoryService>();
        services.AddScoped<CartService>();
        services.AddScoped<CouponService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<PaymentOrchestrator>();
        services.AddScoped<FulfillmentService>();
        services.AddScoped<AccessTokenService>();
        services.AddScoped<ProductCatalogQuery>();
        services.AddScoped<ProductAdminService>();
        services.AddScoped<WishlistService>();
        services.AddScoped<ReviewService>();
        services.AddScoped<OrderQueryService>();
        services.AddScoped<OrderAdminService>();
        return services;
    }
}
