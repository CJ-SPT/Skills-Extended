using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using BodyParts = SPTarkov.Server.Core.Constants.BodyParts;

namespace SkillsExtended.Patches;

public class GeneratePlayerScavPatch : AbstractPatch
{
    private static readonly DatabaseService DatabaseService = ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotGenerator), "GeneratePlayerScav");
    }

    [PatchPrefix]
    public static void Prefix(BotGenerator __instance, ref string role)
    {
        Console.WriteLine("Generating cultist");
        
        role = "sectantWarrior";
    }

    [PatchPostfix]
    public static void Postfix(BotGenerator __instance, PmcData __result)
    {
        if (!DatabaseService.GetBots().Types.TryGetValue("sectantwarrior", out var bot))
        {
            Console.WriteLine("Failed to find sectantWarrior");
            return;
        }
        
        SetAppearance(__result, bot!);
        SetHealth(__result, bot!);
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