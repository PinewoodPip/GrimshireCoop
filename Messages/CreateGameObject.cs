

using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Server;

public class CreateGameObject : Message
{
    public override string MessageType => "Server.CreateGameObject";

    public override Direction SyncDirection => Direction.ServerToClient;

    public string GameObjectId;
    public NetId NetId;
    public PeerId OwnerPeerId;
    public Vector3 Position;

    public CreateGameObject() { }

    public CreateGameObject(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(GameObjectId);
        writer.Put(NetId);
        writer.Put(OwnerPeerId);
        writer.PutVector3(Position);
    }

    public override void Deserialize(NetDataReader reader)
    {
        GameObjectId = reader.GetString();
        NetId = reader.GetInt();
        OwnerPeerId = reader.GetInt();
        Position = reader.GetVector3();
    }
}