using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Config;

// Sparse overrides: an absent value preserves the game's rule, including changes made by other mods.
public sealed class NativeSkillData
{
    public Dictionary<string, Dictionary<string, float>> Overrides { get; set; } = new();

    public void Validate()
    {
        if (Overrides == null) throw new ArgumentException("Missing built-in skill overrides.");
        foreach (var skill in Overrides)
        {
            if (!NativeSkillCatalog.ByKey.TryGetValue(skill.Key, out var definition) || skill.Value == null)
                throw new ArgumentException("Unknown built-in skill configuration: " + skill.Key);
            foreach (var value in skill.Value)
            {
                var field = definition.Fields.FirstOrDefault(f => f.Key == value.Key);
                if (field == null || !field.IsValid(value.Value))
                    throw new ArgumentException(definition.Name + ": invalid bonus " + value.Key);
            }
        }
    }
}

public sealed record NativeSetting(string Key, string Label, string Unit, int BuffId = 0,
    string Rule = "", float Scale = 1, float Maximum = 100, bool Integer = false)
{
    public bool IsValid(float value) => !float.IsNaN(value) && !float.IsInfinity(value)
        && value >= 0 && value <= Maximum && (!Integer || value == Math.Truncate(value));
}

public sealed record NativeSkillDefinition(string Key, string Slug, string Category, NativeSetting[] Fields)
{
    public string Name => LevelingSkillCatalog.ByKey[Key].Name;
    public int Id => LevelingSkillCatalog.ByKey[Key].Id;
    public string Url => "/skills-extended/" + Slug;
}

public static class NativeSkillCatalog
{
    private static NativeSetting B(int id, string rule, string label) =>
        new("Buff." + id + "." + rule, label, "%", id, rule, 0.01f);
    private static NativeSetting G(string path, string label, bool percent = true, bool integer = false) =>
        new(path, label, percent ? "%" : "", Scale: percent ? 0.01f : 1,
            Integer: integer);
    private static NativeSkillDefinition Weapon(string key, string slug) => new(key, slug, "Weapon skills",
        new[] { B(65, "PerLevel", "Reload speed bonus per level"), B(67, "PerLevel", "Weapon switching speed bonus per level"),
            B(66, "PerLevel", "Recoil reduction per level"), B(84, "PerLevel", "Ergonomics bonus per level") });

    public static readonly IReadOnlyList<NativeSkillDefinition> All = new NativeSkillDefinition[]
    {
        new("Perception", "perception", "Mental skills", new[] { B(36, "PerLevel", "Loot detection distance bonus per level") }),
        new("Intellect", "intellect", "Mental skills", new[] { B(38, "PerLevel", "Learning speed bonus per level"),
            B(39, "PerLevel", "Weapon maintenance bonus per level"),
            G("Intellect.RepairPointsCostReduction", "Repair point cost reduction per level"),
            G("Intellect.WearAmountReducePerLevel", "Repair wear reduction per level"),
            G("Intellect.WearChanceReduceEliteLevel", "Repair wear chance reduction at elite") }),
        new("Attention", "attention", "Mental skills", new[] { B(44, "PerLevel", "Loot search speed bonus per level"),
            B(45, "PerLevel", "Examination bonus per level"), B(46, "Elite", "Lucky search bonus at elite") }),
        new("Charisma", "charisma", "Mental skills", new[] {
            G("Charisma.BonusSettings.LevelBonusSettings.HealthRestoreDiscount", "Healing discount per level"),
            G("Charisma.BonusSettings.LevelBonusSettings.HealthRestoreTraderDiscount", "Trader healing discount per level"),
            G("Charisma.BonusSettings.LevelBonusSettings.InsuranceDiscount", "Insurance discount per level"),
            G("Charisma.BonusSettings.LevelBonusSettings.InsuranceTraderDiscount", "Trader insurance discount per level"),
            G("Charisma.BonusSettings.LevelBonusSettings.PaidExitDiscount", "Paid extraction discount per level"),
            G("Charisma.BonusSettings.LevelBonusSettings.RepeatableQuestChangeDiscount", "Daily quest replacement discount per level"),
            G("Charisma.BonusSettings.EliteBonusSettings.ScavCaseDiscount", "Scav Case discount at elite"),
            G("Charisma.BonusSettings.EliteBonusSettings.FenceStandingLossDiscount", "Fence reputation loss reduction at elite"),
            G("Charisma.BonusSettings.EliteBonusSettings.RepeatableQuestExtraCount", "Extra daily quests at elite", false, true) }),
        Weapon("Pistol", "pistols"), Weapon("Revolver", "revolvers"), Weapon("SMG", "submachine-guns"),
        Weapon("Assault", "assault-rifles"), Weapon("Shotgun", "shotguns"), Weapon("Sniper", "sniper-rifles"),
        Weapon("LMG", "light-machine-guns"), Weapon("Launcher", "launchers"), Weapon("Melee", "melee"), Weapon("DMR", "designated-marksman-rifles"),
        new("Throwing", "throwing", "Combat skills", new[] { B(88, "PerLevel", "Throw strength bonus per level"), B(89, "PerLevel", "Energy cost reduction per level") }),
        new("RecoilControl", "recoil-control", "Combat skills", new[] { G("RecoilControl.RecoilBonusPerLevel", "Recoil reduction per level") }),
        new("AimDrills", "aim-drills", "Combat skills", new[] { B(68, "Max", "Aiming speed bonus at maximum level"), B(71, "Max", "Aiming sound reduction at maximum level") }),
        new("TroubleShooting", "troubleshooting", "Combat skills", new[] {
            G("TroubleShooting.MalfRepairSpeedBonusPerLevel", "Malfunction repair speed bonus per level"),
            G("TroubleShooting.EliteAmmoChanceReduceMult", "Ammo malfunction chance multiplier at elite", false),
            G("TroubleShooting.EliteDurabilityChanceReduceMult", "Durability malfunction chance multiplier at elite", false),
            G("TroubleShooting.EliteMagChanceReduceMult", "Magazine malfunction chance multiplier at elite", false) }),
        new("CovertMovement", "covert-movement", "Practical skills", new[] { B(77, "PerLevel", "Covert movement sound reduction per level"),
            B(93, "PerLevel", "Surface sound reduction per level"), B(93, "Elite", "Surface sound reduction at elite") }),
        new("Surgery", "surgery", "Practical skills", new[] { B(98, "Max", "Surgery speed bonus at maximum level"), B(98, "Elite", "Surgery speed bonus at elite"),
            B(97, "PerLevel", "Restored limb health bonus per level"), B(97, "Elite", "Restored limb health bonus at elite") }),
        new("Search", "search", "Practical skills", new[] { B(95, "PerLevel", "Container search speed bonus per level") }),
        new("MagDrills", "magazine-drills", "Practical skills", new[] { B(48, "PerLevel", "Magazine loading speed bonus per level"),
            B(49, "PerLevel", "Magazine unloading speed bonus per level"), B(50, "PerLevel", "Magazine checking speed bonus per level") }),
        new("LightVests", "light-armor", "Practical skills", new[] {
            G("LightVests.MoveSpeedPenaltyReductionLVestsReducePerLevel", "Movement penalty reduction per level"),
            G("LightVests.MeleeDamageLVestsReducePerLevel", "Melee damage reduction per level"),
            G("LightVests.WearAmountRepairLVestsReducePerLevel", "Repair wear reduction per level"),
            G("LightVests.WearChanceRepairLVestsReduceEliteLevel", "Repair wear chance reduction at elite") }),
        new("HeavyVests", "heavy-armor", "Practical skills", new[] {
            G("HeavyVests.MoveSpeedPenaltyReductionHVestsReducePerLevel", "Movement penalty reduction per level"),
            G("HeavyVests.BluntThroughputDamageHVestsReducePerLevel", "Blunt damage reduction per level"),
            G("HeavyVests.WearAmountRepairHVestsReducePerLevel", "Repair wear reduction per level"),
            G("HeavyVests.WearChanceRepairHVestsReduceEliteLevel", "Repair wear chance reduction at elite") }),
        new("WeaponTreatment", "weapon-maintenance", "Practical skills", new[] {
            G("WeaponTreatment.DurLossReducePerLevel", "Weapon durability loss reduction per level"),
            G("WeaponTreatment.WearAmountRepairGunsReducePerLevel", "Repair wear reduction per level"),
            G("WeaponTreatment.WearChanceRepairGunsReduceEliteLevel", "Repair wear chance reduction at elite") }),
        new("Crafting", "crafting", "Hideout skills", new[] {
            new NativeSetting("Crafting.CraftTimeReductionPerLevel", "Single craft time reduction per level", "%"),
            new NativeSetting("Crafting.ProductionTimeReductionPerLevel", "Continuous production time reduction per level", "%"),
            G("Crafting.EliteExtraProductions", "Extra productions at elite", false, true) }),
        new("HideoutManagement", "hideout-management", "Hideout skills", new[] {
            new NativeSetting("HideoutManagement.ConsumptionReductionPerLevel", "Resource consumption reduction per level", "%"),
            new NativeSetting("HideoutManagement.SkillBoostPercent", "Area bonus boost per level", "%"),
            G("HideoutManagement.CircleOfCultistsBonusPercent", "Cultist Circle bonus per level") }),
    };
    public static readonly IReadOnlyDictionary<string, NativeSkillDefinition> ByKey = All.ToDictionary(s => s.Key);
    public static IReadOnlyDictionary<string, float> Defaults => NativeSkillDefaults.Values;
}
