
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class SetHeldItem : NetObjectMessage
{
    public override string MessageType => "Client.SetHeldItem";
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
