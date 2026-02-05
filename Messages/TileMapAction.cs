using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Shared;

public class TileMapAction : OwnedMessage
{
    public enum ActionType : byte
    {
        Dig,
        Undig,
        Water,
    }

    public override string MessageType => "Shared.TileMapAction";

    public override Direction SyncDirection => Direction.ClientToServer;

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
}
