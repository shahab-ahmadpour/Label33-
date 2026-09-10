using Label33.Application.Abstractions;
using Label33.Application.Common;
using Label33.Domain.Entities;
using Label33.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Label33.Application.Reviews;

public class ReviewService
{
    private readonly IAppDbContext _db;

    public ReviewService(IAppDbContext db) => _db = db;

    public async Task<ProductReview> SubmitAsync(Guid userId, Guid productId, int rating, string? title, string? body, CancellationToken ct = default)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5.");

        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new DomainException("Product not found.");

        var review = new ProductReview
        {
            UserId = userId,
            ProductId = productId,
            Rating = rating,
            Title = title,
            Body = body,
            Status = ReviewStatus.Pending
        };
        _db.ProductReviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return review;
    }

    public async Task ModerateAsync(Guid reviewId, ReviewStatus status, CancellationToken ct = default)
    {
        if (status is not (ReviewStatus.Approved or ReviewStatus.Rejected))
            throw new DomainException("Invalid moderation status.");

        var review = await _db.ProductReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new DomainException("Review not found.");
        review.Status = status;
        await _db.SaveChangesAsync(ct);
    }
}
