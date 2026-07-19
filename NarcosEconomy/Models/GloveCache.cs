using System.Text.Json;
using NarcosEconomy.Models;

namespace NarcosEconomy.Caches;

internal static class GloveCache
{
    private static readonly Dictionary<string, Glove> _gloves = new();

    internal static IEnumerable<Glove> All => _gloves.Values;

    internal static void Build(JsonElement glovesRoot)
    {

        Console.WriteLine("[GloveCache] Build() called");

        int count = 0;

        foreach (var gloveNode in glovesRoot.EnumerateObject())
        {
            count++;
            Console.WriteLine($"[GloveCache] Found glove: {gloveNode.Name}");

            // existing code...
        }

        
        _gloves.Clear();

        foreach (var gloveNode in glovesRoot.EnumerateObject())
        {
            var glove = gloveNode.Value;

            int defIndex = int.Parse(glove.GetProperty("index").GetString()!);

            string displayName = glove.GetProperty("tag").GetString()!;

            var paintKits = new List<PaintKit>();

            foreach (var paintNode in glove.GetProperty("paint_kits").EnumerateObject())
            {
                var paint = paintNode.Value;

                paintKits.Add(new PaintKit
                {
                    Id = int.Parse(paint.GetProperty("index").GetString()!),
                    Weapon = gloveNode.Name,
                    InternalName = paintNode.Name,
                    Name = paint.GetProperty("tag").GetString()!,
                    Rarity = paint.GetProperty("rarity").GetString()!,
                    MinFloat = 0.06f,
                    MaxFloat = 0.80f
                });
            }

            _gloves.Add(gloveNode.Name, new Glove
            {
                DefIndex = defIndex,
                InternalName = gloveNode.Name,
                Name = displayName,
                PaintKits = paintKits
            });
        }
        Console.WriteLine($"[GloveCache] Total gloves: {count}");
        Console.WriteLine($"[GloveCache] Cache size: {_gloves.Count}");
    }

    internal static IEnumerable<Glove> GetAll()
        => _gloves.Values;

    internal static Glove? Get(string internalName)
    {
        _gloves.TryGetValue(internalName, out var glove);
        return glove;
    }
}