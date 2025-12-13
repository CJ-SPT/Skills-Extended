using Fika.Core.Networking.LiteNetLib.Utils;
using SkillsExtended.LockPicking;

namespace SkillsExtendedFika.Packets;

public struct DoorPickedPacket : INetSerializable
{
    public string DoorId;
    public bool Unlocked;

    public DoorPickedPacket()
    { }

    public DoorPickedPacket(DoorPickedEventData data)
    {
        DoorId = data.DoorId;
        Unlocked = data.Unlocked;
    }
    
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