using NarcosSkinServer.Models;

namespace NarcosSkinServer.Data;

public static class SkinCatalog
{
    public static readonly Dictionary<string, WeaponDefinition> Weapons = new()
    {
        ["Bayonet"] = new()
        {
            Name = "Bayonet",
            DefIndex = 500,
            Category = WeaponCategory.Knife
        },

        ["Classic Knife"] = new()
        {
            Name = "Classic Knife",
            DefIndex = 503,
            Category = WeaponCategory.Knife
        },

        ["Flip Knife"] = new()
        {
            Name = "Flip Knife",
            DefIndex = 505,
            Category = WeaponCategory.Knife
        },

        ["Gut Knife"] = new()
        {
            Name = "Gut Knife",
            DefIndex = 506,
            Category = WeaponCategory.Knife
        },

        ["Karambit"] = new()
        {
            Name = "Karambit",
            DefIndex = 507,
            Category = WeaponCategory.Knife
        },

        ["M9 Bayonet"] = new()
        {
            Name = "M9 Bayonet",
            DefIndex = 508,
            Category = WeaponCategory.Knife
        },

        ["Huntsman Knife"] = new()
        {
            Name = "Huntsman Knife",
            DefIndex = 509,
            Category = WeaponCategory.Knife
        },

        ["Falchion Knife"] = new()
        {
            Name = "Falchion Knife",
            DefIndex = 512,
            Category = WeaponCategory.Knife
        },

        ["Bowie Knife"] = new()
        {
            Name = "Bowie Knife",
            DefIndex = 514,
            Category = WeaponCategory.Knife
        },

        ["Butterfly Knife"] = new()
        {
            Name = "Butterfly Knife",
            DefIndex = 515,
            Category = WeaponCategory.Knife
        },

        ["Shadow Daggers"] = new()
        {
            Name = "Shadow Daggers",
            DefIndex = 516,
            Category = WeaponCategory.Knife
        },

        ["Paracord Knife"] = new()
        {
            Name = "Paracord Knife",
            DefIndex = 517,
            Category = WeaponCategory.Knife
        },

        ["Survival Knife"] = new()
        {
            Name = "Survival Knife",
            DefIndex = 518,
            Category = WeaponCategory.Knife
        },

        ["Ursus Knife"] = new()
        {
            Name = "Ursus Knife",
            DefIndex = 519,
            Category = WeaponCategory.Knife
        },

        ["Navaja Knife"] = new()
        {
            Name = "Navaja Knife",
            DefIndex = 520,
            Category = WeaponCategory.Knife
        },

        ["Nomad Knife"] = new()
        {
            Name = "Nomad Knife",
            DefIndex = 521,
            Category = WeaponCategory.Knife
        },

        ["Stiletto Knife"] = new()
        {
            Name = "Stiletto Knife",
            DefIndex = 522,
            Category = WeaponCategory.Knife
        },

        ["Talon Knife"] = new()
        {
            Name = "Talon Knife",
            DefIndex = 523,
            Category = WeaponCategory.Knife
        },

        ["Skeleton Knife"] = new()
        {
            Name = "Skeleton Knife",
            DefIndex = 525,
            Category = WeaponCategory.Knife
        },

        ["Kukri Knife"] = new()
        {
            Name = "Kukri Knife",
            DefIndex = 526,
            Category = WeaponCategory.Knife
        }
    };
}