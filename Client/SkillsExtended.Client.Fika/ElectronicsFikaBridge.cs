using System;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using Newtonsoft.Json;
using SkillsExtended.Electronics;
using SkillsExtended.Skills.Electronics;

namespace SkillsExtendedFika;

internal struct ElectronicsRequestPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(4096);
}

internal struct ElectronicsReplyPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(60000);
}

internal static class ElectronicsFikaBridge
{
    private static FikaServer? _server;
    private static FikaClient? _client;

    public static void Initialize()
    {
        ElectronicsRuntime.IsAuthority = () => FikaBackendUtils.IsServer;
        ElectronicsRuntime.Transport = Send;
        ElectronicsRuntime.AuthorityReply += Broadcast;
        FikaEventDispatcher.SubscribeEvent<PeerDisconnectedEvent>(e =>
        {
            if (FikaBackendUtils.IsServer && e.Peer.Tag is string actor)
            {
                ElectronicsRuntime.Instance?.Disconnect(actor);
            }
            else if (!FikaBackendUtils.IsServer)
            {
                ElectronicsRuntime.Instance?.ConnectionLost();
            }
        });
    }

    public static void Connect(object manager)
    {
        _server = manager as FikaServer;
        _client = manager as FikaClient;
        _server?.RegisterPacket<ElectronicsRequestPacket, NetPeer>(ReceiveRequest);
        _client?.RegisterPacket<ElectronicsReplyPacket>(ReceiveReply);
        if (_client != null)
        {
            ElectronicsRuntime.Instance?.ConnectionLost();
        }
    }

    private static void Send(HackRequest request)
    {
        if (_client == null)
        {
            return;
        }

        var packet = new ElectronicsRequestPacket { Json = JsonConvert.SerializeObject(request) };
        _client.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }

    private static void ReceiveRequest(ElectronicsRequestPacket packet, NetPeer peer)
    {
        try
        {
            var request = JsonConvert.DeserializeObject<HackRequest>(packet.Json);
            // Fika authenticates the peer's profile in Tag. Never trust a caller-supplied actor.
            if (request == null || !(peer.Tag is string actor) || request.Actor != actor)
            {
                return;
            }

            ElectronicsRuntime.Instance?.Handle(request);
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Rejected PDA request: " + e.Message);
        }
    }

    private static void ReceiveReply(ElectronicsReplyPacket packet)
    {
        try
        {
            ElectronicsRuntime.Instance?.Receive(
                JsonConvert.DeserializeObject<HackReply>(packet.Json)
            );
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Rejected PDA reply: " + e.Message);
        }
    }

    private static void Broadcast(HackReply reply)
    {
        if (_server == null)
        {
            return;
        }

        // Send snapshots, never the authority's mutable board. Hide unexplored contents and RNG state.
        var snapshot = JsonConvert.DeserializeObject<HackReply>(
            JsonConvert.SerializeObject(reply)
        )!;
        if (snapshot.Board != null)
        {
            snapshot.Board.RandomState = 0;
            foreach (var n in snapshot.Board.Nodes)
            {
                n.CacheContent = NodeKind.Empty;
                if (!n.Revealed)
                {
                    n.Kind = NodeKind.Empty;
                    n.Coherence = n.MaximumCoherence = n.Strength = 0;
                }
            }
        }

        var packet = new ElectronicsReplyPacket { Json = JsonConvert.SerializeObject(snapshot) };
        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }
}
