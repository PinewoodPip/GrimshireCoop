
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class StoppedMoving : NetObjectMessage
{
    public override string MessageType => "Client.StoppedMoving";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public StoppedMoving() { }

    public StoppedMoving(NetDataReader reader) => Deserialize(reader);
}
