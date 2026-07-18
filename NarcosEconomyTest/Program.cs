using NarcosEconomy;

Economy.Initialize(AppContext.BaseDirectory);

Console.WriteLine(Economy.WeaponCount);

var ak = Economy.GetWeapon("weapon_ak47");

Console.WriteLine(ak.EnumerateObject().Count());