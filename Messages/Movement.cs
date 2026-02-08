
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class Movement : NetObjectMessage
{
    public override string MessageType => "Client.Movement";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public Vector3 OldPosition;
    public Vector3 NewPosition;

    public Movement() { }

    public Movement(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.PutVector3(OldPosition);
        writer.PutVector3(NewPosition);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        OldPosition = reader.GetVector3();
        NewPosition = reader.GetVector3();
    }
}