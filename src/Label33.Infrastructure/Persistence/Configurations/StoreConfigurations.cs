using Label33.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Label33.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasOne(x => x.ParentCategory).WithMany(x => x.Children).HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(320).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.Status, x.PublishedAtUtc });
        b.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.BasePrice).HasPrecision(18, 0);
        b.Property(x => x.CompareAtPrice).HasPrecision(18, 0);
        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.IsActive });
        b.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.DigitalAsset).WithMany().HasForeignKey(x => x.DigitalAssetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.Property(x => x.PathOrUrl).HasMaxLength(1000).IsRequired();
        // Product cascade + Variant SetNull creates multiple cascade paths on SQL Server.
        b.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Variant).WithMany(x => x.Images).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> b)
    {
        b.HasKey(x => new { x.ProductId, x.TagId });
        b.HasOne(x => x.Product).WithMany(x => x.ProductTags).HasForeignKey(x => x.ProductId);
        b.HasOne(x => x.Tag).WithMany(x => x.ProductTags).HasForeignKey(x => x.TagId);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.HasKey(x => new { x.ProductId, x.CategoryId });
        b.HasOne(x => x.Product).WithMany(x => x.ProductCategories).HasForeignKey(x => x.ProductId);
        b.HasOne(x => x.Category).WithMany(x => x.ProductCategories).HasForeignKey(x => x.CategoryId);
    }
}

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> b)
    {
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> b)
    {
        b.HasIndex(x => new { x.CollectionId, x.ProductId }).IsUnique();
        b.HasOne(x => x.Collection).WithMany(x => x.Items).HasForeignKey(x => x.CollectionId);
        b.HasOne(x => x.Product).WithMany(x => x.CollectionItems).HasForeignKey(x => x.ProductId);
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.HasKey(x => x.ProductVariantId);
        b.HasOne(x => x.ProductVariant).WithOne(x => x.InventoryItem)
            .HasForeignKey<InventoryItem>(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Ignore(x => x.Available);
    }
}

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> b)
    {
        b.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId);
        b.HasOne(x => x.Cart).WithMany().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.AnonymousToken).HasMaxLength(64);
        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => x.AnonymousToken);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 0);
        b.HasIndex(x => new { x.CartId, x.ProductVariantId }).IsUnique();
        b.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Value).HasPrecision(18, 0);
        b.Property(x => x.MinOrderAmount).HasPrecision(18, 0);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.Subtotal).HasPrecision(18, 0);
        b.Property(x => x.DiscountTotal).HasPrecision(18, 0);
        b.Property(x => x.ShippingTotal).HasPrecision(18, 0);
        b.Property(x => x.TaxTotal).HasPrecision(18, 0);
        b.Property(x => x.GrandTotal).HasPrecision(18, 0);
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ShippingAddress).WithOne(x => x.Order).HasForeignKey<OrderAddress>(x => x.OrderId);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
        b.Property(x => x.SkuSnapshot).HasMaxLength(100).IsRequired();
        b.Property(x => x.VariantTitleSnapshot).HasMaxLength(300).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 0);
        b.Property(x => x.LineTotal).HasPrecision(18, 0);
        b.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId);
        b.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> b)
    {
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40).IsRequired();
        b.Property(x => x.Province).HasMaxLength(100).IsRequired();
        b.Property(x => x.City).HasMaxLength(100).IsRequired();
        b.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.Line1).HasMaxLength(300).IsRequired();
        b.Property(x => x.Line2).HasMaxLength(300);
    }
}

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> b)
    {
        b.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        b.Property(x => x.ProviderRef).HasMaxLength(200);
        b.Property(x => x.Amount).HasPrecision(18, 0);
        b.HasIndex(x => new { x.Provider, x.ProviderRef });
        b.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId);
    }
}

public class DigitalEntitlementConfiguration : IEntityTypeConfiguration<DigitalEntitlement>
{
    public void Configure(EntityTypeBuilder<DigitalEntitlement> b)
    {
        b.HasIndex(x => x.OrderItemId).IsUnique();
        b.HasOne(x => x.OrderItem).WithOne(x => x.DigitalEntitlement).HasForeignKey<DigitalEntitlement>(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.DigitalAsset).WithMany().HasForeignKey(x => x.DigitalAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseAccessTokenConfiguration : IEntityTypeConfiguration<PurchaseAccessToken>
{
    public void Configure(EntityTypeBuilder<PurchaseAccessToken> b)
    {
        b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.Entitlement).WithMany(x => x.AccessTokens).HasForeignKey(x => x.EntitlementId);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> b)
    {
        b.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
    }
}

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> b)
    {
        b.Property(x => x.Title).HasMaxLength(200);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
    }
}

public class ProductReviewVoteConfiguration : IEntityTypeConfiguration<ProductReviewVote>
{
    public void Configure(EntityTypeBuilder<ProductReviewVote> b)
    {
        b.HasIndex(x => new { x.ReviewId, x.UserId }).IsUnique();
        b.HasOne(x => x.Review).WithMany(x => x.Votes).HasForeignKey(x => x.ReviewId);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class OnlineUserConfiguration : IEntityTypeConfiguration<OnlineUser>
{
    public void Configure(EntityTypeBuilder<OnlineUser> b)
    {
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserName).HasMaxLength(256).IsRequired();
    }
}

public class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> b)
    {
        b.HasIndex(x => new { x.ProductVariantId, x.ProductAttributeValueId }).IsUnique();
        // Both default Cascade would diamond from Product → Variants and Product → Attributes (SQL Server rejects).
        b.HasOne(x => x.ProductVariant).WithMany(x => x.AttributeValues).HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ProductAttributeValue).WithMany(x => x.VariantLinks).HasForeignKey(x => x.ProductAttributeValueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> b)
    {
        // Order.CouponId uses SetNull; dual Cascade here creates multiple paths on SQL Server.
        b.HasOne(x => x.Coupon).WithMany(x => x.Redemptions).HasForeignKey(x => x.CouponId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CouponId, x.OrderId }).IsUnique();
    }
}
