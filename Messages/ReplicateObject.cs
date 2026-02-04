
using GrimshireCoop.Network;
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Shared;

public class ReplicateObject : NetObjectMessage
{
    public override string MessageType => "Shared.ReplicateObject";

    public override Direction SyncDirection => Direction.ClientToServer;

    public string GameObjectId;
    public Vector3 Position;
    public string SceneId; // Scene of the object being replicated
    public PeerId TargetPeerId;

    public ReplicateObject() { }

    public ReplicateObject(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(GameObjectId);
        writer.PutVector3(Position);
        writer.Put(SceneId);
        writer.Put(TargetPeerId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        GameObjectId = reader.GetString();
        Position = reader.GetVector3();
        SceneId = reader.GetString();
        TargetPeerId = reader.GetInt();
    }
}
