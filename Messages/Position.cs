
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class Position : OwnedMessage
{
    public override string MessageType => "Shared.Position";

    public override Direction SyncDirection => Direction.ServerToClient;

    public int NetId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public Position() { }

    public Position(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
    }
}