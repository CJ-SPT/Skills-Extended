using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Shared.Patches;

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
        HandleUsecNegotiations(__instance, role);
        HandleBearRawPower(__instance, role);
    }

    private static void HandleSilentOps(
        LocationStatisticsCollectorAbstractClass statisticsCollector
        )
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
            Logger.LogDebug($"Applying `{xp}` Melee XP to Silent Ops");
#endif
        }
        
        if (itemInHands is Weapon weap)
        {
            var isSuppressed = weap.Mods.Any(x => x is SilencerItemClass);

            if (!isSuppressed) return;
            
            player.ExecuteSkill(() => skillManagerExt.SilentOpsGunAction.Complete(xp));

#if DEBUG
            Logger.LogDebug($"Applying `{xp}` Gun XP to Silent Ops");
#endif
        }
    }
    
    private static void HandleShadowConnections(
        LocationStatisticsCollectorAbstractClass statisticsCollector, 
        WildSpawnType role
        )
    {
        if (!SkillsPlugin.SkillData.ShadowConnections.Enabled)
        {
            return;
        }
        
        if (role is WildSpawnType.sectantWarrior or WildSpawnType.sectantPriest)
        {
            var player = statisticsCollector.Player_0;
            var skillManagerExt = player.Skills.SkillManagerExtended;
            var xp = SkillsPlugin.SkillData.ShadowConnections.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.ShadowConnectionsKillAction.Complete(xp));
            
#if DEBUG
        Logger.LogDebug($"Applied `{xp}` XP to Shadow Connections");
#endif
        }
    }

    private static void HandleUsecNegotiations(
        LocationStatisticsCollectorAbstractClass statisticsCollector, 
        WildSpawnType role
        )
    {
        var skillData = SkillsPlugin.SkillData.UsecNegotiations;
        if (!skillData.Enabled)
        {
            return;
        }
        
        var player = statisticsCollector.Player_0;
        if (player.Side != EPlayerSide.Usec && skillData.FactionLocked)
        {
            return;
        }
        
        if (role is WildSpawnType.pmcBEAR)
        {
            var skillManagerExt = player.Skills.SkillManagerExtended;
            var xp = skillData.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.UsecNegotiationsKillAction.Complete(xp));
            
#if DEBUG
            Logger.LogDebug($"Applied `{xp}` XP to Usec Negotiations");
#endif
        }
    }

    private static void HandleBearRawPower(
        LocationStatisticsCollectorAbstractClass statisticsCollector, 
        WildSpawnType role
        )
    {
        var skillData = SkillsPlugin.SkillData.BearRawPower;
        if (!skillData.Enabled)
        {
            return;
        }
        
        var player = statisticsCollector.Player_0;
        if (player.Side != EPlayerSide.Bear && skillData.FactionLocked)
        {
            return;
        }
        
        if (role is WildSpawnType.pmcUSEC)
        {
            var skillManagerExt = player.Skills.SkillManagerExtended;
            var xp = skillData.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.BearRawPowerKillAction.Complete(xp));
            
#if DEBUG
            Logger.LogDebug($"Applied `{xp}` XP to Bear raw power");
#endif
        }
    }
}