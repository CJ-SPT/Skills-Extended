using System.Reflection;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Backend;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtendedFika;

internal static class FikaPeerIdentity
{
    internal static readonly PeerActorRegistry<NetPeer> Peers = new();
}

// Observe Fika's own handshake without replacing its packet handler. Tag is a nickname.
internal sealed class FikaPeerIdentityPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(
            typeof(FikaServer),
            "OnNetworkSettingsPacketReceived",
            new[] { typeof(NetworkSettingsPacket), typeof(NetPeer) }
        );

    [PatchPostfix]
    private static void Postfix(NetworkSettingsPacket packet, NetPeer peer) =>
        FikaPeerIdentity.Peers.Bind(peer, packet.ProfileId);
}
