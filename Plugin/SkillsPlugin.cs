﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SPT.Common.Http;
using System;
using System.Linq;
using Comfort.Common;
using EFT.UI;
using IcyClawz.CustomInteractions;
using SkillsExtended.Config;
using SkillsExtended.ItemInteractions;
using SkillsExtended.Skills.LockPicking;
using UnityEngine;

namespace SkillsExtended;

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.4.2")]
[BepInDependency("com.IcyClawz.CustomInteractions")]
[BepInDependency("com.dirtbikercj.QuestsExtended")]

// Because I need the idle state type from it for lockpicking
[BepInDependency("com.boogle.oldtarkovmovement", BepInDependency.DependencyFlags.SoftDependency)] 
public class SkillsPlugin : BaseUnityPlugin
{
    public const int TarkovVersion = 33420;
    
    // Contains key information
    public static KeysResponse Keys;

    // Contains skill data
    public static SkillDataResponse SkillData;
    
    public static RealismConfig RealismConfig;

    private static GameObject _hook;
    
    internal static BuffController BuffController;
    
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
        CustomInteractionsManager.Register(new CustomInteractionsProvider());
        ConfigManager.RegisterConfig(Config);
        
#if DEBUG
        Logger.LogWarning("PRE RELEASE BUILD - NO SUPPORT");
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        _hook = new GameObject("Skills Controller Object");
        DontDestroyOnLoad(_hook);
        
        BuffController = _hook.AddComponent<BuffController>();

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
        
        // If realism is installed, load its config
        if (Chainloader.PluginInfos.ContainsKey("RealismMod"))
        {
            var jsonString = RequestHandler.GetJson("/RealismMod/GetInfo");
            RealismConfig = JsonConvert.DeserializeObject<RealismConfig>(jsonString);
            Log.LogInfo("Realism mod detected");
        }

        LockPickingHelpers.LoadMiniGame();
    }
}