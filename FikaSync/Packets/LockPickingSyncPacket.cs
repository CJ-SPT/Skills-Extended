using Fika.Core.Networking.LiteNetLib.Utils;
using SkillsExtended.LockPicking;

namespace SkillsExtendedFika.Packets;

public struct LockPickingSyncPacket : INetSerializable
{
    public string DoorId;
    public int Attempts;
    public bool Unlocked;
    public bool Broken;

    public LockPickingSyncPacket(LockPickingEventData data)
    {
        DoorId = data.DoorId;
        Attempts = data.Attempts;
        Unlocked = data.Unlocked;
        Broken = data.Broken;
    }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(DoorId);
        writer.Put(Attempts);
        writer.Put(Unlocked);
        writer.Put(Broken);
    }

    public void Deserialize(NetDataReader reader)
    {
        DoorId = reader.GetString();
        Attempts = reader.GetInt();
        Unlocked = reader.GetBool();
        Broken = reader.GetBool();
    }
}