using BepInEx;
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

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.4.1")]
[BepInDependency("com.IcyClawz.CustomInteractions")]
[BepInDependency("com.dirtbikercj.QuestsExtended")]

// Because I need the idle state type from it for lockpicking
[BepInDependency("com.boogle.oldtarkovmovement", BepInDependency.DependencyFlags.SoftDependency)] 

// NOT COMPATIBLE
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
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

    private static bool _IsFikaPresent;
    private static bool _showingIncompatibiltyMessage;
    
    internal static bool IsOldTarkovMovementDetected { get; private set; }
    
    private void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
        {
            throw new Exception("Invalid EFT Version");
        }

        _IsFikaPresent = Chainloader.PluginInfos.Keys.Contains("com.fika.core");
        
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
        if (_IsFikaPresent) return;
        
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

    private void Update()
    {
        if (!_IsFikaPresent) return;
        
        if (Singleton<PreloaderUI>.Instantiated && !_showingIncompatibiltyMessage)
        {
            _showingIncompatibiltyMessage = true;
            
            PreloaderUI.Instance.ShowCriticalErrorScreen(
                "Skills Extended.", 
                "Skills Extended is not compatible with Fika. Remove the mod in its entirety from user/mods, Bepinex/patchers and Bepinex/plugins.\n\nYou may also run into profile issues now because of the trader associated with this mod. This is the price you pay for not reading mod pages. Do not ask me or Fika/SPT support to save your profile if it has issues. You're on your own.",
                ErrorScreen.EButtonType.QuitButton,
                30f,
                () => Application.Quit(0),
                () => Application.Quit(0));
        }
    }
}