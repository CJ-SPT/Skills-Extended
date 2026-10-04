using EFT;
using EFT.UI;
using SkillsExtended.Config;
using SkillsExtended.Skills.Hacking;
using SkillsExtended.Skills.Practice;
using UnityEngine;
using UnityEngine.EventSystems;

internal static class PracticeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var config = new SkillsConfig
        {
            LockPicking = new() { Enabled = true },
            Hacking = new() { Enabled = true },
            SignalsIntelligence = new() { Enabled = true },
        };
        check(PracticeSettings.ForSkill(ESkillId.Lockpicking) == PracticeGame.LockPicking
            && PracticeSettings.ForSkill((ESkillId)200) == PracticeGame.Hacking
            && PracticeSettings.ForSkill((ESkillId)201) == PracticeGame.Signals
            && PracticeSettings.ForSkill(ESkillId.Endurance) == PracticeGame.None, "Only the three practice skills map to games");
        foreach (var game in new[] { PracticeGame.LockPicking, PracticeGame.Hacking, PracticeGame.Signals })
        {
            foreach (var level in new[] { 0, 25, 51 })
            {
                var settings = new PracticeSettings(game, level);
                check(settings.Level == level && settings.Difficulty == 1 && !settings.CustomizeLevel,
                    "Fresh setup uses displayed level and introductory difficulty");
                settings.SetLevel(99);
                check(settings.Level == level, "Changing a stored override does not enable it");
                settings.CustomizeLevel = true;
                check(settings.Level == 51, "Simulated level cannot exceed 51");
                settings.SetLevel(-10);
                check(settings.Level == 0, "Simulated level cannot be negative");
                settings.CustomizeLevel = false;
                check(settings.Level == level && settings.DisplayedLevel == level, "Disabling customization restores displayed level");
                settings.SetDifficulty(99);
                check(settings.Difficulty == (game == PracticeGame.LockPicking ? 5 : game == PracticeGame.Hacking ? 3 : 1),
                    "Difficulty respects each existing game's bounds");
                settings.SetDifficulty(-1);
                check(settings.Difficulty == 1, "Difficulty starts at one");
            }
            check(PracticeSettings.Available(game, config, false, false, false, false), "Menu practice is available");
            check(PracticeSettings.Available(game, config, false, false, true, true), "Hideout practice is available");
            check(!PracticeSettings.Available(game, config, false, false, true, false), "Raid practice is blocked even before a player exists");
            check(!PracticeSettings.Available(game, config, true, false, false, false), "Locked skills are unavailable");
            check(!PracticeSettings.Available(game, config, false, true, false, false), "Headless has no practice UI");
            check(!PracticeSettings.Available(game, null, false, false, false, false), "Unloaded configuration is unavailable");
        }
        config.Hacking.Enabled = config.LockPicking.Enabled = config.SignalsIntelligence.Enabled = false;
        foreach (var game in Enum.GetValues<PracticeGame>())
            check(!PracticeSettings.Available(game, config, false, false, false, false), "Disabled skills and unrelated skills have no launch");

        var session = new PracticeSession();
        var launches = 0;
        var closes = 0;
        var alive = false;
        void Launch() { launches++; alive = true; }
        void Close() { closes++; alive = false; }
        check(!session.Start(false, false, Launch, () => alive, Close)
            && !session.Start(true, true, Launch, () => alive, Close) && launches == 0,
            "Availability and overlap are rechecked before calling any game");
        check(session.Start(true, false, Launch, () => alive, Close), "A valid launch owns one game");
        check(!session.Start(true, false, Launch, () => alive, Close) && launches == 1, "Double-clicks cannot launch twice");
        check(!session.Returned(), "Setup stays hidden while its game is alive");
        alive = false;
        check(session.Returned() && !session.Returned() && closes == 0, "Normal game close returns to setup once");
        check(session.Start(true, false, Launch, () => alive, Close), "A returned setup can start a fresh practice");
        session.Close(); session.Close();
        check(closes == 1 && !alive && !session.Running, "Scene/screen teardown closes its game exactly once");
        check(!session.Start(true, false, Launch, () => alive, Close), "A disposed setup cannot relaunch");

        session = new PracticeSession();
        check(!session.Start(true, false, () => { }, () => false, Close) && !session.Running,
            "Silent asset/guard failures return setup to an idle state");
        try { session.Start(true, false, () => { alive = true; throw new InvalidOperationException(); }, () => alive, Close); }
        catch (InvalidOperationException) { }
        check(!session.Running && !alive, "Partial launch exceptions clean up the game");
        check(session.Start(true, false, Launch, () => alive, Close), "Setup remains usable after launch failure");
        session.Close();

        // Execute the production UI state helper as a nested setup/game stack.
        var ui = UIEventSystem.Instance = new();
        var selected = new GameObject();
        ui.Events.SetSelectedGameObject(selected);
        ui.Events.sendNavigationEvents = true;
        ui.Events.enabled = ui.Module.enabled = true;
        EventSystem.current = ui.Events;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        var setupInput = new HackingUiInputState();
        var gameInput = new HackingUiInputState();
        setupInput.Capture();
        gameInput.Capture();
        gameInput.Maintain(true);
        gameInput.Restore();
        check(Cursor.visible && !ui.Events.sendNavigationEvents && ui.Events.currentSelectedGameObject == null,
            "Game close/failure restores the setup input layer without leaking navigation");
        setupInput.Restore();
        check(Cursor.visible && ui.Events.sendNavigationEvents && ui.Events.currentSelectedGameObject == selected,
            "Final setup close restores original Skills-screen focus and navigation");
    }
}
