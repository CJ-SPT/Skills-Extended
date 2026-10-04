using System;
using EFT;
using SkillsExtended.Config;
using SkillsExtended.Hacking;
using SkillsExtended.Signals;

namespace SkillsExtended.Skills.Practice;

internal enum PracticeGame { None, LockPicking, Hacking, Signals }

/// <summary>Session-local settings; never writes to the displayed skill or profile.</summary>
internal sealed class PracticeSettings
{
    public PracticeGame Game { get; }
    public int DisplayedLevel { get; }
    public bool CustomizeLevel { get; set; }
    public bool ShowCoaching { get; set; } = true;
    public int CustomLevel { get; private set; }
    public int Level => CustomizeLevel ? CustomLevel : DisplayedLevel;
    public int Difficulty { get; private set; } = 1;
    public int MaxDifficulty => Game == PracticeGame.LockPicking ? 5 : Game == PracticeGame.Hacking ? 3 : 1;
    public string Title => Game == PracticeGame.LockPicking ? "Lock Picking" : Game == PracticeGame.Hacking ? "Hacking" : "Signals";

    public PracticeSettings(PracticeGame game, int displayedLevel)
    {
        Game = game;
        DisplayedLevel = CustomLevel = Math.Clamp(displayedLevel, 0, 51);
    }

    public void SetDifficulty(int value) => Difficulty = Math.Clamp(value, 1, MaxDifficulty);
    public void SetLevel(int value) => CustomLevel = Math.Clamp(value, 0, 51);

    public static PracticeGame ForSkill(ESkillId id) => id switch
    {
        ESkillId.Lockpicking => PracticeGame.LockPicking,
        (ESkillId)HackingIds.Skill => PracticeGame.Hacking,
        (ESkillId)SignalsIds.Skill => PracticeGame.Signals,
        _ => PracticeGame.None,
    };

    public static bool Available(PracticeGame game, SkillsConfig config, bool locked,
        bool headless, bool worldLoaded, bool hideout) => !locked && !headless
        && (!worldLoaded || hideout) && (game switch
        {
            PracticeGame.LockPicking => config?.LockPicking?.Enabled == true,
            PracticeGame.Hacking => config?.Hacking?.Enabled == true,
            PracticeGame.Signals => config?.SignalsIntelligence?.Enabled == true,
            _ => false,
        });
}
