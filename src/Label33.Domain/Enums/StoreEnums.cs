namespace Label33.Domain.Enums;

public enum ProductType
{
    Physical = 0,
    Digital = 1,
    Bundle = 2
}

public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum StockMode
{
    Tracked = 0,
    Unlimited = 1
}

public enum CartStatus
{
    Open = 0,
    Converted = 1,
    Abandoned = 2
}

public enum OrderStatus
{
    Draft = 0,
    AwaitingPayment = 1,
    Paid = 2,
    Fulfilling = 3,
    PartiallyFulfilled = 4,
    Completed = 5,
    Cancelled = 6,
    Refunded = 7
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refunded = 3
}

public enum ShipmentStatus
{
    Preparing = 0,
    Shipped = 1,
    Delivered = 2,
    Returned = 3
}

public enum ReservationStatus
{
    Active = 0,
    Consumed = 1,
    Released = 2,
    Expired = 3
}

public enum DiscountType
{
    Percent = 0,
    FixedAmount = 1
}

public enum ReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum SalesWindowScope
{
    Global = 0,
    Collection = 1,
    Variant = 2
}
