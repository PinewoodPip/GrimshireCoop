
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class Position : NetObjectMessage
{
    public override string MessageType => "Client.Position";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public Vector3 Pos;

    public Position() { }

    public Position(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.PutVector3(Pos);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        Pos = reader.GetVector3();
    }
}