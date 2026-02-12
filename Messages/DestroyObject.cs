
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class DestroyObject : NetObjectMessage
{
    public override string MessageType => "Client.DestroyObject";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public DestroyObject() { }

    public DestroyObject(NetDataReader reader) => Deserialize(reader);
}
