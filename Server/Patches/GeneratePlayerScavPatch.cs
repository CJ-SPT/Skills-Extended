using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using BodyParts = SPTarkov.Server.Core.Constants.BodyParts;

namespace SkillsExtended.Patches;

[Injectable]
public class GeneratePlayerScavPatch(
    ISptLogger<GeneratePlayerScavPatch> logger,
    BotTable botTable,
    ConfigController configController,
    RandomUtil randomUtil,
    SkillUtil skillUtil
) : AbstractPatch
{
    private static BotTable _botTable = null!;
    private static ConfigController _configController = null!;
    private static RandomUtil _randomUtil = null!;
    private static SkillUtil _skillUtil = null!;
    private static ISptLogger<GeneratePlayerScavPatch> _logger = null!;

    private static bool _generateAsCultist;

    protected override MethodBase? GetTargetMethod()
    {
        _botTable = botTable;
        _configController = configController;
        _randomUtil = randomUtil;
        _skillUtil = skillUtil;
        _logger = logger;

        return AccessTools.Method(typeof(BotGenerator), "GeneratePlayerScav");
    }

    [PatchPrefix]
    public static void Prefix(MongoId sessionId, ref string role)
    {
        if (!_configController.SkillsConfig.ShadowConnections.Enabled)
        {
            return;
        }

        if (!_skillUtil.TryGetSkillLevel(sessionId, SkillTypes.Shadowconnections, out var level))
        {
            return;
        }

        var chanceConfig =
            _configController.SkillsConfig.ShadowConnections.ScavGenerateAsCultistChance * level;

        _generateAsCultist = _randomUtil.GetChance100(chanceConfig);
        if (!_generateAsCultist)
        {
            return;
        }

        _logger.Info("Replacing scav as cultist");
        role = "sectantWarrior";
    }

    [PatchPostfix]
    public static void Postfix(PmcData __result)
    {
        if (!_configController.SkillsConfig.ShadowConnections.Enabled)
        {
            return;
        }

        if (!_generateAsCultist)
        {
            return;
        }

        if (!_botTable!.Types.TryGetValue("sectantwarrior", out var bot))
        {
            Console.WriteLine("[Skills Extended] Failed to find sectantWarrior");
            return;
        }

        SetAppearance(__result, bot!);
        SetHealth(__result, bot!);

        _generateAsCultist = false;
    }

    private static void SetAppearance(PmcData botBase, BotType botTemplate)
    {
        var appearence = botTemplate.BotAppearance;

        botBase.Customization!.Body = appearence.Body.First().Key;
        botBase.Customization.Feet = appearence.Feet.First().Key;
        botBase.Customization.Head = appearence.Head.Last().Key;
        botBase.Customization.Hands = appearence.Hands.First().Key;
        botBase.Customization.Voice = appearence.Voice.First().Key;
    }

    private static void SetHealth(PmcData botBase, BotType botTemplate)
    {
        var templateHealth = botTemplate.BotHealth;
        var health = botTemplate.BotHealth.BodyParts.First();

        var baseHealth = new BotBaseHealth
        {
            Hydration = new CurrentMinMax
            {
                Current = templateHealth.Hydration.Max,
                Maximum = templateHealth.Hydration.Max,
            },
            Energy = new CurrentMinMax
            {
                Current = templateHealth.Energy.Max,
                Maximum = templateHealth.Energy.Max,
            },
            Temperature = new CurrentMinMax
            {
                Current = templateHealth.Temperature.Min,
                Maximum = templateHealth.Temperature.Max,
            },
            BodyParts = new Dictionary<string, BodyPartHealth>()
            {
                {
                    BodyParts.Head,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.Head.Max,
                            Maximum = health.Head.Max,
                        },
                    }
                },
                {
                    BodyParts.Chest,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.Chest.Max,
                            Maximum = health.Chest.Max,
                        },
                    }
                },
                {
                    BodyParts.Stomach,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.Stomach.Max,
                            Maximum = health.Stomach.Max,
                        },
                    }
                },
                {
                    BodyParts.LeftArm,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.LeftArm.Max,
                            Maximum = health.LeftArm.Max,
                        },
                    }
                },
                {
                    BodyParts.RightArm,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.RightArm.Max,
                            Maximum = health.RightArm.Max,
                        },
                    }
                },
                {
                    BodyParts.LeftLeg,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.LeftLeg.Max,
                            Maximum = health.LeftLeg.Max,
                        },
                    }
                },
                {
                    BodyParts.RightLeg,
                    new BodyPartHealth
                    {
                        Health = new CurrentMinMax
                        {
                            Current = health.RightLeg.Max,
                            Maximum = health.RightLeg.Max,
                        },
                    }
                },
            },
        };

        botBase.Health = baseHealth;
    }
}
