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

[BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "1.1.0")]
public class Plugin : BaseUnityPlugin
{
    public const int TarkovVersion = 30626;

    public static ISession Session;

    public static GameWorld GameWorld => Singleton<GameWorld>.Instance;
    public static IEnumerable<Item> Items => Session?.Profile?.Inventory?.AllRealPlayerItems;

    // Contains key information
    public static KeysResponse Keys;

    // Contains skill data
    public static SkillDataResponse SkillData;

    public static RealismConfig RealismConfig;

    private static GameObject Hook;

    internal static FirstAidBehaviour FirstAidScript;
    internal static FieldMedicineBehaviour FieldMedicineScript;
    internal static UsecRifleBehaviour NatoWeaponScript;
    internal static BearRifleBehaviour EasternWeaponScript;

    internal static AnimationClip[] AnimationClips { get; private set; }
    
    internal static ManualLogSource Log;

    private void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
        {
            throw new Exception("Invalid EFT Version");
        }

        Log = Logger;
        
        new SkillPanelShowPatch().Enable();
        new BuffIconShowPatch().Enable();
        new SkillManagerConstructorPatch().Enable();
        new OnScreenChangePatch().Enable();
        new OnGameStartedPatch().Enable();
        
        new SkillManagerMethod1Patch().Enable();
        new SkillManagerMethod2Patch().Enable();
        new SkillManagerMethod3Patch().Enable();
        new SkillManagerMethod4Patch().Enable();
        new SkillManagerMethod5Patch().Enable();
        
        new DoorActionPatch().Enable();
        new KeycardDoorActionPatch().Enable();

        new DoMedEffectPatch().Enable();

#if DEBUG
        Logger.LogWarning("PRE RELEASE BUILD - NO SUPPORT");
        new LocationSceneAwakePatch().Enable();
        
        Logger.LogWarning("DEBUG BUILD FEATURES ENABLED");
        ConsoleCommands.RegisterCommands();
#endif
        
        Utils.GetTypes();
        
        Logger.LogInfo("Creating Skill Controller Object");
        
        Hook = new GameObject("Skills Controller Object");
        DontDestroyOnLoad(Hook);
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

        if (SkillData.MedicalSkills.EnableFirstAid)
        {
            FirstAidScript = Hook.AddComponent<FirstAidBehaviour>();
        }

        if (SkillData.MedicalSkills.EnableFieldMedicine)
        {
            FieldMedicineScript = Hook.AddComponent<FieldMedicineBehaviour>();
        }

        if (SkillData.NatoRifleSkill.Enabled)
        {
            NatoWeaponScript = Hook.AddComponent<UsecRifleBehaviour>();
        }
        
        if (SkillData.EasternRifleSkill.Enabled)
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