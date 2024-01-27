using System;
using BepInEx;
using UnityEngine;
using BepInEx.Logging;
using Aki.Reflection.Utils;
using SkillsExtended.Controllers;
using DrakiaXYZ.VersionChecker;
using SkillsExtended.Helpers;
using Skills_Extended.Controllers;
using SkillsExtended.Patches;

namespace SkillsExtended
{
    [BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "0.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 26535;

        public static ISession Session;
        public static Profile SEProfile;

        internal static GameObject Hook;
        internal static MedicalBehavior MedicalScript;
        internal static WeaponProficiencyBehaviors WeaponsScript;

        internal static ManualLogSource Log;

        private bool _warned = false;

        void Awake()
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
         
            Log = Logger;
            
            Hook = new GameObject("Skills Controller Object");
           
            MedicalScript = Hook.AddComponent<MedicalBehavior>();
            WeaponsScript = Hook.AddComponent<WeaponProficiencyBehaviors>();

            DontDestroyOnLoad(Hook);           

#if DEBUG
            ConsoleCommands.RegisterCommands();
#endif
        }

        void Update()
        {

            #if BETA
            if (!_warned && PreloaderUI.Instance != null)
            {
                PreloaderUI.Instance.ShowErrorScreen("Skills Extended", "Skills Extended: This is a BETA build. Report all bugs in the thread, or on the website.");
                Log.LogDebug("User was warned.");
                _warned = true;
            }
            #endif

            if (Session == null && ClientAppUtils.GetMainApp().GetClientBackEndSession() != null)
            {
                Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();

                Log.LogDebug("Session set");
            }
        }
    }
}