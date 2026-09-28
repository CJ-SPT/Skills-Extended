using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Config.Skills;

public class ElectronicsData
{
    public bool Enabled { get; set; } = true;
    public int AttemptsPerDoor { get; set; } = 3;
    public int BaseCoherence { get; set; } = 60;
    public int CoherencePerLevel { get; set; } = 1;
    public int BaseStrength { get; set; } = 20;
    public int LevelsPerStrength { get; set; } = 5;
    public int UtilitySlots { get; set; } = 3;
    public int EliteUtilitySlots { get; set; } = 4;
    public float FailureXpRatio { get; set; } = 0.2f;
    public int PdaReferenceValue { get; set; } = 150000;
    public int DefaultDifficulty { get; set; } = 2;
    public Dictionary<string, int> DoorDifficulties { get; set; } = new(); // map/door ID
    public Dictionary<string, int> KeycardDifficulties { get; set; } =
        new()
        {
            ["5c1d0d6d86f7744bb2683e1f"] = 1,
            ["5c1d0c5f86f7744bb2683cf0"] = 3,
            ["5c1d0dc586f7744baf2e7b79"] = 3,
            ["5c1d0efb86f7744baf2e7b7b"] = 3,
            ["5c1d0f4986f7744bb01837fa"] = 3,
            ["5c1e495a86f7743109743dfb"] = 3,
        };
    public List<string> ExcludedDoors { get; set; } = new();
    public List<string> ExcludedKeycards { get; set; } =
        new()
        {
            "5c94bbff86f7747ee735c08f", // Labs entry
            "679b9819a2f2dd4da9023512", // Labyrinth entry
            "5e42c81886f7742a01529f57", // scripted saferoom extraction
        };
    public List<HackingTier> Tiers { get; set; } =
        new()
        {
            new()
            {
                Level = 1,
                Name = "Standard",
                Nodes = 19,
                Defenses = 3,
                Utilities = 2,
                Caches = 1,
                CoreCoherence = 40,
                CoreStrength = 10,
                SuccessXp = 10,
            },
            new()
            {
                Level = 2,
                Name = "Secure",
                Nodes = 31,
                Defenses = 6,
                Utilities = 3,
                Caches = 2,
                CoreCoherence = 70,
                CoreStrength = 15,
                SuccessXp = 15,
            },
            new()
            {
                Level = 3,
                Name = "Hardened",
                Nodes = 43,
                Defenses = 9,
                Utilities = 4,
                Caches = 2,
                CoreCoherence = 100,
                CoreStrength = 20,
                SuccessXp = 20,
            },
        };

    public int Difficulty(string map, string door, string key) =>
        DoorDifficulties.TryGetValue(map + "/" + door, out var d) ? d
        : KeycardDifficulties.TryGetValue(key ?? "", out d) ? d
        : DefaultDifficulty;

    public bool Excluded(string map, string door, string key) =>
        ExcludedDoors.Contains(map + "/" + door) || ExcludedKeycards.Contains(key ?? "");

    public HackingTier Tier(int level) => Tiers.Single(t => t.Level == level);

    public void Validate()
    {
        if (
            AttemptsPerDoor < 1
            || AttemptsPerDoor > 10
            || BaseCoherence < 1
            || BaseCoherence > 1000
            || CoherencePerLevel < 0
            || CoherencePerLevel > 20
            || BaseStrength < 1
            || BaseStrength > 1000
            || LevelsPerStrength < 1
            || LevelsPerStrength > 100
            || UtilitySlots < 1
            || UtilitySlots > 4
            || EliteUtilitySlots < UtilitySlots
            || EliteUtilitySlots > 4
            || PdaReferenceValue < 0
            || float.IsNaN(FailureXpRatio)
            || FailureXpRatio < 0
            || FailureXpRatio > 1
            || DefaultDifficulty < 1
            || DefaultDifficulty > 3
        )
        {
            throw new ArgumentException(
                "Electronics: invalid stat, slot, attempt, difficulty or XP setting."
            );
        }

        if (
            DoorDifficulties == null
            || KeycardDifficulties == null
            || ExcludedDoors == null
            || ExcludedKeycards == null
            || DoorDifficulties
                .Concat(KeycardDifficulties)
                .Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Value < 1 || p.Value > 3)
            || ExcludedDoors.Concat(ExcludedKeycards).Any(string.IsNullOrWhiteSpace)
            || Tiers == null
            || Tiers.Count != 3
            || Tiers.Select(t => t.Level).Distinct().Count() != 3
        )
        {
            throw new ArgumentException("Electronics: invalid door rules or difficulty tiers.");
        }

        foreach (var t in Tiers)
        {
            if (
                t.Level < 1
                || t.Level > 3
                || string.IsNullOrWhiteSpace(t.Name)
                || t.Nodes < 15
                || t.Nodes > 61
                || t.Defenses < 0
                || t.Utilities < 1
                || t.Caches < 0
                || t.Defenses + t.Utilities + t.Caches > t.Nodes - 8
                || t.CoreCoherence < 1
                || t.CoreCoherence > 10000
                || t.CoreStrength < 0
                || t.CoreStrength > 1000
                || float.IsNaN(t.SuccessXp)
                || float.IsInfinity(t.SuccessXp)
                || t.SuccessXp < 0
                || t.SuccessXp > 1000
            )
            {
                throw new ArgumentException("Electronics: invalid board settings.");
            }
        }
    }
}

public class HackingTier
{
    public int Level { get; set; }
    public string Name { get; set; }
    public int Nodes { get; set; }
    public int Defenses { get; set; }
    public int Utilities { get; set; }
    public int Caches { get; set; }
    public int CoreCoherence { get; set; }
    public int CoreStrength { get; set; }
    public float SuccessXp { get; set; }
}
