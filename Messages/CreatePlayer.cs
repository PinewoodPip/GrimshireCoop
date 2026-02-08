
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Server;

public class CreatePlayer : Message
{
    public override string MessageType => "Server.CreatePlayer";
    public override Direction SyncDirection => Direction.ServerToClient;

    public Vector3 Position;

    public CreatePlayer() { }

    public CreatePlayer(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.PutVector3(Position);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Position = reader.GetVector3();
    }
}