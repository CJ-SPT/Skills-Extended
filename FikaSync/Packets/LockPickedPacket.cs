using Fika.Core.Networking.LiteNetLib.Utils;

namespace SkillsExtendedFika.Packets;

public struct LockPickedPacket : INetSerializable
{
    public string DoorId;
    public bool Unlocked;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(DoorId);
        writer.Put(Unlocked);
    }

    public void Deserialize(NetDataReader reader)
    {
        DoorId = reader.GetString();
        Unlocked = reader.GetBool();
    }
}