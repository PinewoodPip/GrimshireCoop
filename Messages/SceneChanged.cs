
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class SceneChanged : OwnedMessage
{
    public override string MessageType => "Shared.SceneChanged";

    public override Direction SyncDirection => Direction.ClientToServer;

    public string SceneId;
    public NetId ClientPlayerNetId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;

    public SceneChanged() { }

    public SceneChanged(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(SceneId);
        writer.Put(ClientPlayerNetId);
        writer.Put(PositionX);
        writer.Put(PositionY);
        writer.Put(PositionZ);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        SceneId = reader.GetString();
        ClientPlayerNetId = reader.GetInt();
        PositionX = reader.GetFloat();
        PositionY = reader.GetFloat();
        PositionZ = reader.GetFloat();
    }
}
