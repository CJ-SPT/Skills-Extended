using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Skills;

public class OnEnemyKillPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocationStatisticsCollectorAbstractClass),
            nameof(LocationStatisticsCollectorAbstractClass.OnEnemyKill));
    }

    [PatchPostfix]
    private static void Postfix(Player ___player_0, DamageInfo damage)
    {
        if (!Plugin.SkillData.SilentOps.Enabled) return;
        
        var itemInHands = ___player_0.InventoryControllerClass.ItemInHands;
        
        if (itemInHands is null) return;

        var skills = Plugin.PlayerSkillManagerExt;
        var xp = Plugin.SkillData.SilentOps.XpPerAction;

        if (___player_0.Skills.SilentOps.IsEliteLevel) return;
        
        if (itemInHands.GetItemComponent<KnifeComponent>() is not null)
        {
            ___player_0.ExecuteSkill(() => skills.SilentOpsMeleeAction.Complete(xp));
            Logger.LogDebug($"Applying Melee XP to Silent Ops");
        }
        
        if (itemInHands is Weapon weap)
        {
            var isSuppressed = weap.Mods.Any(x => x is GClass2671);

            if (!isSuppressed) return;
            
            ___player_0.ExecuteSkill(() => skills.SilentOpsGunAction.Complete(xp));
            Logger.LogDebug($"Applying Gun XP to Silent Ops");
        }
    }
}