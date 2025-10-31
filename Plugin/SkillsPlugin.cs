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

[BepInPlugin("com.cj.SkillsExtended", "Skills Extended", "2.1.1")]

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
        Keys = Get<KeysResponse>("/skillsExtended/GetKeys");
        SkillData = Get<SkillDataResponse>("/skillsExtended/GetSkillsConfig");
        
        LockPickingHelpers.LoadMiniGame();
    }
    
    /// <summary>
    ///     Get json from the server
    /// </summary>
    /// <param name="url">url to request</param>
    /// <typeparam name="T">Type of response</typeparam>
    /// <returns>Response</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static T Get<T>(string url)
    {
        var req = RequestHandler.GetJson(url);
            
        if (string.IsNullOrEmpty(req))
        {
            throw new InvalidOperationException("The response from the server is null or empty.");
        }

        return JsonConvert.DeserializeObject<T>(req);
    }
}