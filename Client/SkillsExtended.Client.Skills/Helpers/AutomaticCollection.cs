using HarmonyLib;
using UnityEngine.Scripting;

namespace SkillsExtended.Helpers;

internal static class AutomaticCollection
{
    private static readonly AutomaticCollectionPolicy Policy = new();
    private static bool _applying;
    private static bool _raidActive, _editorActive;

    internal static void Enable()
    {
        var harmony = new Harmony("com.cj.skillsextended.memory");
        harmony.Patch(AccessTools.PropertySetter(typeof(GarbageCollector), nameof(GarbageCollector.GCMode)),
            prefix: new HarmonyMethod(typeof(AutomaticCollection), nameof(BeforeMode)));
        // The native wrapper can skip Unity's setter when the requested mode is
        // already current. Preserve that request too, especially on raid teardown.
        harmony.Patch(AccessTools.PropertySetter(typeof(InGameMemoryManagement), nameof(InGameMemoryManagement.GCEnabled)),
            prefix: new HarmonyMethod(typeof(AutomaticCollection), nameof(BeforeNativeMode)));
    }

    private static void BeforeNativeMode(bool value)
    {
        if (!_applying) Policy.Filter(value ? GarbageCollector.Mode.Enabled : GarbageCollector.Mode.Disabled);
    }

    private static void BeforeMode(ref GarbageCollector.Mode value)
    {
        if (!_applying) value = Policy.Filter(value);
    }

    internal static void SetRaidActive(bool active)
    {
        if (_raidActive == active) return;
        _raidActive = active;
        Apply();
    }

    internal static void SetEditorActive(bool active)
    {
        if (_editorActive == active) return;
        _editorActive = active;
        Apply();
    }

    private static void Apply()
    {
        var mode = Policy.Transition(_raidActive, _editorActive, GarbageCollector.GCMode);
        if (!mode.HasValue) return;
        _applying = true;
        try
        {
            GarbageCollector.GCMode = mode.Value;
            SkillsExtendedPlugin.Log.LogInfo(Policy.Active
                ? "Skills memory: automatic garbage collection enabled."
                : "Skills memory: restored requested garbage collection mode " + mode.Value + ".");
        }
        finally { _applying = false; }
    }
}
