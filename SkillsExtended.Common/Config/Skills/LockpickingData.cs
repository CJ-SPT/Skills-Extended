using System.Collections.Generic;

namespace SkillsExtended.Config.Skills;

using System;
using System.Linq;

public class LockPickingData
{
    public bool Enabled { get; set; }
    public float PickStrengthBase { get; set; }
    public float PickStrengthPerLevel { get; set; }
    public float SweetSpotRangeBase { get; set; }
    public float SweetSpotRangePerLevel { get; set; }
    public int AttemptsBeforeBreak { get; set; }
    public float InspectLockXpRatio { get; set; }
    public float FailureLockXpRatio { get; set; }
    public Dictionary<string, float> XpTable { get; set; }
    public DoorPickLevels DoorPickLevels { get; set; }
    public float PinTolerancePerLevel { get; set; } = .5f;
    public float PickResiliencePerLevel { get; set; } = .75f;
    public float ExpertControlElite { get; set; } = 10;
    public float PickWearSeconds { get; set; } = 3;
    public List<LockPickingTier> Tiers { get; set; } =
        new()
        {
            new()
            {
                Level = 1,
                Pins = 3,
                Tolerance = .096f,
                StrainWarningSeconds = .65f,
            },
            new()
            {
                Level = 2,
                Pins = 3,
                Tolerance = .08f,
                StrainWarningSeconds = .55f,
            },
            new()
            {
                Level = 3,
                Pins = 4,
                Tolerance = .068f,
                StrainWarningSeconds = .45f,
            },
            new()
            {
                Level = 4,
                Pins = 4,
                Tolerance = .056f,
                StrainWarningSeconds = .35f,
            },
            new()
            {
                Level = 5,
                Pins = 5,
                Tolerance = .044f,
                StrainWarningSeconds = .3f,
            },
        };

    public LockPickingTier Tier(int level) =>
        Tiers.Single(t => t.Level == Math.Max(1, Math.Min(5, level)));

    public void Validate()
    {
        if (
            !Range(PinTolerancePerLevel, 0, 3)
            || !Range(PickResiliencePerLevel, 0, 5)
            || !Range(ExpertControlElite, 0, 50)
            || !Range(PickWearSeconds, .5f, 30)
            || !Range(InspectLockXpRatio, 0, 1)
            || !Range(FailureLockXpRatio, 0, 1)
            || Tiers == null
            || Tiers.Count != 5
            || Tiers.Any(t => t == null)
            || Tiers.Select(t => t.Level).Distinct().Count() != 5
            || Tiers.Any(t =>
                t.Level < 1
                || t.Level > 5
                || t.Pins < 3
                || t.Pins > 5
                || !Range(t.Tolerance, .02f, .2f)
                || !Range(t.StrainWarningSeconds, .1f, 2)
            )
        )
            throw new ArgumentException(
                "Lock Picking: invalid pin tiers, control bonuses, wear, or XP ratios."
            );
    }

    private static bool Range(float v, float min, float max) =>
        !float.IsNaN(v) && !float.IsInfinity(v) && v >= min && v <= max;
}

public class LockPickingTier
{
    public int Level { get; set; }
    public int Pins { get; set; }
    public float Tolerance { get; set; }
    public float StrainWarningSeconds { get; set; }
}

// DoorId : level to pick the lock
public class DoorPickLevels
{
    public Dictionary<string, int> Factory { get; set; }
    public Dictionary<string, int> Woods { get; set; }
    public Dictionary<string, int> Customs { get; set; }
    public Dictionary<string, int> Interchange { get; set; }
    public Dictionary<string, int> Reserve { get; set; }
    public Dictionary<string, int> Shoreline { get; set; }
    public Dictionary<string, int> Labs { get; set; }
    public Dictionary<string, int> Lighthouse { get; set; }
    public Dictionary<string, int> Streets { get; set; }
    public Dictionary<string, int> GroundZero { get; set; }
    public Dictionary<string, int> Labyrinth { get; set; }
}
