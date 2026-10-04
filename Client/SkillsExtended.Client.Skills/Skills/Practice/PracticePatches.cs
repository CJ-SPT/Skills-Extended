using System.Reflection;
using EFT;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Practice;

internal sealed class PracticeSkillPanelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.Show));
    [PatchPostfix]
    private static void Postfix(SkillPanel __instance, Skill skill) => PracticeSkillButton.Bind(__instance, skill, false);
}

internal sealed class PracticeSkillIconPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillIcon), nameof(SkillIcon.Show));
    [PatchPostfix]
    private static void Postfix(SkillIcon __instance, Skill skill) => PracticeSkillButton.Bind(__instance, skill, true);
}

internal sealed class PracticeSkillsClosePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillsScreen), nameof(SkillsScreen.Close));
    [PatchPrefix]
    private static void Prefix(SkillsScreen __instance) => PracticeController.ScreenClosed(__instance);
}

internal sealed class PracticeWorldStartingPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.InitLevel));
    [PatchPrefix]
    private static void Prefix() => PracticeController.WorldStarting();
}

// EFT routes keyboard commands independently from Unity's EventSystem. Consume them
// while the practice stack owns input (and through the frame that closes it).
internal static class PracticeCommands
{
    internal static bool Prefix(ref InputNode.ETranslateResult result)
    {
        if (!PracticeController.BlocksInput) return true;
        result = InputNode.ETranslateResult.BlockAll;
        return false;
    }
}

// ModulePatch discovers declared static methods, so every patch declares its prefix.
internal sealed class PracticeInventoryCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(InventoryScreen), "TranslateCommand");
    [PatchPrefix] private static bool Prefix(ref InputNode.ETranslateResult __result) => PracticeCommands.Prefix(ref __result);
}
internal sealed class PracticeMenuCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuScreen), "TranslateCommand");
    [PatchPrefix] private static bool Prefix(ref InputNode.ETranslateResult __result) => PracticeCommands.Prefix(ref __result);
}
internal sealed class PracticeTaskbarCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(MenuTaskBar), "TranslateCommand");
    [PatchPrefix] private static bool Prefix(ref InputNode.ETranslateResult __result) => PracticeCommands.Prefix(ref __result);
}
internal sealed class PracticeHideoutCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(HideoutPlayerOwner), "TranslateCommand");
    [PatchPrefix] private static bool Prefix(ref InputNode.ETranslateResult __result) => PracticeCommands.Prefix(ref __result);
}
