

using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Server;

public class CreateGameObject : Message
{
    public override string MessageType => "Server.CreateGameObject";

    public override Direction SyncDirection => Direction.ServerToClient;

    public string GameObjectId;
    public NetId NetId;
    public PeerId OwnerPeerId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public CreateGameObject() { }

    public CreateGameObject(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(GameObjectId);
        writer.Put(NetId);
        writer.Put(OwnerPeerId);

        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        GameObjectId = reader.GetString();
        NetId = reader.GetInt();
        OwnerPeerId = reader.GetInt();

        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
    }
}