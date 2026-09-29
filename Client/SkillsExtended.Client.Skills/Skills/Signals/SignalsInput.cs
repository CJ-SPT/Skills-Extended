using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Signals;

public class SignalsInventoryActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(
            typeof(ItemUiContext),
            nameof(ItemUiContext.GetItemContextInteractions),
            new[] { typeof(ItemContext), typeof(Action) }
        );

    [PatchPostfix]
    private static void Postfix(ItemContext __0, ContextInteractions<EItemInfoButton> __result)
    {
        var itemContext = __0;
        if (
            !(
                SignalsRuntime.Instance?.Manifest?.Config?.Enabled
                ?? SkillsExtendedPlugin.SkillData?.SignalsIntelligence.Enabled
                ?? false
            )
        )
            return;
        if (
            itemContext?.Item?.TemplateId != SkillsExtended.Hacking.HackingIds.Pda
            || !SignalsRuntime.Instance
        )
            return;
        var field = AccessTools.Field(
            typeof(ContextInteractions<EItemInfoButton>),
            "_dynamicInteractions"
        );
        var actions = (Dictionary<string, DynamicContextInteraction>)field.GetValue(__result);
        if (actions == null)
        {
            actions = new Dictionary<string, DynamicContextInteraction>();
            field.SetValue(__result, actions);
        }
        actions["skills-signals"] = new DynamicContextInteraction(
            "skills-signals",
            "Open receiver",
            () => SignalsRuntime.Instance?.Open(),
            null
        );
    }
}

public class SignalsAxesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GamePlayerOwner), "TranslateAxes");

    [PatchPrefix]
    private static bool Prefix(GamePlayerOwner __instance, ref float[] __0)
    {
        var view = SignalsView.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
            return true;
        var moving = __0.Length >= 2 && (Math.Abs(__0[0]) > .01f || Math.Abs(__0[1]) > .01f);
        Array.Clear(__0, 0, __0.Length);
        if (moving)
            view.Close();
        return false;
    }
}

public class SignalsCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GamePlayerOwner), "TranslateCommand");

    [PatchPrefix]
    private static bool Prefix(
        GamePlayerOwner __instance,
        ECommand __0,
        ref InputNode.ETranslateResult __result
    )
    {
        var view = SignalsView.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
            return true;
        if (
            __0 == ECommand.Escape
            || __0 == ECommand.Jump
            || __0 == ECommand.ToggleSprinting
            || __0 == ECommand.ToggleDuck
            || __0 == ECommand.ToggleProne
        )
            view.Close();
        __result = InputNode.ETranslateResult.BlockAll;
        return false;
    }
}

public class SignalsCursorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(PlayerOwner), nameof(PlayerOwner.ShouldLockCursor));

    [PatchPostfix]
    private static void Postfix(PlayerOwner __instance, ref ECursorResult __result)
    {
        if (
            SignalsView.Current
            && SignalsView.Current.InRaid
            && __instance.Player?.IsYourPlayer == true
        )
            __result = ECursorResult.ShowCursor;
    }
}
