using System.Globalization;
using System.Reflection;
using SkillsExtended.Config;

namespace SkillsExtended.Core.Editing;

public record SkillDefinition(
    string Key,
    string Slug,
    string Name,
    string Icon,
    string Description,
    string Category
)
{
    public string Url => "/skills-extended/" + Slug;

    public object Data(SkillsConfig config) =>
        typeof(SkillsConfig).GetProperty(Key)!.GetValue(config)!;

    public IReadOnlyList<SettingDefinition> Fields => SkillCatalog.Fields[Key];
}

public record SettingDefinition(
    PropertyInfo Property,
    string Label,
    string Unit,
    string Group,
    double? Maximum
)
{
    public string Key => Property.Name;
    public bool IsBoolean => Property.PropertyType == typeof(bool);
    public bool IsInteger => Property.PropertyType == typeof(int);
    public string Help =>
        IsBoolean
            ? (
                Key == "FactionLocked"
                    ? "BEAR or USEC, according to this skill."
                    : "Changes are applied after saving and restarting the game client."
            )
        : Unit == "ratio" ? "A fraction from 0 to 1. For example, 0.15 means 15%."
        : Unit == "%" ? "Percentage points: 0.5 means 0.5%, not 50%."
        : "Enter a nonnegative value. Existing precision is preserved.";

    public string? Parse(string text, out object? value)
    {
        value = null;
        if (
            !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var number)
            || !double.IsFinite(number)
            || number < 0
        )
        {
            return "Enter a finite, nonnegative number.";
        }

        if (Maximum is { } maximum && number > maximum)
        {
            return $"Enter a value between 0 and {maximum}.";
        }

        if (IsInteger)
        {
            if (number != Math.Truncate(number) || number > int.MaxValue)
            {
                return "Enter a nonnegative whole number.";
            }

            value = (int)number;
        }
        else
        {
            if (number > float.MaxValue)
            {
                return "This number is too large.";
            }

            value = (float)number;
        }

        return null;
    }
}

public static class SkillCatalog
{
    public static readonly IReadOnlyList<SkillDefinition> All =
    [
        new(
            "SignalsIntelligence",
            "signals-intelligence",
            "Signals Intelligence",
            "Skill_SignalsIntelligence",
            "Tune and triangulate hidden supply caches using the Modified PDA.",
            "Extended skills"
        ),
        new(
            "Hacking",
            "hacking",
            "Hacking",
            "Skill_Hacking",
            "Hack keycard doors with a Modified PDA. Configure attempts, hacking stats, and door difficulty.",
            "Extended skills"
        ),
        new(
            "FirstAid",
            "first-aid",
            "First Aid",
            "Skill_FirstAid",
            "Reduce medkit resource costs and use time.",
            "Extended skills"
        ),
        new(
            "FieldMedicine",
            "field-medicine",
            "Field Medicine",
            "Skill_FieldMedicine",
            "Improve positive injector effects and duration.",
            "Extended skills"
        ),
        new(
            "NatoWeapons",
            "nato-weapons",
            "NATO Weapon Proficiency",
            "Skill_NatoRifle",
            "Tune weapon handling, XP sharing, and the NATO weapon list.",
            "Extended skills"
        ),
        new(
            "EasternWeapons",
            "eastern-weapons",
            "Eastern Weapon Proficiency",
            "Skill_EasternRifle",
            "Tune weapon handling, XP sharing, and the Eastern weapon list.",
            "Extended skills"
        ),
        new(
            "LockPicking",
            "lock-picking",
            "Lock Picking",
            "Skill_Lockpicking",
            "Configure pick time, forgiveness, XP, and per-map lock difficulty.",
            "Extended skills"
        ),
        new(
            "ProneMovement",
            "prone-movement",
            "Prone Movement",
            "Skill_ProneMovement",
            "Adjust movement speed and sound while prone.",
            "Extended skills"
        ),
        new(
            "SilentOps",
            "silent-ops",
            "Silent Ops",
            "Skill_SilentOps",
            "Adjust melee speed, door sound, and suppressor discounts.",
            "Extended skills"
        ),
        new(
            "ShadowConnections",
            "shadow-connections",
            "Shadow Connections",
            "Skill_ShadowConnections",
            "Configure scav cooldown, Cultist Circle returns, and Cultist scav chance.",
            "Extended skills"
        ),
        new(
            "UsecNegotiations",
            "usec-negotiations",
            "USEC Negotiations",
            "Skill_UsecNegotiations",
            "Configure USEC trade discounts and quest cash bonuses.",
            "Extended skills"
        ),
        new(
            "BearRawPower",
            "bear-raw-power",
            "BEAR Raw Power",
            "Skill_BearRawPower",
            "Configure BEAR trade discounts and quest XP bonuses.",
            "Extended skills"
        ),
        new(
            "Endurance",
            "endurance",
            "Endurance",
            "Skill_Endurance",
            "Tune stamina, jumping costs, and breath recovery.",
            "Physical skills"
        ),
        new(
            "Strength",
            "strength",
            "Strength",
            "Skill_Strength",
            "Tune carrying capacity, movement, and melee bonuses.",
            "Physical skills"
        ),
        new(
            "Vitality",
            "vitality",
            "Vitality",
            "Skill_Vitality",
            "Reduce bleeding and death from losing a limb.",
            "Physical skills"
        ),
        new(
            "Health",
            "health",
            "Health",
            "Skill_Health",
            "Tune fracture resistance and energy and hydration consumption.",
            "Physical skills"
        ),
        new(
            "Metabolism",
            "metabolism",
            "Metabolism",
            "Skill_Metabolism",
            "Improve food and drink effects and reduce debuff duration.",
            "Physical skills"
        ),
        new(
            "StressResistance",
            "stress-resistance",
            "Stress Resistance",
            "Skill_StressResistance",
            "Reduce pain shock and tremor.",
            "Physical skills"
        ),
        new(
            "Immunity",
            "immunity",
            "Immunity",
            "Skill_Immunity",
            "Tune negative effects, poison resistance, and painkiller duration.",
            "Physical skills"
        ),
    ];
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["AttemptsPerDoor"] = "Failed attempts allowed per door",
        ["BaseCoherence"] = "Starting coherence at level zero",
        ["CoherencePerLevel"] = "Extra coherence per level",
        ["BaseStrength"] = "Starting attack strength",
        ["LevelsPerStrength"] = "Levels for each extra strength point",
        ["UtilitySlots"] = "Utility inventory slots",
        ["EliteUtilitySlots"] = "Utility inventory slots at elite",
        ["FailureXpRatio"] = "Coherence loss XP ratio",
        ["PdaReferenceValue"] = "PDA reference value in roubles",
        ["DefaultDifficulty"] = "Default difficulty (1-3)",
        ["AllTraderCostDecrease"] = "All-trader discount at elite",
        ["AlwaysLevelEndurance"] = "Always Level Endurance",
        ["AlwaysLevelStrength"] = "Always Level Strength",
        ["AttemptsBeforeBreak"] = "Attempts before a lock breaks",
        ["AvoidMiscEffectsChanceElite"] = "Negative effect avoidance chance at elite",
        ["AvoidPoisonChanceElite"] = "Poison avoidance chance at elite",
        ["BreakChanceRedPerLevel"] = "Fracture Chance Reduction Per Level",
        ["BuffAimFatigueMax"] = "Arm fatigue reduction at maximum level",
        ["BuffBleedChanceRedPerLevel"] = "Bleed Chance Reduction Per Level",
        ["BuffBreathTimeIncMax"] = "Breath holding bonus at maximum level",
        ["BuffEnduranceIncElite"] = "Stamina elite bonus",
        ["BuffEnduranceIncMax"] = "Stamina bonus at maximum level",
        ["BuffJumpCostRedMax"] = "Jump cost reduction at maximum level",
        ["BuffJumpHeightIncMax"] = "Jump height bonus at maximum level",
        ["BuffLiftWeightIncMax"] = "Carrying capacity bonus at maximum level",
        ["BuffMeleeCritsEliteBonus"] = "Melee critical chance elite bonus",
        ["BuffMeleeCritsPerLevel"] = "Melee Crits Per Level",
        ["BuffMeleePowerIncMax"] = "Melee power bonus at maximum level",
        ["BuffRestorationElite"] = "Breath recovery elite bonus",
        ["BuffRestorationMax"] = "Breath recovery bonus at maximum level",
        ["BuffSprintSpeedIncMax"] = "Sprint speed bonus at maximum level",
        ["BuffSurviobilityIncPerLevel"] = "Limb Loss Death Chance Reduction Per Level",
        ["BuffThrowDistanceIncMax"] = "Throw distance bonus at maximum level",
        ["ColliderSpeedBuffMax"] = "Bush and swamp movement bonus at maximum level",
        ["CultistCircleReturnTimeReduction"] = "Cultist Circle time reduction per level",
        ["DurationBonus"] = "Duration Bonus Increase Per Level",
        ["Enabled"] = "Enable this skill",
        ["EnergyPerLevel"] = "Energy Reduction Per Level",
        ["ErgoMod"] = "Ergo Modifier Per Level",
        ["FactionLocked"] = "Restrict bonuses to the matching faction",
        ["FailureLockXpRatio"] = "Failed attempt XP ratio",
        ["HandsElite"] = "Hand stamina elite bonus",
        ["HandsPerLevel"] = "Hand stamina increase per level",
        ["HydrationPerLevel"] = "Hydration Reduction Per Level",
        ["InspectLockXpRatio"] = "Inspection XP ratio",
        ["ItemSpeedBonus"] = "Medkit Speed Bonus Per Level",
        ["MedkitUsageReduction"] = "Resource Cost Reduction Per Level",
        ["MeleeSpeedInc"] = "Melee Speed Increase Per Level",
        ["MiscDebuffTimePerLevel"] = "Negative food effect duration reduction per level",
        ["MiscEffectsPerLevel"] = "Negative Food Effect Reduction Per Level",
        ["MovementSpeedInc"] = "Movement Speed Increase Per Level",
        ["MovementVolumeDec"] = "Volume Decrease Per Level",
        ["PainKillerPerLevel"] = "Painkiller Action Time Increase Per Level",
        ["PeacekeeperTradingCostDec"] = "Peacekeeper discount per level",
        ["PickStrengthBase"] = "Base pick time (seconds)",
        ["PickStrengthPerLevel"] = "Pick time bonus per level",
        ["PoisonBuffPerLevel"] = "Poison Effect Reduction Per Level",
        ["PositiveEffectChanceBonus"] = "Positive injector effect chance bonus per level",
        ["PraporTradingCostDec"] = "Prapor discount per level",
        ["QuestExpRewardInc"] = "Quest XP increase per level",
        ["QuestMoneyRewardInc"] = "Quest money increase per level",
        ["RatioPlusPerLevel"] = "Food and drink restoration bonus per level",
        ["RecoilReduction"] = "Recoil Reduction Per Level",
        ["ScavCooldownTimeDec"] = "Scav cooldown reduction per level",
        ["ScavGenerateAsCultistChance"] = "Cultist scav chance per level",
        ["SilencerPriceReduction"] = "Suppressor discount per level",
        ["SkillBonus"] = "Skill Bonus Cap Increase Per Level",
        ["SkillShareEnabled"] = "Share weapon XP",
        ["SkillShareXpRatio"] = "Shared weapon XP ratio",
        ["StressPainPerLevel"] = "Pain Shock Chance Reduction Per Level",
        ["StressTremorPerLevel"] = "Tremor Reduction Per Level",
        ["SweetSpotRangeBase"] = "Base sweet spot half-angle (degrees)",
        ["SweetSpotRangePerLevel"] = "Sweet spot bonus per level",
        ["VolumeReduction"] = "Door volume reduction per level",
        ["XpPerAction"] = "XP Per Action",
    };
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<SettingDefinition>> Fields =
        All.ToDictionary(
            s => s.Key,
            s =>
                (IReadOnlyList<SettingDefinition>)
                    typeof(SkillsConfig)
                        .GetProperty(s.Key)!
                        .PropertyType.GetProperties()
                        .Where(p =>
                            p.PropertyType == typeof(bool)
                            || p.PropertyType == typeof(float)
                            || p.PropertyType == typeof(int)
                        )
                        .Select(p => Describe(p))
                        .ToArray()
        );

    private static SettingDefinition Describe(PropertyInfo p)
    {
        var key = p.Name;
        if (p.DeclaringType == typeof(SkillsExtended.Config.Skills.SignalsIntelligenceData))
        {
            var signalUnit =
                key.EndsWith("Seconds") ? "seconds"
                : key.Contains("Uncertainty") ? "degrees"
                : key == "MinimumSeparation" ? "metres"
                : key == "TuningTolerance" ? "MHz"
                : key == "TuningBonus" ? "ratio"
                : key.EndsWith("Xp") ? "XP"
                : key.Contains("LootValue") ? "roubles"
                : "";
            return new SettingDefinition(
                p,
                key == "Enabled"
                    ? "Enable Signals Intelligence"
                    : System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2"),
                signalUnit,
                "Receiver and rewards",
                null
            );
        }
        var ratio =
            key
            is "SkillShareXpRatio"
                or "InspectLockXpRatio"
                or "FailureLockXpRatio"
                or "FailureXpRatio";
        var chance = key is "AvoidPoisonChanceElite" or "AvoidMiscEffectsChanceElite";
        var unit =
            ratio ? "ratio"
            : key == "XpPerAction" ? "XP"
            : key == "PickStrengthBase" ? "seconds"
            : key == "SweetSpotRangeBase" ? "degrees"
            : p.PropertyType == typeof(bool) || p.PropertyType == typeof(int) ? ""
            : "%";
        var group =
            p.PropertyType == typeof(bool) ? "Behavior"
            : key.Contains("Elite") || key == "AllTraderCostDecrease" ? "Elite bonuses"
            : key.Contains("Xp") ? "Progression"
            : "Bonuses";
        return new(
            p,
            Labels.GetValueOrDefault(key, key),
            unit,
            group,
            ratio ? 1
                : chance ? 100
                : null
        );
    }
}
