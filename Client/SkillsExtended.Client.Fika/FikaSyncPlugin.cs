using System;
using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using SkillsExtended;

namespace SkillsExtendedFika;

[BepInPlugin("com.cj.SkillsExtendedFika", "Skills Extended Fika", SkillsExtendedInfo.SYNC_VERSION)]
[BepInDependency(SkillsExtendedInfo.MOD_GUID, SkillsExtendedInfo.VERSION)]
[BepInDependency("com.fika.core", SkillsExtendedInfo.MIN_FIKA_VERSION)]
public class FikaSyncPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource? Logger;

    private void Awake()
    {
        Logger = base.Logger;
        if (!VersionChecker.CheckEftVersion(Logger, Config))
        {
            throw new Exception("Invalid EFT Version");
        }

        SkillsExtendedInfo.SyncPluginPresent = true;
        ElectronicsFikaBridge.Initialize();
        SignalsFikaBridge.Initialize();
        LockPickingFikaBridge.Initialize();
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
    }

    private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent createdEvent)
    {
        ElectronicsFikaBridge.Connect(createdEvent.Manager);
        SignalsFikaBridge.Connect(createdEvent.Manager);
        LockPickingFikaBridge.Connect(createdEvent.Manager);
    }
}
