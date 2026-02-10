
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class SetHeldItem : NetObjectMessage
{
    public override string MessageType => "Client.SetHeldItem";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

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

    public override void Reset()
    {
        base.Reset();
        ItemId = default;
    }
}
