using System.Reflection;
using System.Text;
using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Buffs;
using SkillsExtended.Controllers;
using SkillsExtended.Models;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.UI;

public class SkillLevelPanelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.method_1));
    }

    [PatchPostfix]
    private static void Postfix(
        SkillClass ___skillClass,
        CustomTextMeshProUGUI ____level
        )
    {
        ____level.text += BuildBuffText(BuffController.GetActiveBuffForSkill(___skillClass.Id));
    }

    private static string BuildBuffText(AbstractBuff buff)
    {
        if (buff is null) return string.Empty;

        var model = buff.Buff;
        
        var buffText = $": <color=#54C1FFFF>({model.Strength} level boost)</color>";
        
        return buffText;
    }
}