namespace Label33.Application.Abstractions;

public interface ISmsSender
{
    string ProviderName { get; }
    Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default);
}

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Development | Kavenegar</summary>
    public string Provider { get; set; } = "Development";
    public int CodeLength { get; set; } = 5;
    public int ExpiryMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    /// <summary>When provider is Development, show OTP on the login page for local testing.</summary>
    public bool ExposeCodeInDevelopment { get; set; } = true;
    public KavenegarOptions Kavenegar { get; set; } = new();
}

public sealed class KavenegarOptions
{
    public string ApiKey { get; set; } = "";
    public string Sender { get; set; } = "";
    public string? Template { get; set; }
}
