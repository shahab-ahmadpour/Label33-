using Label33.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Brand> Brands { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }
    DbSet<VariantAttributeValue> VariantAttributeValues { get; }
    DbSet<Tag> Tags { get; }
    DbSet<ProductTag> ProductTags { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Collection> Collections { get; }
    DbSet<CollectionItem> CollectionItems { get; }
    DbSet<DigitalAsset> DigitalAssets { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryReservation> InventoryReservations { get; }
    DbSet<Domain.Entities.Cart> Carts { get; }
    DbSet<Domain.Entities.CartItem> CartItems { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<CouponRedemption> CouponRedemptions { get; }
    DbSet<SalesWindow> SalesWindows { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderAddress> OrderAddresses { get; }
    DbSet<OrderEvent> OrderEvents { get; }
    DbSet<PaymentTransaction> PaymentTransactions { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<DigitalEntitlement> DigitalEntitlements { get; }
    DbSet<PurchaseAccessToken> PurchaseAccessTokens { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    DbSet<CustomerAddress> CustomerAddresses { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<ProductReview> ProductReviews { get; }
    DbSet<ProductReviewVote> ProductReviewVotes { get; }
    DbSet<SiteSetting> SiteSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<PageViewLog> PageViewLogs { get; }
    DbSet<OnlineUser> OnlineUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
