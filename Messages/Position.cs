
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class Position : NetObjectMessage
{
    public override string MessageType => "Shared.Position";

    public override Direction SyncDirection => Direction.ServerToClient;

    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public Position() { }

    public Position(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
    }
}