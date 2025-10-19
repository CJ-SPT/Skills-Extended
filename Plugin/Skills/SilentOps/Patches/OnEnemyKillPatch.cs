using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
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
    private static void Postfix(LocationStatisticsCollectorAbstractClass __instance, DamageInfoStruct damage)
    {
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;

        var player = __instance.Player_0;
        var itemInHands = player.InventoryController.ItemInHands;
        
        if (itemInHands is null) return;

        var skills = SkillManagerExt.Instance(EPlayerSide.Usec);
        var xp = SkillsPlugin.SkillData.SilentOps.XpPerAction;

        if (player.Skills.SilentOps.IsEliteLevel) return;
        
        if (itemInHands.GetItemComponent<KnifeComponent>() is not null)
        {
            player.ExecuteSkill(() => skills.SilentOpsMeleeAction.Complete(xp));
            Logger.LogDebug($"Applying Melee XP to Silent Ops");
        }
        
        if (itemInHands is Weapon weap)
        {
            var isSuppressed = weap.Mods.Any(x => x is SilencerItemClass);

            if (!isSuppressed) return;
            
            player.ExecuteSkill(() => skills.SilentOpsGunAction.Complete(xp));
            Logger.LogDebug($"Applying Gun XP to Silent Ops");
        }
    }
}