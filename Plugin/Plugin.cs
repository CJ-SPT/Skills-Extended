using Aki.Reflection.Utils;
using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.VersionChecker;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SkillsExtended.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillsExtended
{
    [BepInPlugin("com.dirtbikercj.SkillsExtended", "Skills Extended", "0.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const int TarkovVersion = 29197;

        public static ISession Session;

        public static GameWorld GameWorld => Singleton<GameWorld>.Instance;
        public static Player Player => Singleton<GameWorld>.Instance.MainPlayer;
        public static IEnumerable<Item> Items => Session?.Profile?.Inventory?.AllRealPlayerItems;

        internal static GameObject Hook;
        internal static FirstAidBehavior FirstAidScript;
        internal static FieldMedicineBehavior FieldMedicineScript;
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
            new DoMedEffectPatch().Enable();
            new SetItemInHands().Enable();

            SEConfig.InitializeConfig(Config);

            Log = Logger;

            Hook = new GameObject("Skills Controller Object");

            FirstAidScript = Hook.AddComponent<FirstAidBehavior>();
            FieldMedicineScript = Hook.AddComponent<FieldMedicineBehavior>();
            WeaponsScript = Hook.AddComponent<WeaponProficiencyBehaviors>();
            BearPowerScript = Hook.AddComponent<BearRawPowerBehavior>();

            DontDestroyOnLoad(Hook);

#if DEBUG
            new LocationSceneAwakePatch().Enable();
            //new AnimationEventInitClassPatch().Enable();
            ConsoleCommands.RegisterCommands();
#endif
        }

        private void Start()
        {
            Constants.Keys = Utils.Get<KeysResponse>("/skillsExtended/GetKeys");
            Constants.SkillData = Utils.Get<SkillDataResponse>("/skillsExtended/GetSkillsConfig");
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