using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SkillsExtended.Skills.Signals;

/// <summary>Remember delivered raid totals across client reconnects and process restarts.</summary>
internal static class SignalsXpLedger
{
    private static Dictionary<string, float> _totals;
    private static string FilePath =>
        Path.Combine(BepInEx.Paths.ConfigPath, "skills-extended-signals-xp.json");

    public static float Reserve(string key, float total)
    {
        if (_totals == null)
        {
            _totals = File.Exists(FilePath)
                ? JsonConvert.DeserializeObject<Dictionary<string, float>>(
                    File.ReadAllText(FilePath)
                )
                : new Dictionary<string, float>();
            if (
                _totals == null
                || _totals.Any(p =>
                    !SkillsExtended.Signals.SignalPoint.Finite(p.Value) || p.Value < 0
                )
            )
                throw new InvalidDataException("Signal XP receipt file is invalid.");
        }
        _totals.TryGetValue(key, out var previous);
        if (total <= previous)
            return 0;
        var next = new Dictionary<string, float>(_totals) { [key] = total };
        while (next.Count > 256)
            next.Remove(next.Keys.First());
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonConvert.SerializeObject(next));
            if (File.Exists(FilePath))
                File.Replace(temporary, FilePath, null);
            else
                File.Move(temporary, FilePath);
            _totals = next;
            return total - previous;
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
