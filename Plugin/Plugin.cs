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
using SPT.Reflection.Patching;

namespace SkillsExtended;

[BepInPlugin("com.cj.SkillsExtended", "Skills Extended", SkillsExtendedInfo.Version)]

// Because I need the idle state type from it for lockpicking
[BepInDependency("com.boogle.oldtarkovmovement", BepInDependency.DependencyFlags.SoftDependency)] 
public class Plugin : BaseUnityPlugin
{
    /// <summary>
    ///     Key Information
    /// </summary>
    public static KeysResponse Keys { get; private set; }

    /// <summary>
    ///     Skills config
    /// </summary>
    public static SkillDataResponse SkillData { get; private set; }
    
    /// <summary>
    ///     Logger
    /// </summary>
    internal static ManualLogSource Log { get; private set; }
    
    /// <summary>
    ///     Is old tarkov movement present
    /// </summary>
    internal static bool IsOldTarkovMovementDetected { get; private set; }
    
    private static PatchManager _patchManager;
    
    private void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
        {
            throw new Exception("Invalid EFT Version");
        }
        
        Log = Logger;
        ConfigManager.RegisterConfig(Config);

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();
        
#if DEBUG
        Logger.LogWarning($"PRE RELEASE BUILD OF `{SkillsExtendedInfo.Version}` - NO SUPPORT");
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        
        DetectSoftDependencies();
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

    private static void DetectSoftDependencies()
    {
        // Compatibility for lockpicking
        if (Chainloader.PluginInfos.Keys.Contains("com.boogle.oldtarkovmovement"))
        {
            RE.GetOldMovementTypes();
            IsOldTarkovMovementDetected = true;
            Log.LogInfo("Enabling compatibility for old tarkov movement");
        }
    }
}