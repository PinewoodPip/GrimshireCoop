
using GrimshireCoop.Network;
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class SceneChanged : OwnedMessage
{
    public override string MessageType => "Shared.SceneChanged";

    public override Direction SyncDirection => Direction.ClientToServer;

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
}
