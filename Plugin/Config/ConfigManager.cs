using BepInEx.Configuration;
using UnityEngine;

namespace SkillsExtended.Config;

public static class ConfigManager
{
    private static int _lpOrder = 1000;
    
    public static ConfigEntry<KeyCode> LpMiniGameTurnKey;
    public static ConfigEntry<float> LpMiniGameVolume;
    public static ConfigEntry<bool> LpMiniEnableHealthBar;
    public static ConfigEntry<bool> LpMiniEnablePickLossOnExit;
    
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
        
        LpMiniGameVolume = config.Bind(
            "LP Mini Game",
            "Mini-game volume",
            1f,
            new ConfigDescription(
                "LP Mini game volume", 
                new AcceptableValueRange<float>(0f, 1f), 
                new ConfigurationManagerAttributes
                {
                    ShowRangeAsPercent = true, 
                    Order =  _lpOrder--
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
        
        LpMiniEnablePickLossOnExit = config.Bind(
            "LP Mini Game",
            "Enable pick loss on exit",
            false,
            new ConfigDescription(
                "Enables the ability to lose a pick duration when exiting the mini-game by pressing a key", 
                null, 
                new ConfigurationManagerAttributes
                {
                    Order =  _lpOrder--
                }));
    }
}