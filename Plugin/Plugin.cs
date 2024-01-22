using System;
using EFT.UI;
using BepInEx;
using System.IO;
using UnityEngine;
using BepInEx.Logging;
using System.Reflection;
using SkillRedux.Helpers;
using Aki.Reflection.Utils;
using SkillsExtended.Controllers;
using DrakiaXYZ.VersionChecker;
using SkillsExtended.Helpers;

namespace SkillsExtended
{
    [BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "0.2.1")]

    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 26535;

        public static ISession Session;

        internal static GameObject Hook;
        internal static MedicalBehavior MedicalScript;
        internal static ManualLogSource Log;

        private bool _warned = false;

        void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }

            Log = Logger;
            Log.LogInfo("Loading Skill Redux");
            Hook = new GameObject("Event Object");
            MedicalScript = Hook.AddComponent<MedicalBehavior>();
            DontDestroyOnLoad(Hook);

            #if DEBUG
            ConsoleCommands.RegisterCommands();
            #endif
        }

        void Update()
        {
            if (!_warned && PreloaderUI.Instance != null)
            {
                PreloaderUI.Instance.ShowErrorScreen("Skills Extended", "Skills Extended: This is a BETA build. Report all bugs in the thread, or on the website. Not compatible with realisms med changes.");
                Log.LogDebug("User was warned.");
                _warned = true;
            }

            if (Session == null && ClientAppUtils.GetMainApp().GetClientBackEndSession() != null)
            {
                Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();

                Log.LogDebug("Session set");
            }
        }
    }
}