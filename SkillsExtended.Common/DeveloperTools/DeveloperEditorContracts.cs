using System;
using System.Collections.Generic;
using SkillsExtended.Config.Skills;
using SkillsExtended.Signals;

namespace SkillsExtended.DeveloperTools;

public class DeveloperEditorRequest
{
    public string Map { get; set; } = "";
    public string Raid { get; set; } = "";
    public string Revision { get; set; } = "";
}
public class DeveloperEditorReply
{
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public string Map { get; set; } = "";
    public string Raid { get; set; } = "";
    public string Revision { get; set; } = "";
    public bool Success => Status == "success";
}
public static class DeveloperEditorPolicy
{
    public static bool Eligible(bool enabled, bool loadedMap, bool alivePmc,
        bool headless, bool fika, bool soloHost) =>
        enabled && loadedMap && alivePmc && !headless && (!fika || soloHost);
    public static bool Accepts(string ownerRaid, string ownerMap, DeveloperEditorRequest request) =>
        request != null && !string.IsNullOrEmpty(ownerRaid) && ownerRaid == request.Raid
        && !string.IsNullOrWhiteSpace(ownerMap) && SignalsMaps.Same(ownerMap, request.Map);
}

public static class DoorRuleMaps
{
    public static Dictionary<string, int> Locks(DoorPickLevels maps, string map) =>
        SignalsMaps.Normalize(map) switch
        {
            "factory4_day" or "factory4_night" => maps?.Factory,
            "woods" => maps?.Woods,
            "bigmap" => maps?.Customs,
            "interchange" => maps?.Interchange,
            "rezervbase" => maps?.Reserve,
            "shoreline" => maps?.Shoreline,
            "laboratory" => maps?.Labs,
            "lighthouse" => maps?.Lighthouse,
            "tarkovstreets" => maps?.Streets,
            "sandbox" or "sandbox_high" => maps?.GroundZero,
            "labyrinth" => maps?.Labyrinth,
            _ => null,
        };
    public static bool OnMap(string key, string map) => key != null && key.Contains("/")
        && SignalsMaps.Same(key.Substring(0, key.IndexOf('/')), map);
    public static string Door(string key) => key.Substring(key.IndexOf('/') + 1);
    public static bool TryDifficulty(Dictionary<string, int> rules, string map, string door, out int value)
    {
        if (rules.TryGetValue(map + "/" + door, out value)) return true;
        foreach (var rule in rules)
            if (OnMap(rule.Key, map) && Door(rule.Key) == door) { value = rule.Value; return true; }
        value = 0; return false;
    }
}

public class DoorAuthoringRequest : DeveloperEditorRequest
{
    public Dictionary<string, int> HackingDifficulties { get; set; } = new();
    public List<string> ExcludedHackingDoors { get; set; } = new();
    public Dictionary<string, int> LockLevels { get; set; } = new();
}
public sealed class DoorAuthoringReply : DeveloperEditorReply
{
    public DoorAuthoringRequest Rules { get; set; } = new();
    public HackingData Hacking { get; set; } = new();
    public bool LockPickingEnabled { get; set; }
    public bool LockMapSupported { get; set; }
    public List<int> MissingXpLevels { get; set; } = new();
}
