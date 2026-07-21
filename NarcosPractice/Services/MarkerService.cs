using Newtonsoft.Json;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

public class MarkerService
{
    // Close enough to count as "the same stand-spot" when saving a new lineup, so
    // near-identical positions stack onto one marker instead of littering duplicates.
    private const float SameMarkerRadius = 48f;

    // Close enough to show the "Press E" prompt and allow interacting with a marker.
    public const float InteractRadius = 80f;

    private readonly string _dataDirectory;
    private readonly Dictionary<string, List<Marker>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public MarkerService(string pluginDirectory)
    {
        _dataDirectory = Path.Combine(pluginDirectory, "Data", "Markers");
        Directory.CreateDirectory(_dataDirectory);
    }

    private string PathFor(string map) => Path.Combine(_dataDirectory, $"{map}.json");

    public List<Marker> GetMarkers(string map)
    {
        if (_cache.TryGetValue(map, out var cached))
            return cached;

        string path = PathFor(map);
        List<Marker> markers = File.Exists(path)
            ? JsonConvert.DeserializeObject<List<Marker>>(File.ReadAllText(path)) ?? []
            : [];

        _cache[map] = markers;
        return markers;
    }

    public Marker GetOrCreateMarker(string map, float x, float y, float z)
    {
        var markers = GetMarkers(map);
        var existing = markers.FirstOrDefault(m => Distance(m, x, y, z) <= SameMarkerRadius);
        if (existing != null)
            return existing;

        var marker = new Marker { PosX = x, PosY = y, PosZ = z };
        markers.Add(marker);
        Persist(map, markers);
        return marker;
    }

    public Marker? FindNearest(string map, float x, float y, float z, float maxDistance)
    {
        Marker? nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var marker in GetMarkers(map))
        {
            float d = Distance(marker, x, y, z);
            if (d <= maxDistance && d < nearestDist)
            {
                nearest = marker;
                nearestDist = d;
            }
        }

        return nearest;
    }

    public void AddLineup(string map, Marker marker, Lineup lineup)
    {
        marker.Lineups.Add(lineup);
        Persist(map, GetMarkers(map));
    }

    public bool DeleteLineup(string map, Marker marker, string name, NadeType type)
    {
        int removed = marker.Lineups.RemoveAll(l =>
            l.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && l.Type == type);

        if (removed == 0)
            return false;

        var markers = GetMarkers(map);
        if (marker.Lineups.Count == 0)
            markers.Remove(marker);

        Persist(map, markers);
        return true;
    }

    private static float Distance(Marker marker, float x, float y, float z)
    {
        float dx = marker.PosX - x;
        float dy = marker.PosY - y;
        float dz = marker.PosZ - z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private void Persist(string map, List<Marker> markers)
    {
        File.WriteAllText(PathFor(map), JsonConvert.SerializeObject(markers, Formatting.Indented));
    }
}
