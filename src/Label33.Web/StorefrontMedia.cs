namespace Label33.Web;

public static class StorefrontMedia
{
    public const string FallbackImage = "~/brand/diyar/diyar-main.png";

    public static string Resolve(string? pathOrUrl)
        => string.IsNullOrWhiteSpace(pathOrUrl) ? FallbackImage : pathOrUrl!;
}
