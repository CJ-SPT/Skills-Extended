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

            Incompatible();

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

        private void Incompatible()
        {
            var dllLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var modsLoc = Path.Combine(dllLoc, "..", "..", "user", "mods", "wayfarer");
            var fullPath = Path.GetFullPath(modsLoc);

            if (Directory.Exists(fullPath))
            {
                Environment.Exit(0);
            }
        }
    }
}