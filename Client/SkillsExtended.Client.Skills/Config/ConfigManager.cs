using BepInEx.Configuration;
using UnityEngine;

namespace SkillsExtended.Config;

public static class ConfigManager
{
    private static int _lpOrder = 1000;
    public static ConfigEntry<KeyCode> LpMiniGameTurnKey;
    public static ConfigEntry<bool> LpMiniEnableHealthBar;
    public static ConfigEntry<int> HackingVolume;
    public static ConfigEntry<int> SignalsVolume;
    public static ConfigEntry<KeyboardShortcut> SignalsShortcut;

    public static void RegisterConfig(ConfigFile config)
    {
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
                "Key to turn the cylinder",
                null,
                new ConfigurationManagerAttributes { Order = _lpOrder-- }
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
