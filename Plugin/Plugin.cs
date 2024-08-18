using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using EFT;
using EFT.InventoryLogic;
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
using System.Reflection;
using SkillsExtended.Skills;
using UnityEngine;

namespace SkillsExtended;

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.2.0")]
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

    internal static readonly SkillManagerExt PlayerSkillManagerExt = new();
    internal static readonly SkillManagerExt ScavSkillManagerExt = new();
    
    internal static AnimationClip[] AnimationClips { get; private set; }
    
    internal static ManualLogSource Log;

    private void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
        {
            throw new Exception("Invalid EFT Version");
        }

        Log = Logger;
        
        PatchManager.PatchAll();
        
#if DEBUG
        Logger.LogWarning("PRE RELEASE BUILD - NO SUPPORT");
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        
        Utils.GetTypes();
        
        Hook = new GameObject("Skills Controller Object");
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
        if (Session == null && ClientAppUtils.GetMainApp().GetClientBackEndSession() != null)
        {
            Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();

            Log.LogDebug("Session set");
        }
    }

    private static void LoadBundle()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        AnimationClips = AssetBundle.LoadFromFile($"{directory}/bundles/lockpicking_anim.bundle").LoadAllAssets<AnimationClip>();
    }
}