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
        },

        ["Galil AR"] = new()
        {
            Name = "Galil AR",
            DefIndex = 13,
            Category = WeaponCategory.Rifle
        },

        ["FAMAS"] = new()
        {
            Name = "FAMAS",
            DefIndex = 10,
            Category = WeaponCategory.Rifle
        },

        ["AK-47"] = new()
        {
            Name = "AK-47",
            DefIndex = 7,
            Category = WeaponCategory.Rifle
        },

        ["M4A4"] = new()
        {
            Name = "M4A4",
            DefIndex = 16,
            Category = WeaponCategory.Rifle
        },

        ["M4A1-S"] = new()
        {
            Name = "M4A1-S",
            DefIndex = 60,
            Category = WeaponCategory.Rifle
        },

        ["SG 553"] = new()
        {
            Name = "SG 553",
            DefIndex = 39,
            Category = WeaponCategory.Rifle
        },

        ["AUG"] = new()
        {
            Name = "AUG",
            DefIndex = 8,
            Category = WeaponCategory.Rifle
        },

        ["Desert Eagle"] = new()
        {
            Name = "Desert Eagle",
            DefIndex = 1,
            Category = WeaponCategory.Pistol
        },

        ["Dual Berettas"] = new()
        {
            Name = "Dual Berettas",
            DefIndex = 2,
            Category = WeaponCategory.Pistol
        },

        ["Five-SeveN"] = new()
        {
            Name = "Five-SeveN",
            DefIndex = 3,
            Category = WeaponCategory.Pistol
        },

        ["Glock-18"] = new()
        {
            Name = "Glock-18",
            DefIndex = 4,
            Category = WeaponCategory.Pistol
        },

        ["Tec-9"] = new()
        {
            Name = "Tec-9",
            DefIndex = 30,
            Category = WeaponCategory.Pistol
        },

        ["P2000"] = new()
        {
            Name = "P2000",
            DefIndex = 32,
            Category = WeaponCategory.Pistol
        },

        ["P250"] = new()
        {
            Name = "P250",
            DefIndex = 36,
            Category = WeaponCategory.Pistol
        },

        ["USP-S"] = new()
        {
            Name = "USP-S",
            DefIndex = 61,
            Category = WeaponCategory.Pistol
        },

        ["CZ75-Auto"] = new()
        {
            Name = "CZ75-Auto",
            DefIndex = 63,
            Category = WeaponCategory.Pistol
        },

        ["R8 Revolver"] = new()
        {
            Name = "R8 Revolver",
            DefIndex = 64,
            Category = WeaponCategory.Pistol
        },

        ["MAC-10"] = new()
        {
            Name = "MAC-10",
            DefIndex = 17,
            Category = WeaponCategory.SMG
        },

        ["P90"] = new()
        {
            Name = "P90",
            DefIndex = 19,
            Category = WeaponCategory.SMG
        },

        ["MP5-SD"] = new()
        {
            Name = "MP5-SD",
            DefIndex = 23,
            Category = WeaponCategory.SMG
        },

        ["UMP-45"] = new()
        {
            Name = "UMP-45",
            DefIndex = 24,
            Category = WeaponCategory.SMG
        },

        ["PP-Bizon"] = new()
        {
            Name = "PP-Bizon",
            DefIndex = 26,
            Category = WeaponCategory.SMG
        },

        ["MP7"] = new()
        {
            Name = "MP7",
            DefIndex = 33,
            Category = WeaponCategory.SMG
        },

        ["MP9"] = new()
        {
            Name = "MP9",
            DefIndex = 34,
            Category = WeaponCategory.SMG
        },

        ["AWP"] = new()
        {
            Name = "AWP",
            DefIndex = 9,
            Category = WeaponCategory.Sniper
        },

        ["G3SG1"] = new()
        {
            Name = "G3SG1",
            DefIndex = 11,
            Category = WeaponCategory.Sniper
        },

        ["SCAR-20"] = new()
        {
            Name = "SCAR-20",
            DefIndex = 38,
            Category = WeaponCategory.Sniper
        },

        ["SSG 08"] = new()
        {
            Name = "SSG 08",
            DefIndex = 40,
            Category = WeaponCategory.Sniper
        },

        ["M249"] = new()
        {
            Name = "M249",
            DefIndex = 14,
            Category = WeaponCategory.Heavy
        },

        ["MAG-7"] = new()
        {
            Name = "MAG-7",
            DefIndex = 27,
            Category = WeaponCategory.Heavy
        },

        ["Negev"] = new()
        {
            Name = "Negev",
            DefIndex = 28,
            Category = WeaponCategory.Heavy
        },

        ["Sawed-Off"] = new()
        {
            Name = "Sawed-Off",
            DefIndex = 29,
            Category = WeaponCategory.Heavy
        },

        ["XM1014"] = new()
        {
            Name = "XM1014",
            DefIndex = 25,
            Category = WeaponCategory.Heavy
        },

        ["Nova"] = new()
        {
            Name = "Nova",
            DefIndex = 35,
            Category = WeaponCategory.Heavy
        },

        ["Zeus x27"] = new()
        {
            Name = "Zeus x27",
            DefIndex = 31,
            Category = WeaponCategory.Equipment
        }
    };
}