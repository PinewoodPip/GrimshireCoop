
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Server;

public class CreatePlayer : Message
{
    public override string MessageType => "Server.CreatePlayer";

    public override Direction SyncDirection => Direction.ServerToClient;

    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public CreatePlayer() { }

    public CreatePlayer(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
    }
}