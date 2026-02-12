
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages.Client;

public class PickupItem : NetObjectMessage
{
    public override string MessageType => "Client.PickupItem";
    public override Direction SyncDirection => Direction.ClientToPeers;
    public override bool IsLocal => true;

    public PeerId PickerPeerId; // The peer that will pickup the item; won't be the message owner, as item pickups are approved by the netobject owner but performed by the picker

    public PickupItem() { }

    public PickupItem(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(PickerPeerId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        PickerPeerId = reader.GetInt();
    }

    public override void Reset()
    {
        base.Reset();
        PickerPeerId = default;
    }
}
