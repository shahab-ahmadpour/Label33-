using Label33.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Label33.Infrastructure.Sms;

public sealed class DevelopmentSmsSender : ISmsSender
{
    private readonly string _root;
    private readonly ILogger<DevelopmentSmsSender> _logger;

    public DevelopmentSmsSender(IOptions<SmsOptions> options, ILogger<DevelopmentSmsSender> logger)
    {
        _ = options;
        _root = Path.Combine(AppContext.BaseDirectory, "App_Data", "sms");
        Directory.CreateDirectory(_root);
        _logger = logger;
    }

    public string ProviderName => "Development";

    public async Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var path = Path.Combine(_root, $"{stamp}__{phoneE164}.txt");
        var content = $"To: {phoneE164}\nProvider: Development\nUtc: {DateTime.UtcNow:O}\n\n{message}\n";
        await File.WriteAllTextAsync(path, content, cancellationToken);
        _logger.LogWarning("DEV SMS to {Phone}: {Message}", phoneE164, message);
    }
}

/// <summary>
/// Kavenegar HTTP sender. Active when Sms:Provider=Kavenegar and ApiKey is set.
/// </summary>
public sealed class KavenegarSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _options;
    private readonly ILogger<KavenegarSmsSender> _logger;

    public KavenegarSmsSender(HttpClient http, IOptions<SmsOptions> options, ILogger<KavenegarSmsSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Kavenegar";

    public async Task SendAsync(string phoneE164, string message, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Kavenegar.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Kavenegar ApiKey is not configured (Sms:Kavenegar:ApiKey).");

        // Kavenegar expects receptor like 0912... or 98912...
        var receptor = phoneE164.StartsWith("98", StringComparison.Ordinal) ? "0" + phoneE164[2..] : phoneE164;
        var sender = _options.Kavenegar.Sender?.Trim() ?? "";

        // Plain send API — swap to verify/lookup template later if Template is set.
        var url = $"https://api.kavenegar.com/v1/{apiKey}/sms/send.json";
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["receptor"] = receptor,
            ["sender"] = sender,
            ["message"] = message
        });

        using var response = await _http.PostAsync(url, form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Kavenegar HTTP {Status}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Kavenegar send failed with status {(int)response.StatusCode}.");
        }

        _logger.LogInformation("Kavenegar SMS accepted for {Phone}", receptor);
    }
}
