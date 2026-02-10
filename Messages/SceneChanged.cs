
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class SceneChanged : OwnedMessage
{
    public override string MessageType => "Client.SceneChanged";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => false; // All clients should keep track of which scenes peers are in.

    public string SceneId;
    public NetId ClientPlayerNetId;
    public Vector3 Position;

    public SceneChanged() { }

    public SceneChanged(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(SceneId);
        writer.Put(ClientPlayerNetId);
        writer.PutVector3(Position);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        SceneId = reader.GetString();
        ClientPlayerNetId = reader.GetInt();
        Position = reader.GetVector3();
    }

    public override void Reset()
    {
        base.Reset();
        SceneId = "";
        ClientPlayerNetId = default;
        Position = default;
    }
}
