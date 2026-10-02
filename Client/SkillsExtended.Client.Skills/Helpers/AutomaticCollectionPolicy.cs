using UnityEngine.Scripting;

namespace SkillsExtended.Helpers;

// Retain the latest game request until both raid and editor ownership end.
internal sealed class AutomaticCollectionPolicy
{
    internal bool Active { get; private set; }
    private GarbageCollector.Mode _restore;

    internal GarbageCollector.Mode? Transition(bool raidActive, bool editorActive, GarbageCollector.Mode current)
    {
        var active = raidActive || editorActive;
        if (active == Active) return null;
        Active = active;
        if (!active) return _restore;
        _restore = current;
        return GarbageCollector.Mode.Enabled;
    }

    internal GarbageCollector.Mode Filter(GarbageCollector.Mode requested)
    {
        if (!Active) return requested;
        _restore = requested;
        return GarbageCollector.Mode.Enabled;
    }
}
