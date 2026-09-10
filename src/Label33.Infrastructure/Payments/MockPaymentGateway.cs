using Label33.Application.Abstractions;

namespace Label33.Infrastructure.Payments;

/// <summary>
/// Development payment gateway. Start returns a fake provider ref;
/// Verify succeeds unless providerRef starts with "FAIL-".
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public string ProviderName => "Mock";

    public Task<PaymentStartResult> StartAsync(PaymentStartRequest request, CancellationToken cancellationToken = default)
    {
        var providerRef = $"MOCK-{request.OrderId:N}";
        var redirect = $"{request.CallbackUrl}?providerRef={providerRef}";
        return Task.FromResult(new PaymentStartResult(true, providerRef, redirect, null));
    }

    public Task<PaymentVerifyResult> VerifyAsync(PaymentVerifyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProviderRef.StartsWith("FAIL-", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(new PaymentVerifyResult(false, request.ProviderRef, "Mock payment failed."));

        return Task.FromResult(new PaymentVerifyResult(true, request.ProviderRef, null));
    }
}
