
using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class Position : NetObjectMessage
{
    public override string MessageType => "Shared.Position";

    public override Direction SyncDirection => Direction.ServerToClient;

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