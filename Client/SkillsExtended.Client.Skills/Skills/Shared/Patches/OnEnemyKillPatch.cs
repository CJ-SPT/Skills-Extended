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
        return AccessTools.Method(
            typeof(BaseStatisticsManager),
            nameof(BaseStatisticsManager.OnEnemyKill)
        );
    }

    [PatchPostfix]
    private static void Postfix(BaseStatisticsManager __instance, WildSpawnType role)
    {
        HandleSilentOps(__instance);
        HandleShadowConnections(__instance, role);
        HandleUsecNegotiations(__instance, role);
        HandleBearRawPower(__instance, role);
    }

    private static void HandleSilentOps(BaseStatisticsManager statisticsCollector)
    {
        if (!SkillsExtendedPlugin.SkillData.SilentOps.Enabled)
        {
            return;
        }

        var player = statisticsCollector.Player;
        var itemInHands = player.InventoryController.ItemInHands;

        if (itemInHands is null)
        {
            return;
        }

        var skillManagerExt = player.Skills.SkillsExtendedManager;
        var xp = SkillsExtendedPlugin.SkillData.SilentOps.XpPerAction;

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
            var isSuppressed = weap.Mods.Any(x => x is Silencer);

            if (!isSuppressed)
                return;

            player.ExecuteSkill(() => skillManagerExt.SilentOpsGunAction.Complete(xp));

#if DEBUG
            Logger.LogDebug($"Applying `{xp}` Gun XP to Silent Ops");
#endif
        }
    }

    private static void HandleShadowConnections(
        BaseStatisticsManager statisticsCollector,
        WildSpawnType role
    )
    {
        if (!SkillsExtendedPlugin.SkillData.ShadowConnections.Enabled)
        {
            return;
        }

        if (role is WildSpawnType.sectantWarrior or WildSpawnType.sectantPriest)
        {
            var player = statisticsCollector.Player;
            var skillManagerExt = player.Skills.SkillsExtendedManager;
            var xp = SkillsExtendedPlugin.SkillData.ShadowConnections.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.ShadowConnectionsKillAction.Complete(xp));

#if DEBUG
            Logger.LogDebug($"Applied `{xp}` XP to Shadow Connections");
#endif
        }
    }

    private static void HandleUsecNegotiations(
        BaseStatisticsManager statisticsCollector,
        WildSpawnType role
    )
    {
        var skillData = SkillsExtendedPlugin.SkillData.UsecNegotiations;
        if (!skillData.Enabled)
        {
            return;
        }

        var player = statisticsCollector.Player;
        if (player.Side != EPlayerSide.Usec && skillData.FactionLocked)
        {
            return;
        }

        if (role is WildSpawnType.pmcBEAR)
        {
            var skillManagerExt = player.Skills.SkillsExtendedManager;
            var xp = skillData.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.UsecNegotiationsKillAction.Complete(xp));

#if DEBUG
            Logger.LogDebug($"Applied `{xp}` XP to Usec Negotiations");
#endif
        }
    }

    private static void HandleBearRawPower(
        BaseStatisticsManager statisticsCollector,
        WildSpawnType role
    )
    {
        var skillData = SkillsExtendedPlugin.SkillData.BearRawPower;
        if (!skillData.Enabled)
        {
            return;
        }

        var player = statisticsCollector.Player;
        if (player.Side != EPlayerSide.Bear && skillData.FactionLocked)
        {
            return;
        }

        if (role is WildSpawnType.pmcUSEC)
        {
            var skillManagerExt = player.Skills.SkillsExtendedManager;
            var xp = skillData.XpPerAction;
            player.ExecuteSkill(() => skillManagerExt.BearRawPowerKillAction.Complete(xp));

#if DEBUG
            Logger.LogDebug($"Applied `{xp}` XP to Bear raw power");
#endif
        }
    }
}
