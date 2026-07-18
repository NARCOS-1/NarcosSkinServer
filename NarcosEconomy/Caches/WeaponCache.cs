using System.Text.Json;

namespace NarcosEconomy.Caches;

internal static class WeaponCache
{
    private static readonly Dictionary<string, JsonElement> _weapons = new();

    internal static void Build(JsonElement root)
    {
        _weapons.Clear();

        JsonElement skins = root.GetProperty("skins");

        foreach (JsonProperty weapon in skins.EnumerateObject())
        {
            _weapons[weapon.Name] = weapon.Value;
        }
    }

    internal static JsonElement Get(string weaponName)
    {
        if (!_weapons.TryGetValue(weaponName, out JsonElement weapon))
            throw new KeyNotFoundException($"Weapon '{weaponName}' was not found.");

        return weapon;
    }

    internal static bool Contains(string weaponName)
    {
        return _weapons.ContainsKey(weaponName);
    }

    internal static int Count => _weapons.Count;
}