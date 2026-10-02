namespace UnityEngine.Scripting;

// Only the enum is needed to exercise the production collection policy offline.
internal static class GarbageCollector
{
    internal enum Mode { Disabled = 0, Enabled = 1, Manual = 2 }
}
