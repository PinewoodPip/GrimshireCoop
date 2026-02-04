
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Shared;

public class ReplicateObject : NetObjectMessage
{
    public override string MessageType => "Shared.ReplicateObject";

    public override Direction SyncDirection => Direction.ClientToServer;

    public string GameObjectId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    public string SceneId; // Scene of the object being replicated
    public int TargetPeerId;

    public ReplicateObject() { }

    public ReplicateObject(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(GameObjectId);
        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
        writer.Put(SceneId);
        writer.Put(TargetPeerId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        GameObjectId = reader.GetString();
        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
        SceneId = reader.GetString();
        TargetPeerId = reader.GetInt();
    }
}
