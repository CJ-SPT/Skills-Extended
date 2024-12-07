using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SPT.Common.Http;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IcyClawz.CustomInteractions;
using SkillsExtended.Config;
using SkillsExtended.ItemInteractions;
using SkillsExtended.Skills.LockPicking;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended;

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.4.0")]
[BepInDependency("com.IcyClawz.CustomInteractions")]
[BepInDependency("com.dirtbikercj.QuestsExtended")]
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
    
    // TODO: Move this to the lockpicking namespace
    public static GameObject LockPickingGame;
    
    internal static ManualLogSource Log;

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
        
        LoadMiniGame();
    }
    
    // TODO: Move this to the lockpicking namespace
    private static void LoadMiniGame()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        var assetBundle = AssetBundle.LoadFromFile($"{directory}/bundles/doorlock.bundle");
        var gameObject = assetBundle.LoadAssetWithSubAssets("DoorLock").First();
        LockPickingGame = Instantiate(gameObject as GameObject);

        DontDestroyOnLoad(LockPickingGame);
        var lpComp = LockPickingGame.GetOrAddComponent<LpLockPicking>();
        
        var audioSources = LockPickingGame.GetComponents(typeof(AudioSource));
        
        foreach (var source in audioSources)
        {
            var audio = source as AudioSource;
            audio!.playOnAwake = false;
            
            switch (audio!.clip.name)
            {
                case "LockpickingReset":
                    lpComp.resetSound = audio.clip;
                    break;
                
                case "LockpickingStuck":
                    lpComp.clickSound = audio.clip;
                    break;
                
                case "LockpickingTurn":
                    lpComp.rotateSound = audio.clip;
                    break;
                    
                case "LockpickingUnlocked":
                    lpComp.winSound = audio.clip;
                    break;
            }
        }

        var children = LockPickingGame
            .GetComponentsInChildren<RectTransform>();
        
        lpComp.cylinder = children
            .First(x => x.gameObject.name == "Cylinder");

        lpComp.lockpick = children
            .First(x => x.gameObject.name == "Lockpick");
        
        lpComp.levelText = LockPickingGame.GetComponentsInChildren<Text>()
            .FirstOrDefault(x => x.gameObject.name == "LockLevelText");
        
        lpComp.keyText = LockPickingGame.GetComponentsInChildren<Text>()
            .FirstOrDefault(x => x.gameObject.name == "KeyNameText");
        
        lpComp.pickStrengthRemainingLower = LockPickingGame.GetComponentsInChildren<Image>()
            .FirstOrDefault(x => x.gameObject.name == "PickStrengthBarLower");
        
        lpComp.pickStrengthRemainingUpper = LockPickingGame.GetComponentsInChildren<Image>()
            .FirstOrDefault(x => x.gameObject.name == "PickStrengthBarUpper");
        
        LockPickingGame.SetActive(false);
    }
}