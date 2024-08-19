using BepInEx.Configuration;
using UnityEngine;

namespace SkillsExtended.Config;

public static class ConfigManager
{
    public static ConfigEntry<KeyCode> LpMiniGameTurnKey;

    public static void RegisterConfig(ConfigFile config)
    {
        LpMiniGameTurnKey = config.Bind(
            "LP Mini Game",
            "Turn Cylinder Key bind",
            KeyCode.A,
            "Key to turn the cylinder");
    }
}