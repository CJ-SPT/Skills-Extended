using BepInEx.Configuration;
using UnityEngine;

namespace SkillsExtended.Config;

public static class ConfigManager
{
    private static int _lpOrder = 1000;
    
    public static ConfigEntry<KeyCode> LpMiniGameTurnKey;
    public static ConfigEntry<bool> LpMiniEnableHealthBar;
    
    public static void RegisterConfig(ConfigFile config)
    {
        LpMiniGameTurnKey = config.Bind(
            "LP Mini Game",
            "Turn Cylinder Key bind",
            KeyCode.A,
            new ConfigDescription(
                "Key to turn the cylinder", 
                null,
                new ConfigurationManagerAttributes
                {
                    Order = _lpOrder--
                }));
        
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