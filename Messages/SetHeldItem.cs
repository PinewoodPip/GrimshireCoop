using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Shared;

public class SetHeldItem : NetObjectMessage
{
    public override string MessageType => "Shared.SetHeldItem";
    public override Direction SyncDirection => Direction.ClientToServer;

    public int ItemId; // -1 for no item.

    public SetHeldItem() { }

    public SetHeldItem(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(ItemId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        ItemId = reader.GetInt();
    }
}
