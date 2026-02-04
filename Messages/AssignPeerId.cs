using System.Collections.Generic;
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Server;

public class AssignPeerId : Message
{
    public override string MessageType => "Server.AssignPeerId";
    public override Direction SyncDirection => Direction.ServerToClient;

    public PeerId PeerId;
    public Dictionary<PeerId, string> PeerScenes = [];

    public AssignPeerId() { }

    public AssignPeerId(PeerId peerId, Dictionary<PeerId, string> peerScenes)
    {
        PeerId = peerId;
        PeerScenes = peerScenes;
    }

    public AssignPeerId(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PeerId);
        writer.Put(PeerScenes.Count);
        foreach (var kvp in PeerScenes)
        {
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        PeerId = reader.GetInt();
        int count = reader.GetInt();
        for (int i = 0; i < count; i++)
        {
            int key = reader.GetInt();
            string value = reader.GetString();
            PeerScenes[key] = value;
        }
    }
}