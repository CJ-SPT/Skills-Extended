using System;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Text;
using EFT;
using HarmonyLib;
using SkillsExtended.Skills.Core;

namespace SkillsExtended.Skills.UI.Patches;

// TODO: What the fuck is this shit?

internal class PersonalBuffFullStringPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2823.GClass2848.GClass2849), nameof(GClass2823.GClass2848.GClass2849.GetStringValue));
    }

    [PatchPostfix]
    public static void PostFix(
        GClass2823.GClass2848.GClass2849 __instance,
        HashSet<string> ___hashSet_0,
        ref string __result)
    {
        if (!SkillsPlugin.SkillData.FieldMedicine.Enabled) return;
        if (__instance.BuffName == "Pain") return;
        
        var durationBuff = SkillManagerExt.Instance(EPlayerSide.Usec).FieldMedicineDurationBonus;
        var chanceBuff = SkillManagerExt.Instance(EPlayerSide.Usec).FieldMedicineChanceBonus;
        
        var flag = __instance.Value.IsZero();
        if (__instance.Delay.IsZero() && (__instance.Duration.IsZero() || __instance.Duration >= 7200f) && __instance.Value.IsZero())
        {
            __result = string.Empty;
            return;
        }
        var stringBuilder = new StringBuilder();
        var text = __instance.BuffName.Localized();
        if (flag && !___hashSet_0.Contains(__instance.BuffName))
        {
            stringBuilder.Append("Applies".Localized() + " ");
            text = text.ToLower();
        }
        stringBuilder.Append(text);
        if (!flag)
        {
            stringBuilder.Append("\n");
            stringBuilder.Append((__instance.Value.Positive() ? "Increase".Localized() : "Decrease".Localized()) + " ");
            stringBuilder.Append(__instance.BuffAbsoluteStringValue());
        }
        if (__instance.Chance < 1f)
        {
            stringBuilder.Append(string.Format("\n{0} {1}%", "UI/ItemAttribute/Chance".Localized(), Math.Round(__instance.Chance * (1f + chanceBuff) * 100f)));
        }
        if (__instance.Delay > 1f)
        {
            stringBuilder.Append(string.Format("\n{0} {1}{2}", "Delay".Localized(), __instance.Delay * (1f + durationBuff), "sec".Localized()));
        }
        if (__instance.Duration > 0f && __instance.Duration < 7200f)
        {
            stringBuilder.AppendFormat("\n{0} {1}{2}", "Duration".Localized(), __instance.Duration * (1f + durationBuff), "sec".Localized());
        }

        __result = stringBuilder.ToString();
    }
}