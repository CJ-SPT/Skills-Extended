using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using SkillsExtended.Skills.LockPicking.Actions;
using SkillsExtendedFika.Packets;
using SPT.Reflection.Patching;

namespace SkillsExtendedFika.Patches;

public class LockPickActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(LockPickActionHandler), nameof(LockPickActionHandler.PickLockAction));
    }

    [PatchPostfix]
    public static void Postfix(LockPickActionHandler __instance, bool unlocked)
    {
        var packet = new DoorPickedPacket
        {
            DoorId = __instance.InteractiveObject.Id,
            Unlocked = unlocked
        };

        FikaSyncPlugin.Logger?.LogError("Sending door unlocked packet");
        
        if (FikaBackendUtils.IsServer)
        {
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
        else
        {
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
    }
}