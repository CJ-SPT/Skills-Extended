using System;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using JsonType;
using SkillsExtended.Skills.Signals;
using SPT.Reflection.Patching;

namespace SkillsExtendedFika;

// HeadlessGame inherits AbstractGame, so it never calls BaseLocalGame.SpawnLoot.
internal sealed class SignalsHeadlessLootPatch : ModulePatch
{
    private static PropertyInfo _world = null!;

    protected override MethodBase GetTargetMethod()
    {
        var game = AccessTools.TypeByName("Fika.Headless.Classes.GameMode.HeadlessGame")
            ?? throw new InvalidOperationException("Fika HeadlessGame type is unavailable.");
        _world = AccessTools.Property(game, "GameWorld");
        var loadLoot = AccessTools.DeclaredMethod(game, "LoadLoot", new[] { typeof(LocationSettings.Location) });
        if (_world?.PropertyType != typeof(GameWorld) || loadLoot?.ReturnType != typeof(Task))
            throw new InvalidOperationException("Fika headless loot initialization boundary changed.");
        return loadLoot;
    }

    [PatchPostfix]
    private static void Postfix(object __instance, ref Task __result) =>
        __result = SignalsRuntime.CompleteLoot(__result, (GameWorld)_world.GetValue(__instance));
}
