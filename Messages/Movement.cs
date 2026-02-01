
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class Movement : OwnedMessage
{
    public override string MessageType => "Shared.Movement";

    public override Direction SyncDirection => Direction.ServerToClient;

    public int NetId;
    public float OldPositionX;
    public float OldPositionY;
    public float OldPositionZ;
    public float NewPositionX;
    public float NewPositionY;
    public float NewPositionZ;

    public Movement() { }

    public Movement(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
        writer.Put(OldPositionX);
        writer.Put(OldPositionY);
        writer.Put(OldPositionZ);
        writer.Put(NewPositionX);
        writer.Put(NewPositionY);
        writer.Put(NewPositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
        OldPositionX = reader.GetFloat();
        OldPositionY = reader.GetFloat();
        OldPositionZ = reader.GetFloat();
        NewPositionX = reader.GetFloat();
        NewPositionY = reader.GetFloat();
        NewPositionZ = reader.GetFloat();
    }
}
