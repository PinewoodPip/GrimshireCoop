
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class ObjectAction : NetObjectMessage
{
    public override string MessageType => "Client.ObjectAction";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

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

    public override void Reset()
    {
        base.Reset();
        Action = "";
    }
}
