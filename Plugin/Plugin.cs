using Aki.Reflection.Utils;
using BepInEx;
using BepInEx.Logging;
using DrakiaXYZ.VersionChecker;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using SkillsExtended.Patches;
using System;
using UnityEngine;

namespace SkillsExtended
{
    [BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "0.4.2")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;

        public static ISession Session;

        internal static GameObject Hook;
        internal static MedicalBehavior MedicalScript;
        internal static WeaponProficiencyBehaviors WeaponsScript;
        internal static BearRawPowerBehavior BearPowerScript;

        internal static ManualLogSource Log;

        private void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            new EnableSkillsPatch().Enable();
            new SkillPanelNamePatch().Enable();
            new SkillPanelDisablePatch().Enable();
            new SimpleToolTipPatch().Enable();
            new SkillManagerConstructorPatch().Enable();
            new OnScreenChangePatch().Enable();
            new GetActionsClassPatch().Enable();

            SEConfig.InitializeConfig(Config);

            Log = Logger;

            Hook = new GameObject("Skills Controller Object");

            MedicalScript = Hook.AddComponent<MedicalBehavior>();
            WeaponsScript = Hook.AddComponent<WeaponProficiencyBehaviors>();
            //BearPowerScript = Hook.AddComponent<BearRawPowerBehavior>();

            DontDestroyOnLoad(Hook);

#if DEBUG
            new LocationSceneAwakePatch().Enable();
            //new AnimationEventInitClassPatch().Enable();
            ConsoleCommands.RegisterCommands();
#endif
        }

        private void Start()
        {
            Utils.GetKeysFromServer();
        }

        private void Update()
        {
            if (Session == null && ClientAppUtils.GetMainApp().GetClientBackEndSession() != null)
            {
                Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();

                Log.LogDebug("Session set");
            }
        }
    }
}