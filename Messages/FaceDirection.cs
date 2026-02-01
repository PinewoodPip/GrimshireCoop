
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class FaceDirection : OwnedMessage
{
    public override string MessageType => "Shared.FaceDirection";

    public override Direction SyncDirection => Direction.ServerToClient;

    public int NetId;

    // Note: this is world pos to face, not a direction
    public float PosX;
    public float PosY;

    public FaceDirection() { }

    public FaceDirection(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
        writer.Put(PosX);
        writer.Put(PosY);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
        PosX = reader.GetFloat();
        PosY = reader.GetFloat();
    }
}
