using System;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using Newtonsoft.Json;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;

namespace SkillsExtendedFika;

internal struct PickingRequestPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(4096);
}

internal struct PickingReplyPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(8192);
}

internal static class LockPickingFikaBridge
{
    private static FikaServer? _server;
    private static FikaClient? _client;

    public static void Initialize()
    {
        PickingRuntime.IsAuthority = () => FikaBackendUtils.IsServer;
        PickingRuntime.Transport = Send;
        PickingRuntime.AuthorityReply += Broadcast;
        FikaEventDispatcher.SubscribeEvent<PeerDisconnectedEvent>(e =>
        {
            if (FikaBackendUtils.IsServer && FikaPeerIdentity.Peers.Actor(e.Peer) is string actor)
                PickingRuntime.Instance?.Disconnect(actor);
            else if (!FikaBackendUtils.IsServer)
                PickingRuntime.Instance?.ConnectionLost();
        });
    }

    public static void Connect(object manager)
    {
        _server = manager as FikaServer;
        _client = manager as FikaClient;
        _server?.RegisterPacket<PickingRequestPacket, NetPeer>(ReceiveRequest);
        _client?.RegisterPacket<PickingReplyPacket>(ReceiveReply);
        if (_client != null)
            PickingRuntime.Instance?.ConnectionLost();
    }

    private static void Send(PickRequest request)
    {
        if (_client == null)
            return;
        var packet = new PickingRequestPacket { Json = JsonConvert.SerializeObject(request) };
        _client.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }

    private static void ReceiveRequest(PickingRequestPacket packet, NetPeer peer)
    {
        try
        {
            var request = JsonConvert.DeserializeObject<PickRequest>(packet.Json);
            if (request == null || !FikaPeerIdentity.Peers.Matches(peer, request.Actor))
                return;
            PickingRuntime.Instance?.Handle(request);
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Rejected picking request: " + e.Message);
        }
    }

    private static void ReceiveReply(PickingReplyPacket packet)
    {
        try
        {
            PickingRuntime.Instance?.Receive(JsonConvert.DeserializeObject<PickReply>(packet.Json));
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Rejected picking reply: " + e.Message);
        }
    }

    private static void Broadcast(PickReply reply)
    {
        if (_server == null)
            return;
        var packet = new PickingReplyPacket { Json = JsonConvert.SerializeObject(reply) };
        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }
}
