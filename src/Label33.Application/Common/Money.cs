namespace Label33.Application.Common;

/// <summary>
/// Store money is persisted as IRR (Rial). Ops UI enters/displays Toman (Rial / 10).
/// </summary>
public static class Money
{
    public const string DisplayUnit = "Toman";

    public static decimal ToToman(decimal rials) => Math.Round(rials / 10m, MidpointRounding.AwayFromZero);

    public static decimal ToRials(decimal toman) => toman * 10m;

    public static string FormatToman(decimal rials)
        => ToToman(rials).ToString("N0");
}
