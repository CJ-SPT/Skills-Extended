using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Newtonsoft.Json;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SPT.Common.Http;
using System;
using System.Linq;
using SkillsExtended.Config;
using SkillsExtended.Skills.LockPicking;
using UnityEngine;

namespace SkillsExtended;

[BepInPlugin("com.cj.SkillsExtended", "Skills Extended", "2.0.1")]

// Because I need the idle state type from it for lockpicking
[BepInDependency("com.boogle.oldtarkovmovement", BepInDependency.DependencyFlags.SoftDependency)] 
public class SkillsPlugin : BaseUnityPlugin
{
    public const int TarkovVersion = 40087;
    
    // Contains key information
    public static KeysResponse Keys;

    // Contains skill data
    public static SkillDataResponse SkillData;
    
    private static GameObject _hook;
    
    internal static ManualLogSource Log;
    
    internal static bool IsOldTarkovMovementDetected { get; private set; }
    
    private void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
        {
            throw new Exception("Invalid EFT Version");
        }
        
        Log = Logger;
        
        PatchManager.PatchAll();
        ConfigManager.RegisterConfig(Config);
        
#if DEBUG
        Logger.LogWarning("PRE RELEASE BUILD - NO SUPPORT");
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        _hook = new GameObject("Skills Controller Object");
        DontDestroyOnLoad(_hook);
        
        // Compatibility for lockpicking
        if (Chainloader.PluginInfos.Keys.Contains("com.boogle.oldtarkovmovement"))
        {
            RE.GetOldMovementTypes();
            IsOldTarkovMovementDetected = true;
            Logger.LogInfo("Enabling compatibility for old tarkov movement");
        }
    }

    private void Start()
    {
        Keys = Utils.Get<KeysResponse>("/skillsExtended/GetKeys");
        SkillData = Utils.Get<SkillDataResponse>("/skillsExtended/GetSkillsConfig");
        
        LockPickingHelpers.LoadMiniGame();
    }
}