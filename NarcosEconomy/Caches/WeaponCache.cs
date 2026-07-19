using NarcosEconomy.Models;
using System.Text.Json;

namespace NarcosEconomy.Caches;

internal static class WeaponCache
{
    private static readonly Dictionary<string, Weapon> _weapons = new();

    /// <summary>
    /// Builds the weapon cache from the "skins" section of the economy JSON.
    /// </summary>
    /// <param name="root">The root JSON element of the economy file.</param>
    /// <param name="weaponDefIndexes">
    /// A lookup of internal weapon name (e.g. "weapon_ak47") to its definition index.
    /// The economy JSON has no "weapons" section of its own, so this must be supplied
    /// by the caller (see <see cref="Economy.Initialize"/>).
    /// </param>
    internal static void Build(JsonElement root, IReadOnlyDictionary<string, int> weaponDefIndexes)
    {
        _weapons.Clear();

        JsonElement skins = root.GetProperty("skins");

        foreach (JsonProperty weapon in skins.EnumerateObject())
        {
            List<PaintKit> paintKits = new();

            foreach (JsonProperty skin in weapon.Value.EnumerateObject())
            {
                int id = int.Parse(
                    skin.Value.GetProperty("index").GetString()!);

                paintKits.Add(PaintKitCache.Get(id));
            }

            if (!weaponDefIndexes.TryGetValue(weapon.Name, out int defIndex))
                continue;

            _weapons.Add(
                weapon.Name,
                new Weapon
                {
                    DefIndex = defIndex,
                    InternalName = weapon.Name,
                    Name = weapon.Name
                        .Replace("weapon_", "")
                        .Replace("_", " "),
                    PaintKits = paintKits
                });
        }
    }

    internal static Weapon Get(string weaponName)
    {
        if (!_weapons.TryGetValue(weaponName, out Weapon? weapon))
            throw new KeyNotFoundException($"Weapon '{weaponName}' was not found.");

        return weapon;
    }
    internal static bool TryGet(string weaponName, out Weapon weapon)
    {
        return _weapons.TryGetValue(weaponName, out weapon!);
    }

    internal static bool Contains(string weaponName)
    {
        return _weapons.ContainsKey(weaponName);
    }

    internal static IEnumerable<Weapon> All => _weapons.Values;

    internal static int Count => _weapons.Count;

    internal static Weapon? GetByDefIndex(int defIndex)
    {
        return _weapons.Values.FirstOrDefault(w => w.DefIndex == defIndex);
    }

}