

using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class CreateGameObject : Message
{
    public override string MessageType => "Client.CreateGameObject";
    public override Direction SyncDirection => Direction.ClientToPeers;

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