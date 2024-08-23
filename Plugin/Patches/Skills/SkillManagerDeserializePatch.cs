using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace SkillsExtended.Patches.Skills;

/// <summary>
/// Rewrite the method that deserializes the skill manager to support assigning the correct side. These fields had to be added as a pre-patch.
/// </summary>
public class SkillManagerDeserializePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type = PatchConstants.EftTypes.SingleCustom(t =>
            t.GetField("Mastering") is not null
            && t.GetMethod("Deserialize") is not null);

        Logger.LogDebug($"Resolved patch type for {nameof(SkillManagerDeserializePatch)} to be {type!.Name}");
        
        var method = type!.GetMethod("Deserialize");

        return method;
    }

    [PatchPrefix]
    private static bool Prefix(GClass1795 __instance, ref SkillManager __result)
    {
        if (__instance.Side == 0x00000000)
        {
            __instance.Side = EPlayerSide.Bear;
            Logger.LogDebug("Creating SkillManager default of Bear");
        }
        
        __result = new SkillManager(__instance.Side);
        
        if (__instance.Common is not null)
        {
            foreach (var gclass in __instance.Common)
            {
                var skill = __result.GetSkill(gclass.Id);

                if (skill is null) continue;
                
                skill.UpdateFromInfo(gclass);
            }
        }

        if (__instance.Mastering is not null)
        {
            foreach (var gclass in __instance.Mastering)
            {
                __result.MasterNewWeapon(
                    Singleton<BackendConfigSettingsClass>.Instance.Mastering.FirstOrDefault(x => x.Id == gclass.Id),
                    gclass.Progress
                );
            }
        }
        
        return false;
    }
}