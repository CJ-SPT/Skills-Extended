using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class OnEnemyKillPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass),
            nameof(LocationStatisticsCollectorAbstractClass.OnEnemyKill));
    }

    [PatchPostfix]
    private static void Postfix(LocationStatisticsCollectorAbstractClass __instance, WildSpawnType role)
    {
        HandleSilentOps(__instance);
        HandleShadowConnections(__instance, role);
    }

    private static void HandleSilentOps(LocationStatisticsCollectorAbstractClass statisticsCollector)
    {
        if (!SkillsPlugin.SkillData.SilentOps.Enabled)
        {
            return;
        }

        var player = statisticsCollector.Player_0;
        var itemInHands = player.InventoryController.ItemInHands;

        if (itemInHands is null)
        {
            return;
        }
        
        var skillManagerExt = player.Skills.SkillManagerExtended;
        var xp = SkillsPlugin.SkillData.SilentOps.XpPerAction;

        if (player.Skills.SilentOps.IsEliteLevel)
        {
            return;
        }
        
        if (itemInHands.GetItemComponent<KnifeComponent>() is not null)
        {
            player.ExecuteSkill(() => skillManagerExt.SilentOpsMeleeAction.Complete(xp));
#if DEBUG
            Logger.LogDebug("Applying Melee XP to Silent Ops");
#endif
        }
        
        if (itemInHands is Weapon weap)
        {
            var isSuppressed = weap.Mods.Any(x => x is SilencerItemClass);

            if (!isSuppressed) return;
            
            player.ExecuteSkill(() => skillManagerExt.SilentOpsGunAction.Complete(xp));

#if DEBUG
            Logger.LogDebug("Applying Gun XP to Silent Ops");
#endif
        }
    }

    private static void HandleShadowConnections(LocationStatisticsCollectorAbstractClass statisticsCollector, WildSpawnType role)
    {
        if (!SkillsPlugin.SkillData.ShadowConnections.Enabled)
        {
            return;
        }

        if (role != WildSpawnType.sectantWarrior && role != WildSpawnType.sectantPriest)
        {
            return;
        }

        var player = statisticsCollector.Player_0;
        var skillManagerExt = player.Skills.SkillManagerExtended;
        var xp = SkillsPlugin.SkillData.ShadowConnections.XpPerAction;
        
        statisticsCollector.Player_0.ExecuteSkill(() => skillManagerExt.ShadowConnectionsKillAction.Complete(xp));
        
#if DEBUG
        Logger.LogDebug("Applying Gun XP to Shadow Connections");
#endif
    }
}