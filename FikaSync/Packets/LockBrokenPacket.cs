using Fika.Core.Networking.LiteNetLib.Utils;

namespace SkillsExtendedFika.Packets;

public struct LockBrokenPacket : INetSerializable
{
    public string DoorId;
    public bool Broken;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(DoorId);
        writer.Put(Broken);
    }

    public void Deserialize(NetDataReader reader)
    {
        DoorId = reader.GetString();
        Broken = reader.GetBool();
    }
}