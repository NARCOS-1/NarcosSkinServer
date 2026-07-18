using System.Text.Json;
using NarcosSkinServer.Models;

namespace NarcosSkinServer.Data;

public static class PaintKitLoader
{
    public static List<PaintKit> PaintKits { get; private set; } = [];

    private static Dictionary<int, PaintKit> _lookup = new();

    public static void Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        string json = File.ReadAllText(path);

        PaintKits = JsonSerializer.Deserialize<List<PaintKit>>(json) ?? [];

        _lookup = PaintKits.ToDictionary(x => x.Id);
    }

    public static PaintKit? Get(int id)
    {
        return _lookup.TryGetValue(id, out var paintKit)
            ? paintKit
            : null;
    }
    public static IEnumerable<PaintKit> GetForWeapon(int defIndex)
    {
        return PaintKits.Where(x => x.Weapons.Contains(defIndex));
    }

}