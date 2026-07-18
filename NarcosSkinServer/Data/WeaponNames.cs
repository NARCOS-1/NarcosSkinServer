namespace NarcosSkinServer.Data;

public static class WeaponNames
{
    private static readonly Dictionary<int, string> Names = new()
    {
        // Knives
        { 500, "Bayonet" },
        { 503, "Classic Knife" },
        { 505, "Flip Knife" },
        { 506, "Gut Knife" },
        { 507, "Karambit" },
        { 508, "M9 Bayonet" },
        { 509, "Huntsman Knife" },
        { 512, "Falchion Knife" },
        { 514, "Bowie Knife" },
        { 515, "Butterfly Knife" },
        { 516, "Shadow Daggers" },
        { 517, "Paracord Knife" },
        { 518, "Survival Knife" },
        { 519, "Ursus Knife" },
        { 520, "Navaja Knife" },
        { 521, "Nomad Knife" },
        { 522, "Stiletto Knife" },
        { 523, "Talon Knife" },
        { 524, "Skeleton Knife" },
        { 525, "Kukri Knife" },

        // Gloves
        { 5027, "Bloodhound Gloves" },
        { 5028, "Default T Gloves" },
        { 5029, "Default CT Gloves" },
        { 5030, "Sport Gloves" },
        { 5031, "Driver Gloves" },
        { 5032, "Hand Wraps" },
        { 5033, "Moto Gloves" },
        { 5034, "Specialist Gloves" },
        { 5035, "Hydra Gloves" },
        { 4725, "Broken Fang Gloves" },

        // Pistols
        { 1, "Desert Eagle" },
        { 2, "Dual Berettas" },
        { 3, "Five-SeveN" },
        { 4, "Glock-18" },
        { 30, "Tec-9" },
        { 32, "P2000" },
        { 36, "P250" },
        { 61, "USP-S" },
        { 63, "CZ75-Auto" },
        { 64, "R8 Revolver" },

        // SMGs
        { 17, "MAC-10" },
        { 19, "P90" },
        { 23, "MP5-SD" },
        { 24, "UMP-45" },
        { 26, "PP-Bizon" },
        { 33, "MP7" },
        { 34, "MP9" },

        // Rifles
        { 7, "AK-47" },
        { 8, "AUG" },
        { 10, "FAMAS" },
        { 13, "Galil AR" },
        { 16, "M4A4" },
        { 39, "SG 553" },
        { 60, "M4A1-S" },

        // Snipers
        { 9, "AWP" },
        { 11, "G3SG1" },
        { 38, "SCAR-20" },
        { 40, "SSG 08" },

        // Heavy
        { 14, "M249" },
        { 27, "MAG-7" },
        { 28, "Negev" },
        { 29, "Sawed-Off" },
        { 25, "XM1014" },
        { 35, "Nova" },

        // Equipment
        { 31, "Zeus x27" }
    };

    public static string Get(int defIndex)
    {
        return Names.TryGetValue(defIndex, out var name)
            ? name
            : $"Item {defIndex}";
    }
}