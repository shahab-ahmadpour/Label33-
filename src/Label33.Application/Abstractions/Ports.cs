namespace Label33.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IOrderNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record PaymentStartRequest(Guid OrderId, decimal Amount, string Currency, string Description, string CallbackUrl);
public sealed record PaymentStartResult(bool Succeeded, string? ProviderRef, string? RedirectUrl, string? Error);
public sealed record PaymentVerifyRequest(string ProviderRef, decimal Amount, string? RawPayload);
public sealed record PaymentVerifyResult(bool Succeeded, string? ProviderRef, string? Error);

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<PaymentStartResult> StartAsync(PaymentStartRequest request, CancellationToken cancellationToken = default);
    Task<PaymentVerifyResult> VerifyAsync(PaymentVerifyRequest request, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
