using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class ObjectAction : NetObjectMessage
{
    public override string MessageType => "Shared.ObjectAction";

    public override Direction SyncDirection => Direction.ClientToServer;

    public string Action;

    public ObjectAction() { }

    public ObjectAction(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(Action);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        Action = reader.GetString();
    }
}
