using System.Collections.Concurrent;
using System.Text.Json;

namespace Label33.Web;

public static class IranGeo
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, HashSet<string>>> Cache = new();

    public static bool IsValidProvinceCity(string webRootPath, string province, string city)
    {
        var map = GetMap(webRootPath);
        return map.TryGetValue(province, out var cities) && cities.Contains(city);
    }

    private static IReadOnlyDictionary<string, HashSet<string>> GetMap(string webRootPath)
    {
        var key = webRootPath ?? "";
        return Cache.GetOrAdd(key, Load);
    }

    private static IReadOnlyDictionary<string, HashSet<string>> Load(string webRootPath)
    {
        var path = Path.Combine(webRootPath, "data", "iran-geo.json");
        if (!File.Exists(path))
            return new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        using var stream = File.OpenRead(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(stream)
                  ?? new Dictionary<string, List<string>>();

        return raw.ToDictionary(
            kv => kv.Key,
            kv => new HashSet<string>(kv.Value ?? Enumerable.Empty<string>(), StringComparer.Ordinal),
            StringComparer.Ordinal);
    }
}
