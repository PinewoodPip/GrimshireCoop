
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class RequestItemPickup : NetObjectMessage
{
    public override string MessageType => "Client.RequestItemPickup";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public RequestItemPickup() { }

    public RequestItemPickup(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
    }

    public override void Reset()
    {
        base.Reset();
    }
}
