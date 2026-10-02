using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Config;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Helpers;

// GameWorld owns this component for one running raid, including headless raids.
public sealed class RaidMemory : MonoBehaviour
{
    private static RaidMemory _current;
    private readonly MemoryDiagnostics _diagnostics = new("raid");

    private void Awake()
    {
        _current = this;
        AutomaticCollection.SetRaidActive(ConfigManager.AutomaticRaidCollection.Value);
        _diagnostics.Begin();
    }

    private void Update()
    {
        if (_current != this) return;
        AutomaticCollection.SetRaidActive(ConfigManager.AutomaticRaidCollection.Value);
        _diagnostics.Tick();
    }

    private void OnDestroy()
    {
        _diagnostics.Stop();
        if (_current != this) return;
        _current = null;
        AutomaticCollection.SetRaidActive(false);
    }
}

public sealed class RaidMemoryStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

    [PatchPostfix]
    private static void Postfix(GameWorld __instance)
    {
        if (__instance is not HideoutGameWorld && !__instance.GetComponent<RaidMemory>())
            __instance.gameObject.AddComponent<RaidMemory>();
    }
}
