using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FirstAid.Patches;

internal class HealthEffectUseTimePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(
            typeof(HealthEffectsComponent),
            nameof(HealthEffectsComponent.UseTime)
        );
    }

    [PatchPostfix]
    public static void PostFix(ref float __result, HealthEffectsComponent __instance)
    {
        var firstAid = SkillsExtendedPlugin.SkillData.FirstAid;

        if (!firstAid.Enabled)
        {
            return;
        }

        // Headless hosts have no local player. Medical effects belong to the
        // inventory owner, whose skills also drive the resource-cost patch.
        if (__instance?.Item?.Owner is not InventoryController inventory
            || inventory.Profile?.SkillsInfo is not SkillManager skillManager
            || skillManager.SkillsExtendedManager == null)
        {
            return;
        }

        __result *= 1f - skillManager.SkillsExtendedManager.FirstAidItemSpeedBuff;
    }
}

internal class SpawnPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(Player.MedsController),
            nameof(Player.MedsController.Spawn)
        );
    }

    [PatchPrefix]
    public static void PreFix(Player.MedsController __instance, ref float animationSpeed)
    {
        var firstAid = SkillsExtendedPlugin.SkillData.FirstAid;

        if (!firstAid.Enabled)
        {
            return;
        }

        if (__instance?.Item?.Owner is not InventoryController inventory
            || inventory.Profile?.SkillsInfo is not SkillManager skillManager
            || skillManager.SkillsExtendedManager == null)
        {
            return;
        }

        animationSpeed *= 1f + skillManager.SkillsExtendedManager.FirstAidItemSpeedBuff;
    }
}
