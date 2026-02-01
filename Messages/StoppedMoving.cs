
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class StoppedMoving : NetObjectMessage
{
    public override string MessageType => "Shared.StoppedMoving";

    public override Direction SyncDirection => Direction.ServerToClient;

    public StoppedMoving() { }

    public StoppedMoving(NetDataReader reader) => Deserialize(reader);
}
