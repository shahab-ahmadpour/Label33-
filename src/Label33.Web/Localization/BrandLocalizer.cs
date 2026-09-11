using Label33.Web.Localization;
using Microsoft.AspNetCore.Localization;

namespace Label33.Web.Localization;

public interface IBrandLocalizer
{
    string Culture { get; }
    bool IsRtl { get; }
    string this[string key] { get; }
}

public sealed class BrandLocalizer : IBrandLocalizer
{
    private readonly IHttpContextAccessor _http;

    public BrandLocalizer(IHttpContextAccessor http) => _http = http;

    public string Culture
    {
        get
        {
            var feature = _http.HttpContext?.Features.Get<IRequestCultureFeature>();
            var name = feature?.RequestCulture.UICulture.TwoLetterISOLanguageName ?? "en";
            return name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "fa";
        }
    }

    public bool IsRtl => Culture == "fa";
    public string this[string key] => BrandText.T(Culture, key);
}
