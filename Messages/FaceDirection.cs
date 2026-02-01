
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class FaceDirection : NetObjectMessage
{
    public override string MessageType => "Shared.FaceDirection";

    public override Direction SyncDirection => Direction.ServerToClient;

    // Note: this is world pos to face, not a direction
    public float PosX;
    public float PosY;

    public FaceDirection() { }

    public FaceDirection(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PosX);
        writer.Put(PosY);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        PosX = reader.GetFloat();
        PosY = reader.GetFloat();
    }
}
