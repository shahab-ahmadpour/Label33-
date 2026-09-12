using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Label33.Application.Auth;

public static class IranianPhone
{
    private static readonly Regex NonDigit = new(@"\D+", RegexOptions.Compiled);

    /// <summary>Normalize to 989xxxxxxxxx. Returns null if invalid.</summary>
    public static string? NormalizeToE164(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = NonDigit.Replace(input.Trim(), string.Empty);
        if (digits.StartsWith("0098", StringComparison.Ordinal))
            digits = digits[4..];
        else if (digits.StartsWith("098", StringComparison.Ordinal) && digits.Length >= 13)
            digits = digits[3..];
        else if (digits.StartsWith("98", StringComparison.Ordinal) && digits.Length == 12)
        { }
        else if (digits.StartsWith('0') && digits.Length == 11)
            digits = "98" + digits[1..];
        else if (digits.Length == 10 && digits.StartsWith('9'))
            digits = "98" + digits;
        else
            return null;

        if (digits.Length != 12 || !digits.StartsWith("989", StringComparison.Ordinal))
            return null;

        return digits;
    }

    public static string ToLocalDisplay(string phoneE164)
        => phoneE164.StartsWith("98", StringComparison.Ordinal) && phoneE164.Length == 12
            ? "0" + phoneE164[2..]
            : phoneE164;

    public static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim()));
        return Convert.ToHexString(bytes);
    }
}
