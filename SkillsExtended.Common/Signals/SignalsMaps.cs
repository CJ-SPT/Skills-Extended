using System;
using System.Collections.Generic;

namespace SkillsExtended.Signals;

public static class SignalsMaps
{
    // GameWorld/Fika use display-case IDs (e.g. Woods); SPT configuration uses woods.
    public static string Normalize(string map) => map?.Trim().ToLowerInvariant() ?? "";

    // Admit loaded raid maps, including modded maps, without manufacturing placements.
    // Factory's day/night variants share the exclusion.
    public static bool IsSupported(string map)
    {
        var normalized = Normalize(map);
        return normalized.Length > 0 && !normalized.StartsWith("factory", StringComparison.Ordinal);
    }

    public static IReadOnlyDictionary<string, string> StandardMaps { get; } = new Dictionary<string, string>
    {
        ["bigmap"] = "Customs", ["woods"] = "Woods", ["shoreline"] = "Shoreline",
        ["interchange"] = "Interchange", ["rezervbase"] = "Reserve", ["laboratory"] = "Labs",
        ["lighthouse"] = "Lighthouse", ["tarkovstreets"] = "Streets of Tarkov",
        ["sandbox"] = "Ground Zero", ["sandbox_high"] = "Ground Zero (level 21+)",
        ["labyrinth"] = "Labyrinth",
    };

    public static bool Same(string a, string b) =>
        !string.IsNullOrWhiteSpace(a) && Normalize(a) == Normalize(b);
}
