
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class ReplicateObject : NetObjectMessage
{
    public override string MessageType => "Client.ReplicateObject";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public string GameObjectId;
    public Vector3 Position;
    public string SceneId; // Scene of the object being replicated
    public PeerId TargetPeerId;
    public byte[] ReplicationData;

    public ReplicateObject() { }

    public ReplicateObject(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(GameObjectId);
        writer.PutVector3(Position);
        writer.Put(SceneId);
        writer.Put(TargetPeerId);
        writer.Put(ReplicationData.Length);
        writer.Put(ReplicationData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        GameObjectId = reader.GetString();
        Position = reader.GetVector3();
        SceneId = reader.GetString();
        TargetPeerId = reader.GetInt();
        int replicationDataLength = reader.GetInt();
        ReplicationData = new byte[replicationDataLength];
        reader.GetBytes(ReplicationData, replicationDataLength);
    }
}
