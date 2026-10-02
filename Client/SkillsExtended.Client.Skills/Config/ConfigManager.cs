using BepInEx.Configuration;
using UnityEngine;

namespace SkillsExtended.Config;

public static class ConfigManager
{
    private static int _lpOrder = 1000;
    public static ConfigEntry<KeyCode> LpMiniGameTurnKey;
    public static ConfigEntry<bool> LpMiniEnableHealthBar;
    public static ConfigEntry<int> HackingVolume;
    public static ConfigEntry<int> LockPickingVolume;
    public static ConfigEntry<float> LockPickingSensitivity;
    public static ConfigEntry<bool> LockPickingReducedMotion;
    public static ConfigEntry<int> SignalsVolume;
    public static ConfigEntry<KeyboardShortcut> SignalsShortcut;
    public static ConfigEntry<bool> DeveloperEditorEnabled;
    public static ConfigEntry<KeyboardShortcut> DeveloperEditorShortcut;

    public static void RegisterConfig(ConfigFile config)
    {
        DeveloperEditorEnabled = config.Bind("Developer tools", "Enable developer editor", false,
            "Enable the in-game developer editor. Fika requires server authorization for your profile. AI and raid time continue while editing.");
        DeveloperEditorShortcut = config.Bind("Developer tools", "Open developer editor",
            new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl), "Toggle the in-game developer editor on a loaded map.");
        LockPickingVolume = config.Bind(
            "Lock Picking",
            "Volume (%)",
            80,
            new ConfigDescription(
                "Mechanical picking feedback volume.",
                new AcceptableValueRange<int>(0, 100)
            )
        );
        LockPickingSensitivity = config.Bind(
            "Lock Picking",
            "Mouse sensitivity",
            1f,
            new ConfigDescription(
                "Pick depth and lift sensitivity.",
                new AcceptableValueRange<float>(.2f, 3f)
            )
        );
        LockPickingReducedMotion = config.Bind(
            "Lock Picking",
            "Reduced motion",
            false,
            "Disable strain vibration; all mechanical feedback remains visible."
        );
        SignalsVolume = config.Bind(
            "Signals Intelligence",
            "Receiver volume (%)",
            70,
            new ConfigDescription(
                "Live receiver and search-area proximity beep volume. Visual readings work when muted.",
                new AcceptableValueRange<int>(0, 100)
            )
        );
        SignalsShortcut = config.Bind(
            "Signals Intelligence",
            "Open receiver",
            new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl),
            "Open the receiver while carrying a Modified PDA."
        );
        HackingVolume = config.Bind(
            "Hacking",
            "Hacking volume (%)",
            100,
            new ConfigDescription(
                "Volume of all PDA hacking sounds, including feedback, ambience and alerts. "
                    + "0 mutes them; 100 keeps the original volume. Applies immediately in practice and raids, alongside the game's volume settings.",
                new AcceptableValueRange<int>(0, 100),
                new ConfigurationManagerAttributes
                {
                    Order = 1000,
                    Category = "Hacking",
                    IsAdvanced = false,
                    ShowRangeAsPercent = false,
                }
            )
        );
        LpMiniGameTurnKey = config.Bind(
            "LP Mini Game",
            "Turn Cylinder Key bind",
            KeyCode.A,
            new ConfigDescription(
                "Hold to apply tension while working pins. Release to reset the pins.",
                null,
                new ConfigurationManagerAttributes
                {
                    Order = _lpOrder--,
                    Category = "Lock Picking",
                    DispName = "Tension key",
                }
            )
        );
        /*
        LpMiniEnableHealthBar = config.Bind(
            "LP Mini Game",
            "Mini-game health bar",
            true,
            new ConfigDescription(
                "Enable or disable the health bar",
                null,
                new ConfigurationManagerAttributes
                {
                    Order =  _lpOrder--
                }));
        */
    }
}
