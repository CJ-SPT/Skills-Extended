using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Extensions;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using BodyParts = SPTarkov.Server.Core.Constants.BodyParts;

namespace SkillsExtended.Patches;

public class GeneratePlayerScavPatch : AbstractPatch
{
    private static readonly DatabaseService DatabaseService = ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly RandomUtil RandomUtil = ServiceLocator.ServiceProvider.GetRequiredService<RandomUtil>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    private static readonly ISptLogger<SkillsExtendedPatch> Logger = ServiceLocator.ServiceProvider.GetRequiredService<ISptLogger<SkillsExtendedPatch>>();
    
    private static bool _generateAsCultist;
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator), "GeneratePlayerScav");
    }

    [PatchPrefix]
    public static void Prefix(MongoId sessionId, ref string role)
    {
        if (!SkillUtil.TryGetSkillLevel(sessionId, SkillTypes.Shadowconnections, out var level))
        {
            return;
        }

        var chanceConfig = ConfigController.SkillsConfig.ShadowConnections.ScavGenerateAsCultistChance * level;
        
        _generateAsCultist = RandomUtil.GetChance100(chanceConfig);
        if (!_generateAsCultist)
        {
            return;
        }
        
        Logger.Info("[Skills Extended] Replacing scav as cultist");
        role = "sectantWarrior";
    }

    [PatchPostfix]
    public static void Postfix(PmcData __result)
    {
        if (!_generateAsCultist)
        {
            return;
        }
        
        if (!DatabaseService.GetBots().Types.TryGetValue("sectantwarrior", out var bot))
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
           }
       };
       
       botBase.Health = baseHealth;
    }
}