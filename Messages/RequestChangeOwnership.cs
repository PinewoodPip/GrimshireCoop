
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class RequestChangeOwnership : NetObjectMessage
{
    public override string MessageType => "Client.RequestChangeOwnership";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public string SceneId;

    public RequestChangeOwnership() { }

    public RequestChangeOwnership(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(SceneId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        SceneId = reader.GetString();
    }

    public override void Reset()
    {
        base.Reset();
        SceneId = "";
    }
}
