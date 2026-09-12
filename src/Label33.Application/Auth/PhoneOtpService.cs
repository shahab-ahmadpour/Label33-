using System.Security.Cryptography;
using System.Text;
using Label33.Application.Abstractions;
using Label33.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Label33.Application.Auth;

public sealed record OtpSendResult(bool Succeeded, string? Error, string? DevCode, int ResendAfterSeconds);
public sealed record OtpVerifyResult(bool Succeeded, string? Error, string? PhoneE164);

public class PhoneOtpService
{
    private readonly IAppDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IClock _clock;
    private readonly SmsOptions _options;
    private readonly ILogger<PhoneOtpService> _logger;

    public PhoneOtpService(
        IAppDbContext db,
        ISmsSender sms,
        IClock clock,
        IOptions<SmsOptions> options,
        ILogger<PhoneOtpService> logger)
    {
        _db = db;
        _sms = sms;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OtpSendResult> SendLoginCodeAsync(string? rawPhone, string? requestIp, CancellationToken ct = default)
    {
        var phone = IranianPhone.NormalizeToE164(rawPhone);
        if (phone is null)
            return new(false, "invalid_phone", null, 0);

        var now = _clock.UtcNow;
        var open = await _db.SmsOtpChallenges
            .Where(x => x.PhoneE164 == phone && x.Purpose == "login" && x.ConsumedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        var latest = open.FirstOrDefault();
        if (latest is not null)
        {
            var elapsed = (now - latest.CreatedAtUtc).TotalSeconds;
            var wait = _options.ResendCooldownSeconds - (int)elapsed;
            if (wait > 0)
                return new(false, "cooldown", null, wait);
        }

        foreach (var prior in open)
            prior.ConsumedAtUtc = now;

        var code = GenerateNumericCode(_options.CodeLength <= 0 ? 5 : _options.CodeLength);
        var challenge = new SmsOtpChallenge
        {
            PhoneE164 = phone,
            CodeHash = IranianPhone.HashCode(code),
            ExpiresAtUtc = now.AddMinutes(_options.ExpiryMinutes <= 0 ? 5 : _options.ExpiryMinutes),
            MaxAttempts = _options.MaxAttempts <= 0 ? 5 : _options.MaxAttempts,
            RequestIp = Truncate(requestIp, 64),
            Purpose = "login"
        };

        _db.SmsOtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);

        var message = $"کد ورود 33: {code}\nاعتبار {(_options.ExpiryMinutes <= 0 ? 5 : _options.ExpiryMinutes)} دقیقه";
        try
        {
            await _sms.SendAsync(phone, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed for {Phone} via {Provider}", phone, _sms.ProviderName);
            return new(false, "sms_failed", null, 0);
        }

        _logger.LogInformation("OTP issued for {Phone} via {Provider}", IranianPhone.ToLocalDisplay(phone), _sms.ProviderName);

        var expose = _options.ExposeCodeInDevelopment
            && string.Equals(_sms.ProviderName, "Development", StringComparison.OrdinalIgnoreCase);
        return new(true, null, expose ? code : null, _options.ResendCooldownSeconds);
    }

    public async Task<OtpVerifyResult> VerifyLoginCodeAsync(string? rawPhone, string? code, CancellationToken ct = default)
    {
        var phone = IranianPhone.NormalizeToE164(rawPhone);
        if (phone is null)
            return new(false, "invalid_phone", null);
        if (string.IsNullOrWhiteSpace(code))
            return new(false, "invalid_code", null);

        var now = _clock.UtcNow;
        var challenge = await _db.SmsOtpChallenges
            .Where(x => x.PhoneE164 == phone && x.Purpose == "login" && x.ConsumedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (challenge is null)
            return new(false, "no_challenge", null);
        if (challenge.ExpiresAtUtc < now)
            return new(false, "expired", null);
        if (challenge.AttemptCount >= challenge.MaxAttempts)
            return new(false, "too_many_attempts", null);

        challenge.AttemptCount += 1;
        var hash = IranianPhone.HashCode(code);
        if (!FixedEquals(hash, challenge.CodeHash))
        {
            await _db.SaveChangesAsync(ct);
            return new(false, "invalid_code", null);
        }

        challenge.ConsumedAtUtc = now;
        await _db.SaveChangesAsync(ct);
        return new(true, null, phone);
    }

    private static string GenerateNumericCode(int length)
    {
        length = Math.Clamp(length, 4, 8);
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + (bytes[i] % 10));
        return new string(chars);
    }

    private static bool FixedEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
