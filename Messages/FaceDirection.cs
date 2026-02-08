
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class FaceDirection : NetObjectMessage
{
    public override string MessageType => "Client.FaceDirection";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

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
