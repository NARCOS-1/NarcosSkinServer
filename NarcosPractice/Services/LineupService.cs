using Newtonsoft.Json;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

public class LineupService
{
    private readonly string _dataDirectory;
    private readonly Dictionary<string, List<Lineup>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public LineupService(string pluginDirectory)
    {
        _dataDirectory = Path.Combine(pluginDirectory, "Data", "Lineups");
        Directory.CreateDirectory(_dataDirectory);
    }

    private string PathFor(string map) => Path.Combine(_dataDirectory, $"{map}.json");

    public List<Lineup> GetLineups(string map)
    {
        if (_cache.TryGetValue(map, out var cached))
            return cached;

        string path = PathFor(map);
        List<Lineup> lineups = File.Exists(path)
            ? JsonConvert.DeserializeObject<List<Lineup>>(File.ReadAllText(path)) ?? []
            : [];

        _cache[map] = lineups;
        return lineups;
    }

    public void SaveLineup(string map, Lineup lineup)
    {
        var lineups = GetLineups(map);
        lineups.RemoveAll(l => l.Name.Equals(lineup.Name, StringComparison.OrdinalIgnoreCase) && l.Type == lineup.Type);
        lineups.Add(lineup);
        Persist(map, lineups);
    }

    public bool DeleteLineup(string map, string name, NadeType type)
    {
        var lineups = GetLineups(map);
        int removed = lineups.RemoveAll(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && l.Type == type);
        if (removed > 0)
            Persist(map, lineups);
        return removed > 0;
    }

    private void Persist(string map, List<Lineup> lineups)
    {
        File.WriteAllText(PathFor(map), JsonConvert.SerializeObject(lineups, Formatting.Indented));
    }
}
