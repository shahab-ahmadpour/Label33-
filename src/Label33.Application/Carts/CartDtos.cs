namespace Label33.Application.Carts;

public sealed record CartLineDto(
    Guid VariantId,
    string ProductName,
    string ProductSlug,
    string SizeTitle,
    string? ImageUrl,
    int Quantity,
    decimal UnitPriceRials,
    decimal LineTotalRials);

public sealed record CartViewDto(
    Guid CartId,
    IReadOnlyList<CartLineDto> Lines,
    decimal SubtotalRials);
