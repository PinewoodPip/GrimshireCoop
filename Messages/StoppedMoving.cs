
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class StoppedMoving : OwnedMessage
{
    public override string MessageType => "Shared.StoppedMoving";

    public override Direction SyncDirection => Direction.ServerToClient;

    public int NetId;

    public StoppedMoving() { }

    public StoppedMoving(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
    }
}
