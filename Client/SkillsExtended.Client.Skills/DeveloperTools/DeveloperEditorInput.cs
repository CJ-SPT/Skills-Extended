using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.DeveloperTools;

public sealed class DeveloperEditorAxesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GamePlayerOwner), "TranslateAxes");
    [PatchPrefix]
    private static bool Prefix(GamePlayerOwner __instance, float[] __0)
    {
        if (SkillsDeveloperEditor.Current?.IsOpen != true || __instance.Player?.IsYourPlayer != true) return true;
        Array.Clear(__0, 0, __0.Length); return false;
    }
}
public sealed class DeveloperEditorCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GamePlayerOwner), "TranslateCommand");
    [PatchPrefix]
    private static bool Prefix(GamePlayerOwner __instance, ref InputNode.ETranslateResult __result)
    {
        if (SkillsDeveloperEditor.Current?.IsOpen != true || __instance.Player?.IsYourPlayer != true) return true;
        __result = InputNode.ETranslateResult.BlockAll; return false;
    }
}
public sealed class DeveloperEditorCursorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(PlayerOwner), nameof(PlayerOwner.ShouldLockCursor));
    [PatchPostfix]
    private static void Postfix(PlayerOwner __instance, ref ECursorResult __result)
    {
        if (SkillsDeveloperEditor.Current?.IsOpen == true && !SkillsDeveloperEditor.Current.Looking
            && __instance.Player?.IsYourPlayer == true) __result = ECursorResult.ShowCursor;
    }
}

public sealed class DeveloperEditorConsoleCommands
{
    [EFT.Console.Core.ConsoleCommand("skills_editor", "", "Open the shared developer editor (enable Developer tools first)")]
    public static void Open() => SkillsDeveloperEditor.ToggleCurrent();
}

public sealed class DeveloperEditorInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.InitLevel));
    [PatchPostfix]
    private static void Postfix(GameWorld __instance, ref Task __result) => __result = Complete(__result, __instance);
    private static async Task Complete(Task pending, GameWorld world)
    {
        await pending;
        if (!world.GetComponent<SkillsDeveloperEditor>()) world.gameObject.AddComponent<SkillsDeveloperEditor>();
    }
}
