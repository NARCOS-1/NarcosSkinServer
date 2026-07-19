using NarcosEconomy.Models;
using System.Text.Json;

namespace NarcosEconomy.Caches;

internal static class PaintKitCache
{
    private static readonly Dictionary<int, PaintKit> _paintKits = new();

    internal static void Build(JsonElement root)
    {
        _paintKits.Clear();

        JsonElement skins = root.GetProperty("skins");

        foreach (JsonProperty weapon in skins.EnumerateObject())
        {
            foreach (JsonProperty skin in weapon.Value.EnumerateObject())
            {
                int index = int.Parse(
                    skin.Value.GetProperty("index").GetString()!);

                var paintKit = new PaintKit
                {
                    Id = index,
                    Weapon = weapon.Name,
                    InternalName = skin.Name,
                    Name = skin.Value.GetProperty("tag").GetString()!,
                    Rarity = skin.Value.GetProperty("rarity").GetString()!,
                    MinFloat = skin.Value.GetProperty("lowest_float").GetSingle(),
                    MaxFloat = skin.Value.GetProperty("highest_float").GetSingle()
                };

                _paintKits[index] = paintKit;
            }
        }
    }

    internal static PaintKit Get(int paintKit)
    {
        if (!_paintKits.TryGetValue(paintKit, out var result))
            throw new KeyNotFoundException();

        return result;
    }

    internal static int Count => _paintKits.Count;

    
    internal static IReadOnlyCollection<PaintKit> GetAll()
    {
        return _paintKits.Values;
    }

    internal static IReadOnlyDictionary<int, PaintKit> PaintKits => _paintKits;
}