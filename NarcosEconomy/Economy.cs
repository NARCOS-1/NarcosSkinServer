using NarcosEconomy.Caches;
using NarcosEconomy.Models;
using System.Reflection;
using System.Text.Json;

namespace NarcosEconomy;

public static class Economy
{
    private static JsonDocument? _document;

    public static bool IsLoaded => _document != null;



    public static void Initialize(string pluginDirectory)
    {
        if (IsLoaded)
            return;

        string jsonPath = Path.Combine(pluginDirectory, "Assets", "cs2.json");
        

        Console.WriteLine($"[NarcosEconomy] Plugin Directory: {pluginDirectory}");
        Console.WriteLine($"[NarcosEconomy] Loading economy from: {jsonPath}");


        if (!File.Exists(jsonPath))
            throw new FileNotFoundException(
                $"Could not locate economy database: {jsonPath}");

        using FileStream stream = File.OpenRead(jsonPath);

        _document = JsonDocument.Parse(stream);

        PaintKitCache.Build(_document.RootElement);
        WeaponCache.Build(_document.RootElement);
        GloveCache.Build(_document.RootElement.GetProperty("gloves"));
    }

    internal static JsonElement Root
    {
        get
        {
            if (_document == null)
                throw new InvalidOperationException(
                    "Economy has not been initialized.");

            return _document.RootElement;
        }
    }

    public static JsonElement GetSection(string sectionName)
    {
        if (!Root.TryGetProperty(sectionName, out JsonElement section))
            throw new KeyNotFoundException($"Section '{sectionName}' was not found.");

        return section;
    }

    public static bool HasSection(string sectionName)
    {
        return Root.TryGetProperty(sectionName, out _);
    }

    public static Weapon GetWeapon(string weaponName)
    {
        return WeaponCache.Get(weaponName);
    }

    public static IEnumerable<Weapon> GetWeapons()
    {
        return WeaponCache.All;
    }

    public static PaintKit GetPaintKit(int paintKit)
    {
        return PaintKitCache.Get(paintKit);
    }

    public static IEnumerable<Glove> GetGloves()
    {
        return GloveCache.GetAll();
    }

    public static Glove? GetGlove(string internalName)
    {
        return GloveCache.Get(internalName);
    }

    public static Weapon? GetWeapon(int defIndex)
    {
        return WeaponCache.GetByDefIndex(defIndex);
    }
}