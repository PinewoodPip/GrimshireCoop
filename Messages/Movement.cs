
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class Movement : NetObjectMessage
{
    public override string MessageType => "Shared.Movement";

    public override Direction SyncDirection => Direction.ServerToClient;

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
        OldPositionX = reader.GetFloat();
        OldPositionY = reader.GetFloat();
        OldPositionZ = reader.GetFloat();
        NewPositionX = reader.GetFloat();
        NewPositionY = reader.GetFloat();
        NewPositionZ = reader.GetFloat();
    }
}
