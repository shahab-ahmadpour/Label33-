using System.Collections.Concurrent;
using System.Text.Json;

namespace Label33.Web;

public static class IranGeo
{
    private static readonly ConcurrentDictionary<string, GeoMap> Cache = new();

    public static bool IsValidProvinceCity(string webRootPath, string province, string city)
    {
        var map = GetMap(webRootPath);
        // Accept either Persian or English labels (UI language may differ).
        if (map.ByFa.TryGetValue(province, out var citiesFa) && citiesFa.Contains(city))
            return true;
        if (map.ByEn.TryGetValue(province, out var citiesEn) && citiesEn.Contains(city))
            return true;

        // Province in one language, city in the other — resolve via fa/en bridges.
        if (map.ProvinceEnToFa.TryGetValue(province, out var provinceFa) &&
            map.ByFa.TryGetValue(provinceFa, out var cities) &&
            (cities.Contains(city) ||
             (map.CityEnToFa.TryGetValue(city, out var cityFa) && cities.Contains(cityFa))))
            return true;

        if (map.ProvinceFaToEn.TryGetValue(province, out var provinceEn) &&
            map.ByEn.TryGetValue(provinceEn, out var citiesEn2) &&
            (citiesEn2.Contains(city) ||
             (map.CityFaToEn.TryGetValue(city, out var cityEn) && citiesEn2.Contains(cityEn))))
            return true;

        return false;
    }

    /// <summary>Normalize submitted province/city to Persian canonical names when possible.</summary>
    public static (string Province, string City) ToPersian(string webRootPath, string province, string city)
    {
        var map = GetMap(webRootPath);
        var provinceFa = map.ProvinceEnToFa.TryGetValue(province, out var pf) ? pf : province;
        var cityFa = map.CityEnToFa.TryGetValue(city, out var cf) ? cf : city;
        return (provinceFa, cityFa);
    }

    private static GeoMap GetMap(string webRootPath)
    {
        var key = webRootPath ?? "";
        return Cache.GetOrAdd(key, Load);
    }

    private static GeoMap Load(string webRootPath)
    {
        var path = Path.Combine(webRootPath, "data", "iran-geo.json");
        if (!File.Exists(path))
            return GeoMap.Empty;

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("provinces", out var provincesEl))
            return GeoMap.Empty;

        var byFa = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var byEn = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var provinceEnToFa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var provinceFaToEn = new Dictionary<string, string>(StringComparer.Ordinal);
        var cityEnToFa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cityFaToEn = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var p in provincesEl.EnumerateArray())
        {
            var fa = p.GetProperty("fa").GetString() ?? "";
            var en = p.GetProperty("en").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(fa) || string.IsNullOrWhiteSpace(en))
                continue;

            provinceEnToFa[en] = fa;
            provinceFaToEn[fa] = en;

            var citiesFa = new HashSet<string>(StringComparer.Ordinal);
            var citiesEn = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (p.TryGetProperty("cities", out var citiesEl))
            {
                foreach (var c in citiesEl.EnumerateArray())
                {
                    var cfa = c.GetProperty("fa").GetString() ?? "";
                    var cen = c.GetProperty("en").GetString() ?? "";
                    if (string.IsNullOrWhiteSpace(cfa))
                        continue;
                    citiesFa.Add(cfa);
                    if (!string.IsNullOrWhiteSpace(cen))
                    {
                        citiesEn.Add(cen);
                        cityEnToFa[cen] = cfa;
                        cityFaToEn[cfa] = cen;
                    }
                }
            }

            byFa[fa] = citiesFa;
            byEn[en] = citiesEn;
        }

        return new GeoMap(byFa, byEn, provinceEnToFa, provinceFaToEn, cityEnToFa, cityFaToEn);
    }

    private sealed record GeoMap(
        IReadOnlyDictionary<string, HashSet<string>> ByFa,
        IReadOnlyDictionary<string, HashSet<string>> ByEn,
        IReadOnlyDictionary<string, string> ProvinceEnToFa,
        IReadOnlyDictionary<string, string> ProvinceFaToEn,
        IReadOnlyDictionary<string, string> CityEnToFa,
        IReadOnlyDictionary<string, string> CityFaToEn)
    {
        public static GeoMap Empty { get; } = new(
            new Dictionary<string, HashSet<string>>(),
            new Dictionary<string, HashSet<string>>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>());
    }
}
