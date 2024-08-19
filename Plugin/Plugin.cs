using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using Newtonsoft.Json;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SkillsExtended.Patches;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using IcyClawz.CustomInteractions;
using SkillsExtended.ItemInteractions;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills;
using UnityEngine;

namespace SkillsExtended;

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.2.0")]
[BepInDependency("com.IcyClawz.CustomInteractions")]
public class Plugin : BaseUnityPlugin
{
    public const int TarkovVersion = 30626;

    public static ISession Session;
    
    // Contains key information
    public static KeysResponse Keys;

    // Contains skill data
    public static SkillDataResponse SkillData;
    
    public static List<QuestResponse> Quests;
    
    public static RealismConfig RealismConfig;

    private static GameObject Hook;
    
    internal static UsecRifleBehaviour NatoWeaponScript;
    internal static BearRifleBehaviour EasternWeaponScript;
    internal static BuffController BuffController;

    internal static readonly SkillManagerExt PlayerSkillManagerExt = new();
    internal static readonly SkillManagerExt ScavSkillManagerExt = new();

    private static GameObject _miniGameObject;
    internal static LPLockpicking MiniGame { get; private set; }
    
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
        
#if DEBUG
        Logger.LogWarning("PRE RELEASE BUILD - NO SUPPORT");
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        
        Utils.GetTypes();
        
        Hook = new GameObject("Skills Controller Object");
        
        BuffController = Hook.AddComponent<BuffController>();
        
        DontDestroyOnLoad(Hook);
    }

    private void Start()
    {
        Keys = Utils.Get<KeysResponse>("/skillsExtended/GetKeys");
        SkillData = Utils.Get<SkillDataResponse>("/skillsExtended/GetSkillsConfig");
        Quests = Utils.Get<List<QuestResponse>>("/skillsExtended/GetCustomQuestConditions");
        
        // If realism is installed, load its config
        if (Chainloader.PluginInfos.ContainsKey("RealismMod"))
        {
            var jsonString = RequestHandler.GetJson("/RealismMod/GetInfo");
            RealismConfig = JsonConvert.DeserializeObject<RealismConfig>(jsonString);
            Log.LogInfo("Realism mod detected");
        }
        
        if (SkillData.NatoRifle.Enabled)
        {
            NatoWeaponScript = Hook.AddComponent<UsecRifleBehaviour>();
        }
        
        if (SkillData.EasternRifle.Enabled)
        {
            EasternWeaponScript = Hook.AddComponent<BearRifleBehaviour>();
        }
        
        LoadBundle();
    }

    private void Update()
    {
        if (Session == null && ClientAppUtils.GetClientApp().GetClientBackEndSession() != null)
        {
            Session = ClientAppUtils.GetClientApp().GetClientBackEndSession();
            
            Log.LogDebug("Session set");
        }
    }

    private static void LoadBundle()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        var assetBundle = AssetBundle.LoadFromFile($"{directory}/bundles/lp_door.bundle");
        var dataBundle = AssetBundle.LoadFromFile($"{directory}/bundles/lp_game_data.bundle");
        
        var gameObject = assetBundle.LoadAssetWithSubAssets("lp_door").First();
        var dataObject = dataBundle.LoadAssetWithSubAssets("lp_Game_Data").First();

        _miniGameObject = Instantiate(gameObject as GameObject);
        var dataInstance = Instantiate(dataObject as GameObject);
        
        Log.LogError(_miniGameObject.name);
        
        DontDestroyOnLoad(_miniGameObject);
        
        var visualObjects = _miniGameObject.GetComponentsInChildren<MonoBehaviour>();
        var audioObjects = dataInstance.GetComponent<AudioClip>();
        
        var doorComp = visualObjects
            .FirstOrDefault(x => x.gameObject.name == "DoorLock");

        var cylinderComp = visualObjects
            .FirstOrDefault(x => x?.gameObject?.name == "Cylinder");

        var lockPickComp = visualObjects
            .FirstOrDefault(x => x?.gameObject?.name == "Lockpick");
        
        var buttonRotate = visualObjects
            .FirstOrDefault(x => x?.gameObject?.name == "ButtonRotate");
        
        var audioSource = _miniGameObject.GetComponentsInChildren(typeof(AudioSource));
        
        buttonRotate?.gameObject.SetActive(false);
        
        MiniGame = doorComp?.gameObject.GetOrAddComponent<LPLockpicking>();

        MiniGame.cylinder = cylinderComp?.gameObject.GetComponent<RectTransform>();
        MiniGame.lockpick = lockPickComp?.gameObject.GetComponent<RectTransform>();
        MiniGame.audioSource = audioSource.First() as AudioSource;
        
        foreach (var data in dataInstance.gameObject.GetComponentsInChildren(typeof(AudioSource)))
        {
            if (data.gameObject.name == "Sound_Reset")
            {
                var source = data as AudioSource;
                MiniGame!.resetSound = source!.clip;
            }
            
            if (data.gameObject.name == "Sound_Stuck")
            {
                var source = data as AudioSource;
                MiniGame!.clickSound = source!.clip;
            }
            
            if (data.gameObject.name == "Sound_Turn")
            {
                var source = data as AudioSource;
                MiniGame!.rotateSound = source!.clip;
            }
            
            if (data.gameObject.name == "Sound_Unlocked")
            {
                var source = data as AudioSource;
                MiniGame!.winSound = source!.clip;
            }
        }
    }
}