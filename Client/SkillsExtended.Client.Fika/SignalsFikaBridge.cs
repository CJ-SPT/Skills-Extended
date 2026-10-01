using System;
using System.Linq;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.LiteNetLib.Utils;
using Newtonsoft.Json;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Signals;

namespace SkillsExtendedFika;

internal struct SignalsRequestPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(4096);
}

internal struct SignalsReplyPacket : INetSerializable
{
    public string Json;

    public void Serialize(NetDataWriter writer) => writer.Put(Json);

    public void Deserialize(NetDataReader reader) => Json = reader.GetString(120000);
}

internal static class SignalsFikaBridge
{
    private static FikaServer? _server;
    private static FikaClient? _client;

    public static void Initialize()
    {
        SkillsExtended.DeveloperTools.SkillsDeveloperEditor.FikaSoloHost = () => FikaBackendUtils.IsServer && _server != null
            && _server.NetServer != null
            && !_server.NetServer.Cast<NetPeer>().Any(p => p.ConnectionState == ConnectionState.Connected);
        SignalsRuntime.IsAuthority = () => FikaBackendUtils.IsServer;
        SignalsRuntime.Transport = Send;
        SignalsRuntime.ConnectedActors = () =>
            (
                _server
                    ?.NetServer?.Cast<NetPeer>()
                    .Where(peer => peer.ConnectionState == ConnectionState.Connected)
                    .Select(peer => FikaPeerIdentity.Peers.Actor(peer))
                ?? Enumerable.Empty<string>()
            )
                .Concat(new[] { SignalsRuntime.Instance?.World.MainPlayer?.ProfileId })
                .Where(id => !string.IsNullOrEmpty(id))!;
        SignalsRuntime.AuthorityReply += Broadcast;
        FikaEventDispatcher.SubscribeEvent<PeerDisconnectedEvent>(e =>
        {
            if (FikaBackendUtils.IsServer && FikaPeerIdentity.Peers.Actor(e.Peer) is string actor)
                SignalsRuntime.Instance?.Disconnect(actor);
            else if (!FikaBackendUtils.IsServer)
                SignalsView.Current?.Close();
        });
    }

    public static void Connect(object manager)
    {
        _server = manager as FikaServer;
        _client = manager as FikaClient;
        _server?.RegisterPacket<SignalsRequestPacket, NetPeer>(ReceiveRequest);
        _client?.RegisterPacket<SignalsReplyPacket>(ReceiveReply);
    }

    private static void Send(SignalRequest request)
    {
        if (_client == null)
            return;
        var packet = new SignalsRequestPacket { Json = JsonConvert.SerializeObject(request) };
        _client.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }

    private static void ReceiveRequest(SignalsRequestPacket packet, NetPeer peer)
    {
        try
        {
            var request = JsonConvert.DeserializeObject<SignalRequest>(packet.Json);
            if (request == null || !FikaPeerIdentity.Peers.Matches(peer, request.Actor))
                return;
            SignalsRuntime.Instance?.Handle(request);
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Signal request rejected: " + e.Message);
        }
    }

    private static void ReceiveReply(SignalsReplyPacket packet)
    {
        try
        {
            SignalsRuntime.Instance?.Receive(
                JsonConvert.DeserializeObject<SignalsEnvelope>(packet.Json)
            );
        }
        catch (Exception e)
        {
            FikaSyncPlugin.Logger?.LogWarning("Signal reply rejected: " + e.Message);
        }
    }

    private static void Broadcast(SignalsEnvelope reply)
    {
        if (_server == null)
            return;
        var packet = new SignalsReplyPacket { Json = JsonConvert.SerializeObject(reply) };
        _server.SendData(ref packet, DeliveryMethod.ReliableOrdered, false);
    }
}
