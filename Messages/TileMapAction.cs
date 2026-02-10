
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Client;

public class TileMapAction : OwnedMessage
{
    public enum ActionType : byte
    {
        Dig,
        Undig,
        Water,
    }

    public override string MessageType => "Client.TileMapAction";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public Vector3 Position;
    public ActionType Action;

    public TileMapAction() { }

    public TileMapAction(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.PutVector3(Position);
        writer.Put((byte)Action);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        Position = reader.GetVector3();
        Action = (ActionType)reader.GetByte();
    }

    public override void Reset()
    {
        base.Reset();
        Position = default;
        Action = default;
    }
}
