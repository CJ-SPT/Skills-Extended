using System.Reflection;
using System.Text.Json;
using EFT;
using EFT.InventoryLogic;
using EFT.Interactive;
using JsonType;
using SkillsExtended;
using SkillsExtended.Config;
using SkillsExtended.Core;
using SkillsExtended.Patches;
using SkillsExtended.Skills.Core.Patches;
using SkillsExtended.Skills.FieldMedicine.Patches;
using SkillsExtended.Skills.FirstAid.Patches;
using SkillsExtended.Skills.ProneMovement.Patches;
using SkillsExtended.Skills.SilentOps.Patches;
using SkillsExtended.Skills.Strength.Patches;
using SkillsExtended.Utils;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

var config = JsonSerializer.Deserialize<SkillsConfig>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SkillsConfig.json")));
SkillsExtendedPlugin.SkillData = config;
var controller = new ConfigController { SkillsConfig = config };
var levels = new SkillUtil();
var profiles = new ProfileHelper();
var id = new MongoId("test-profile");
var profile = new SptProfile();
profile.ProfileInfo.ProfileId = id;
profile.CharacterData.PmcData.Id = id;
profile.CharacterData.PmcData.Info.Side = "Bear";
profiles.Profile = profile.CharacterData.PmcData;
Bind(new QuestExperienceRewardPatch(controller, levels, profiles));
Bind(new QuestMoneyRewardPatch(controller, levels));
Bind(new CultistProductionPatch(controller, levels));
Bind(new GetTraderAssortPatch(controller, levels, profiles, new PaymentHelper()));
Bind(new ScavCooldownTimerPatch(controller, new FenceService(), levels, new TimeUtil(), new GlobalTable()));
Bind(new ProneMoveVolumePatch());

var failures = new List<string>();
var assertions = 0;
Run("Quest XP percentages, faction lock and disabled setting", () =>
{
    foreach (var level in new[] { 0, 1, 10, 51 })
    {
        levels.Levels[SkillTypes.BearRawpower] = level;
        profiles.AwardedExperience = 0;
        QuestExperienceRewardPatch.Postfix([new Reward { Type = RewardType.Experience, Value = 10000 }], profile);
        Equal(level * 50, profiles.AwardedExperience, $"XP bonus at level {level}");
    }
    config.BearRawPower.Enabled = false;
    profiles.AwardedExperience = 0;
    QuestExperienceRewardPatch.Postfix([new Reward { Type = RewardType.Experience, Value = 10000 }], profile);
    Equal(0, profiles.AwardedExperience, "disabled XP");
    config.BearRawPower.Enabled = true;
    profile.CharacterData.PmcData.Info.Side = "Usec";
    QuestExperienceRewardPatch.Postfix([new Reward { Type = RewardType.Experience, Value = 10000 }], profile);
    Equal(0, profiles.AwardedExperience, "faction locked XP");
    profile.CharacterData.PmcData.Info.Side = "Bear";
});
Run("Circle timers and disabled bypass", () =>
{
    StartSacrificePatch.Prefix(id);
    foreach (var level in new[] { 0, 1, 10, 51 })
    {
        levels.Levels[SkillTypes.Shadowconnections] = level;
        var craft = new CircleCraftDetails { Time = 14400 };
        CultistProductionPatch.Postfix(craft);
        Equal(14400 - level * 144, craft.Time, $"circle at level {level}");
    }
    StartSacrificePatch.Postfix();
    config.ShadowConnections.Enabled = false;
    var unchanged = new CircleCraftDetails { Time = 14400 };
    CultistProductionPatch.Postfix(unchanged);
    Equal(14400, unchanged.Time, "disabled circle without session");
    config.ShadowConnections.Enabled = true;
});
Run("Quest cash registration and percentage units", () =>
{
    Check(typeof(QuestMoneyRewardPatch).IsDefined(typeof(SPTarkov.DI.Annotations.Injectable)), "registered patch");
    levels.Levels[SkillTypes.UsecNegotiations] = 10;
    var pmc = new PmcData { Id = id, Info = new() { Side = "Usec" } };
    double percent = 15;
    QuestMoneyRewardPatch.Postfix(pmc, ref percent);
    Equal(20, percent, "add five percentage points");
    config.UsecNegotiations.Enabled = false;
    QuestMoneyRewardPatch.Postfix(pmc, ref percent);
    Equal(20, percent, "disabled cash bonus");
    config.UsecNegotiations.Enabled = true;
});
Run("Trader switches, independent faction locks, elite cap and barters", () =>
{
    levels.Levels[SkillTypes.UsecNegotiations] = 10;
    levels.Levels[SkillTypes.BearRawpower] = 10;
    foreach (var enabled in new[] { false, true })
    foreach (var bearLocked in new[] { false, true })
    foreach (var usecLocked in new[] { false, true })
    foreach (var side in new[] { "Bear", "Usec" })
    {
        config.BearRawPower.Enabled = config.UsecNegotiations.Enabled = enabled;
        config.BearRawPower.FactionLocked = bearLocked;
        config.UsecNegotiations.FactionLocked = usecLocked;
        profiles.Profile.Info.Side = side;
        Equal(enabled && (side == "Bear" || !bearLocked) ? 950 : 1000, Price(Traders.PRAPOR), "Prapor eligibility", 0.001);
        Equal(enabled && (side == "Usec" || !usecLocked) ? 950 : 1000, Price(Traders.PEACEKEEPER), "Peacekeeper eligibility", 0.001);
    }
    config.BearRawPower.Enabled = config.UsecNegotiations.Enabled = true;
    config.BearRawPower.FactionLocked = config.UsecNegotiations.FactionLocked = false;
    levels.Levels[SkillTypes.BearRawpower] = levels.Levels[SkillTypes.UsecNegotiations] = 51;
    Equal(860, Price(new MongoId("other")), "both elite discounts", 0.001);
    var oldDiscount = config.BearRawPower.PraporTradingCostDec;
    config.BearRawPower.PraporTradingCostDec = 100;
    Equal(100, Price(Traders.PRAPOR), "90 percent ceiling", 0.001);
    config.BearRawPower.PraporTradingCostDec = oldDiscount;
    Equal(1000, Price(Traders.PRAPOR, "barter-item"), "non-money barter unchanged");
    config.BearRawPower.FactionLocked = config.UsecNegotiations.FactionLocked = true;
});
Run("Scav cooldown disabled and elite behavior", () =>
{
    var scav = new PmcData();
    levels.Levels[SkillTypes.Shadowconnections] = 51;
    config.ShadowConnections.Enabled = false;
    var lookups = levels.Lookups;
    Check(ScavCooldownTimerPatch.Prefix(scav, profiles.Profile), "disabled uses game method");
    Equal(lookups, levels.Lookups, "disabled does not read skills");
    Check(scav.Info.SavageLockTime == null, "disabled doesn't modify timer");
    config.ShadowConnections.Enabled = true;
    Check(!ScavCooldownTimerPatch.Prefix(scav, profiles.Profile), "elite overrides timer");
    Equal(1005, scav.Info.SavageLockTime.Value, "elite five second cooldown");
});
Run("All physical skill switches retain original action and buff arrays", () =>
{
    foreach (var name in new[] { "Endurance", "Strength", "Vitality", "Health", "Metabolism", "StressResistance", "Immunity" })
    {
        var data = typeof(SkillsConfig).GetProperty(name).GetValue(config);
        var enabled = data.GetType().GetProperty("Enabled");
        enabled.SetValue(data, false);
        SkillManager.SkillAction[] originalActions = [new()];
        SkillManager.Buff[] originalBuffs = [new()];
        var actions = originalActions;
        var buffs = originalBuffs;
        var manager = Manager();
        CreatePhysicalSkillsPatch.Prefix(manager, Enum.Parse<ESkillId>(name), ref actions, ref buffs);
        Check(ReferenceEquals(originalActions, actions) && ReferenceEquals(originalBuffs, buffs), name + " disabled");
        enabled.SetValue(data, true);
        CreatePhysicalSkillsPatch.Prefix(manager, Enum.Parse<ESkillId>(name), ref actions, ref buffs);
        Check(!ReferenceEquals(originalActions, actions) && !ReferenceEquals(originalBuffs, buffs), name + " enabled");
        if (name == "Endurance")
        {
            Equal(0.5, manager.EnduranceBuffEnduranceInc.MaxValue, "stamina independent of breath");
            Equal(1, manager.EnduranceBuffBreathTimeInc.MaxValue, "breath setting");
        }
    }
});
Run("Field Medicine modifies returned personal settings only", () =>
{
    var manager = Manager();
    manager.SkillsExtendedManager.FieldMedicineDurationBonus.Value = 0.25f;
    manager.SkillsExtendedManager.FieldMedicineChanceBonus.Value = 0.2f;
    var template = new EFT.HealthSystem.EffectsSettings.StimulatorSettings.StimulatorBuffSettings { Duration = 100, Chance = 0.5f };
    var personal = (EFT.HealthSystem.EffectsSettings.StimulatorSettings.StimulatorBuffSettings)template.Clone();
    PersonalBuffPatch.PostFix(manager, personal);
    Equal(125, personal.Duration, "personal duration");
    Equal(0.6, personal.Chance, "personal chance");
    Equal(100, template.Duration, "template remains unchanged");
    personal.IsBuff = false;
    PersonalBuffPatch.PostFix(manager, personal);
    Equal(125, personal.Duration, "negative effects unchanged");
    config.FieldMedicine.Enabled = false;
    personal.IsBuff = true;
    PersonalBuffPatch.PostFix(manager, personal);
    Equal(125, personal.Duration, "disabled injector unchanged");
    config.FieldMedicine.Enabled = true;
});
Run("First Aid treats all three wounds without changing shared templates", () =>
{
    var ownerSkills = Manager();
    ownerSkills.SkillsExtendedManager.FirstAidResourceCostBuff.Value = 0.25f;
    var component = new HealthEffectsComponent();
    component.Item.Owner = new InventoryController { Profile = new() { SkillsInfo = ownerSkills } };
    Dictionary<EDamageEffectType, DamageEffectSpecification> template = new()
    {
        [EDamageEffectType.LightBleeding] = new() { Cost = 40, Delay = 2, Duration = 3, FadeOut = 4, HealthPenaltyMin = 5, HealthPenaltyMax = 6 },
        [EDamageEffectType.HeavyBleeding] = new() { Cost = 100 },
        [EDamageEffectType.Fracture] = new() { Cost = 80 },
        [EDamageEffectType.DestroyedPart] = new() { Cost = 60 },
    };
    for (var i = 0; i < 3; i++)
    {
        var result = template;
        HealthEffectComponentPatch.Postfix(component, ref result);
        Equal(30, result[EDamageEffectType.LightBleeding].Cost, "light bleed");
        Equal(75, result[EDamageEffectType.HeavyBleeding].Cost, "heavy bleed");
        Equal(60, result[EDamageEffectType.Fracture].Cost, "fracture");
        Equal(60, result[EDamageEffectType.DestroyedPart].Cost, "surgery unaffected");
        Equal(40, template[EDamageEffectType.LightBleeding].Cost, "template stable");
        Equal(6, result[EDamageEffectType.LightBleeding].HealthPenaltyMax, "other fields preserved");
    }
    component.Item.Owner = new InventoryController { Profile = new() { SkillsInfo = Manager() } };
    var otherOwner = template;
    HealthEffectComponentPatch.Postfix(component, ref otherOwner);
    Check(ReferenceEquals(template, otherOwner), "other owner gets own level zero costs");
    config.FirstAid.Enabled = false;
    component.Item.Owner = new InventoryController { Profile = new() { SkillsInfo = ownerSkills } };
    HealthEffectComponentPatch.Postfix(component, ref otherOwner);
    Check(ReferenceEquals(template, otherOwner), "disabled leaves original dictionary");
    config.FirstAid.Enabled = true;
});
Run("Prone volume preserves baseline, nonlocal actors and disabled setting", () =>
{
    var player = new Player { IsYourPlayer = true, Skills = Manager() };
    var context = new MovementContext { _player = player, CurrentState = new ProneMovePlayerState() };
    Equal(0.8, Volume(context), "level zero retains baseline");
    player.Skills.ProneMovementVolume.Value = 0.25f;
    Equal(0.6, Volume(context), "25 percent reduction of baseline");
    player.IsYourPlayer = false;
    Equal(0.8, Volume(context), "remote actor unchanged");
    player.IsYourPlayer = true;
    config.ProneMovement.Enabled = false;
    Equal(0.8, Volume(context), "disabled unchanged");
    config.ProneMovement.Enabled = true;
});
Run("Door audio uses only the local operator's skill", () =>
{
    var player = new Player { IsYourPlayer = true, Skills = Manager() };
    player.Skills.SkillsExtendedManager.SilentOpsReduceVolumeBuff.Value = 0.25f;
    var door = new WorldInteractiveObject { InteractingPlayer = player };
    Equal(0.6, DoorVolume(door), "local operator");
    player.IsYourPlayer = false;
    Equal(0.8, DoorVolume(door), "other actor");
    door.InteractingPlayer = null;
    Equal(0.8, DoorVolume(door), "unattributed door");
    player.IsYourPlayer = true;
    door.InteractingPlayer = player;
    config.SilentOps.Enabled = false;
    Equal(0.8, DoorVolume(door), "disabled door bonus");
    config.SilentOps.Enabled = true;
});
Run("Strength obstacles, elite entry and exit, and disabled/nonlocal bypass", () =>
{
    var player = new Player { IsYourPlayer = true, Skills = Manager() };
    player.Skills.SkillsExtendedManager.StrengthBushSpeedIncBuff.Value = 0.5f;
    var context = new MovementContext { _player = player };
    context._enteredObstacles.Add(new() { HasSwampSpeedLimit = true, ConditionsMask = EPhysicalCondition.SprintDisabled | EPhysicalCondition.JumpDisabled | EPhysicalCondition.ProneDisabled });
    Check(!MovementContextSetSpeedLimitPatch.Prefix(context, player), "local enabled override");
    Equal(0.3, context.SwampLimit.Value, "nonelite speed reduction");
    Check(context.PhysicalConditionIs(EPhysicalCondition.SprintDisabled), "nonelite restrictions");
    player.Skills.SkillsExtendedManager.StrengthBushSpeedIncBuffElite.Value = true;
    MovementContextSetSpeedLimitPatch.Prefix(context, player);
    Check(!context.PhysicalConditionIs(EPhysicalCondition.SprintDisabled) && !context.PhysicalConditionIs(EPhysicalCondition.JumpDisabled), "elite clears stale restrictions");
    Check(context.PhysicalConditionIs(EPhysicalCondition.ProneDisabled), "elite preserves prone restrictions");
    Check(context.SwampLimit == null, "elite removes swamp limit");
    context._enteredObstacles.Clear();
    MovementContextSetSpeedLimitPatch.Prefix(context, player);
    Check(context.Conditions == EPhysicalCondition.None && context.SwampLimit == null, "exit clears obstacle effects");
    player.IsYourPlayer = false;
    Check(MovementContextSetSpeedLimitPatch.Prefix(context, player), "remote uses original");
    player.IsYourPlayer = true;
    config.Strength.Enabled = false;
    Check(MovementContextSetSpeedLimitPatch.Prefix(context, player), "disabled uses original");
    config.Strength.Enabled = true;
});

Console.WriteLine($"{assertions} assertions; {failures.Count} failed groups.");
foreach (var failure in failures) Console.Error.WriteLine(failure);
return failures.Count == 0 ? 0 : 1;

SkillManager Manager()
{
    var manager = new SkillManager();
    manager.SkillsExtendedManager = new(manager, config);
    return manager;
}
void Run(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures.Add(name + ": " + ex.GetBaseException().Message); }
}
void Check(bool condition, string label) { assertions++; if (!condition) throw new Exception(label); }
void Equal(double expected, double actual, string label, double tolerance = 0.00001) => Check(Math.Abs(expected - actual) <= tolerance, $"{label}: expected {expected}, got {actual}");
static void Bind(object patch) => patch.GetType().GetMethod("GetTargetMethod", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(patch, null);
static object Call(Type type, string method, params object[] args) => type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
double Price(MongoId trader, string tpl = "money")
{
    var price = new BarterScheme { Template = new(tpl), Count = 1000 };
    var assort = new TraderAssort { BarterScheme = new() { ["offer"] = [[price]] } };
    GetTraderAssortPatch.Postfix(id, trader, assort);
    return price.Count;
}
static float Volume(MovementContext context)
{
    object[] args = [context, 0.8f];
    Call(typeof(ProneMoveVolumePatch), "Postfix", args);
    return (float)args[1];
}
static float DoorVolume(WorldInteractiveObject door)
{
    object[] args = [door, 0.8f];
    Call(typeof(DoorSoundPatch), "Prefix", args);
    return (float)args[1];
}
