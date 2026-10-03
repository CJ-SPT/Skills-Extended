using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Config;

public sealed class LevelingSpeedData
{
    public float GlobalMultiplier { get; set; } = 1;
    public Dictionary<string, float> SkillMultipliers { get; set; } = new();
    public float WeaponMasteryMultiplier { get; set; } = 1;

    public float Individual(string key) =>
        SkillMultipliers != null && SkillMultipliers.TryGetValue(key, out var value) ? value : 1;

    public float ForSkill(int id) =>
        LevelingSkillCatalog.ById.TryGetValue(id, out var skill)
            ? GlobalMultiplier * Individual(skill.Key)
            : 1;

    public static bool IsValid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 100;

    public void Validate()
    {
        if (!IsValid(GlobalMultiplier) || !IsValid(WeaponMasteryMultiplier)
            || SkillMultipliers == null
            || SkillMultipliers.Any(pair => !LevelingSkillCatalog.ByKey.ContainsKey(pair.Key) || !IsValid(pair.Value)))
            throw new ArgumentException("Leveling speed: use known player skill IDs and finite multipliers from 0 to 100.");
    }
}

public sealed class LevelingSkill
{
    public int Id { get; }
    public string Key { get; }
    public string Name { get; }
    public LevelingSkill(int id, string key, string name) { Id = id; Key = key; Name = name; }
}

// Canonical EFT IDs, shared by client and server; excludes BotReload and BotSound.
public static class LevelingSkillCatalog
{
    public static readonly IReadOnlyList<LevelingSkill> All = new LevelingSkill[]
    {
        new(0, "Endurance", "Endurance"), new(1, "Strength", "Strength"),
        new(2, "Vitality", "Vitality"), new(3, "Health", "Health"),
        new(4, "StressResistance", "Stress Resistance"), new(5, "Metabolism", "Metabolism"),
        new(6, "Immunity", "Immunity"), new(7, "Perception", "Perception"),
        new(8, "Intellect", "Intellect"), new(9, "Attention", "Attention"),
        new(10, "Charisma", "Charisma"), new(11, "Memory", "Memory"),
        new(12, "MagDrills", "Magazine Drills"), new(13, "Pistol", "Pistols"),
        new(14, "Revolver", "Revolvers"), new(15, "SMG", "Submachine Guns"),
        new(16, "Assault", "Assault Rifles"), new(17, "Shotgun", "Shotguns"),
        new(18, "Sniper", "Sniper Rifles"), new(19, "LMG", "Light Machine Guns"),
        new(20, "HMG", "Heavy Machine Guns"), new(21, "Launcher", "Launchers"),
        new(22, "AttachedLauncher", "Underbarrel Launchers"), new(23, "Throwing", "Throwing"),
        new(24, "Misc", "Miscellaneous Weapons"), new(25, "Melee", "Melee"),
        new(26, "DMR", "Designated Marksman Rifles"), new(27, "DrawMaster", "Weapon Drawing"),
        new(28, "AimMaster", "Weapon Aiming"), new(29, "RecoilControl", "Recoil Control"),
        new(30, "TroubleShooting", "Troubleshooting"), new(31, "Sniping", "Sniping"),
        new(32, "CovertMovement", "Covert Movement"), new(33, "ProneMovement", "Prone Movement"),
        new(34, "FirstAid", "First Aid"), new(35, "FieldMedicine", "Field Medicine"),
        new(36, "Surgery", "Surgery"), new(37, "LightVests", "Light Armor"),
        new(38, "HeavyVests", "Heavy Armor"), new(39, "WeaponModding", "Weapon Modding"),
        new(40, "AdvancedModding", "Advanced Modding"), new(41, "NightOps", "Night Operations"),
        new(42, "SilentOps", "Silent Ops"), new(43, "Lockpicking", "Lock Picking"),
        new(44, "Search", "Search"), new(45, "WeaponTreatment", "Weapon Maintenance"),
        new(46, "Freetrading", "Free Trading"), new(47, "Auctions", "Auctions"),
        new(48, "Cleanoperations", "Clean Operations"), new(49, "Barter", "Barter"),
        new(50, "Shadowconnections", "Shadow Connections"), new(51, "Taskperformance", "Task Performance"),
        new(52, "BearAssaultoperations", "BEAR Assault Operations"), new(53, "BearAuthority", "BEAR Authority"),
        new(54, "BearAksystems", "Eastern Weapon Proficiency"), new(55, "BearHeavycaliber", "BEAR Heavy Caliber"),
        new(56, "BearRawpower", "BEAR Raw Power"), new(57, "UsecArsystems", "NATO Weapon Proficiency"),
        new(58, "UsecDeepweaponmodding", "USEC Advanced Weapon Modding"), new(59, "UsecLongrangeoptics", "USEC Long Range Optics"),
        new(60, "UsecNegotiations", "USEC Negotiations"), new(61, "UsecTactics", "USEC Tactics"),
        new(64, "AimDrills", "Aim Drills"), new(65, "HideoutManagement", "Hideout Management"),
        new(66, "Crafting", "Crafting"), new(200, "Hacking", "Hacking"),
        new(201, "SignalsIntelligence", "Signals Intelligence"),
    };
    public static readonly IReadOnlyDictionary<int, LevelingSkill> ById = All.ToDictionary(skill => skill.Id);
    public static readonly IReadOnlyDictionary<string, LevelingSkill> ByKey = All.ToDictionary(skill => skill.Key, StringComparer.Ordinal);
}
