using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.UI;

public class QuestFinishPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LocalQuestControllerClass), nameof(LocalQuestControllerClass.FinishQuest));
    }

    [PatchPostfix]
    public static void Postfix(QuestClass quest)
    {
        Logger.LogDebug($"Finished Quest {quest.Template.Name.Localized()}");
    }
}