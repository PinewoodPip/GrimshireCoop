using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Server;

public class AssignPeerId : Message
{
    public override string MessageType => "Server.AssignPeerId";
    public override Direction SyncDirection => Direction.ServerToClient;

    public int PeerId;

    public AssignPeerId() { }

    public AssignPeerId(int peerId)
    {
        PeerId = peerId;
    }

    public AssignPeerId(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PeerId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        PeerId = reader.GetInt();
    }
}