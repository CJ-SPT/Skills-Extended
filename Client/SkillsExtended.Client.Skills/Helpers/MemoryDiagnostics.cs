using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace SkillsExtended.Helpers;

// Seven passive samples per scope let a subsequent live run distinguish
// managed accumulation from native residency. No forced collections or snapshots.
internal sealed class MemoryDiagnostics
{
    private readonly string _scope;
    private float _next = float.PositiveInfinity, _end;

    internal MemoryDiagnostics(string scope) => _scope = scope;

    internal void Begin()
    {
        _next = Time.realtimeSinceStartup;
        _end = _next + 180;
    }

    internal void Stop() => _next = float.PositiveInfinity;

    internal void Tick()
    {
        var now = Time.realtimeSinceStartup;
        if (now < _next) return;
        _next = now >= _end ? float.PositiveInfinity : Math.Min(now + 30, _end);
        try
        {
            using var process = Process.GetCurrentProcess();
            SkillsExtendedPlugin.Log.LogInfo("Skills memory sample: scope=" + _scope + "; gc=" + GarbageCollector.GCMode
                + "; mono-used=" + MiB(Profiler.GetMonoUsedSizeLong())
                + "; mono-reserved=" + MiB(Profiler.GetMonoHeapSizeLong())
                + "; unity-allocated=" + MiB(Profiler.GetTotalAllocatedMemoryLong())
                + "; process-private=" + MiB(process.PrivateMemorySize64)
                + "; collections=" + GC.CollectionCount(0) + "/" + GC.CollectionCount(1) + "/" + GC.CollectionCount(2));
        }
        catch (Exception e)
        {
            Stop();
            SkillsExtendedPlugin.Log.LogWarning("Skills memory samples unavailable: " + e.Message);
        }
    }

    private static string MiB(long bytes) => bytes > 0 ? (bytes / (1024d * 1024)).ToString("F1",
        System.Globalization.CultureInfo.InvariantCulture) + " MiB" : "unavailable";
}
