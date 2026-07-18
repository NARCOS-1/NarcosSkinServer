using System.Text.Json;

namespace NarcosEconomy.Services;

public sealed class LegacyModelResolver
{
    private readonly Dictionary<(int DefIndex, int PaintKit), bool> _legacyModels = new();

    public LegacyModelResolver(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var json = File.ReadAllText(filePath);

        var entries = JsonSerializer.Deserialize<List<LegacyModelEntry>>(json);

        if (entries == null)
            return;

        foreach (var entry in entries)
        {
            _legacyModels[(entry.DefIndex, entry.PaintKit)] = true;
        }
    }

    public bool IsLegacyModel(int defIndex, int paintKit)
    {
        return _legacyModels.ContainsKey((defIndex, paintKit));
    }

    private sealed class LegacyModelEntry
    {
        public int DefIndex { get; set; }
        public int PaintKit { get; set; }
    }
}