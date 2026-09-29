using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Signals;

namespace SkillsExtended.Config.Skills;

public class SignalsIntelligenceData
{
    public bool Enabled { get; set; } = true;
    public float ReadingSeconds { get; set; } = 3;
    public float PairingSeconds { get; set; } = 5;
    public float MinimumSeparation { get; set; } = 125;
    public float BaseUncertainty { get; set; } = 12;
    public float MinimumUncertainty { get; set; } = 4;
    public float TuningTolerance { get; set; } = .2f;
    public float TuningBonus { get; set; } = .5f;
    public float BearingXp { get; set; } = 3;
    public float CompletionXp { get; set; } = 12;
    public int MinimumLootValue { get; set; } = 250000;
    public int MaximumLootValue { get; set; } = 500000;
    public List<SignalPlacement> Placements { get; set; } = SignalsDefaults.Placements();
    public List<SignalLootEntry> Loot { get; set; } = SignalsDefaults.Loot();

    public void Validate()
    {
        foreach (
            var property in GetType().GetProperties().Where(p => p.PropertyType == typeof(float))
        )
        {
            var n = (float)property.GetValue(this);
            if (!SignalPoint.Finite(n) || n < 0)
                throw new ArgumentException(
                    "Signals Intelligence: " + property.Name + " must be finite and nonnegative."
                );
        }
        if (
            ReadingSeconds < 1
            || PairingSeconds < 1
            || MinimumSeparation < 20
            || MinimumSeparation > 500
            || MinimumUncertainty < 1
            || BaseUncertainty > 30
            || MinimumUncertainty > BaseUncertainty
            || TuningTolerance <= 0
            || TuningTolerance > 2
            || TuningBonus > 2
            || MinimumLootValue < 1
            || MaximumLootValue < MinimumLootValue
            || MaximumLootValue > 5000000
        )
            throw new ArgumentException(
                "Signals Intelligence: invalid timing, precision, separation, tuning, or loot limits."
            );
        if (Placements == null || Loot == null || Placements.Count > 500 || Loot.Count > 500)
            throw new ArgumentException("Signals Intelligence: invalid content tables.");
        if (
            Placements.Any(p =>
                p == null
                || string.IsNullOrWhiteSpace(p.Id)
                || p.Position == null
                || !p.Position.IsFinite
                || !SignalPoint.Finite(p.Yaw)
                || (p.Map != "bigmap" && p.Map != "woods")
            )
            || Placements.Select(p => p.Id).Distinct().Count() != Placements.Count
        )
            throw new ArgumentException(
                "Signals Intelligence: placements need unique IDs, a supported map, and finite coordinates."
            );
        if (
            Loot.Any(l =>
                l == null
                || l.Template == null
                || l.Template.Length != 24
                || !l.Template.All(Uri.IsHexDigit)
                || l.Weight < 1
                || l.Weight > 1000
                || string.IsNullOrWhiteSpace(l.Theme)
            )
        )
            throw new ArgumentException(
                "Signals Intelligence: invalid loot template, theme, or weight."
            );
    }
}
