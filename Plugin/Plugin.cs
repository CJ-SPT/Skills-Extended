using System;
using BepInEx;
using System.IO;
using UnityEngine;
using BepInEx.Logging;
using System.Reflection;
using Aki.Reflection.Utils;
using SkillsExtended.Controllers;
using DrakiaXYZ.VersionChecker;
using SkillRedux.Helpers;

namespace SkillsExtended
{
    [BepInPlugin("com.dirtbikercj.SkillsExtended", "Skill Extended", "0.1.0")]

    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 26535;

        public static ISession Session;

        internal static string Directory;
        internal static GameObject Hook;
        internal static FirstAid FAScript;
        internal static ManualLogSource Log;

        void Awake()
        {
            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception("Invalid EFT Version");
            }
            
            Directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            Log = Logger;
            Log.LogInfo("Loading Skill Redux");
            Hook = new GameObject("Event Object");
            FAScript = Hook.AddComponent<FirstAid>();
            DontDestroyOnLoad(Hook);

            #if DEBUG
            ConsoleCommands.RegisterCommands();
            #endif
        }

        void Update()
        {
            if (Session == null && ClientAppUtils.GetMainApp().GetClientBackEndSession() != null)
            {
                Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
                Log.LogDebug("Session set");
            }
        }
    }
}