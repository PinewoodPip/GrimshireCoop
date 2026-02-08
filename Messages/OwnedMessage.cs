
namespace GrimshireCoop.Messages;

public abstract class OwnedMessage : Message
{
    public PeerId OwnerPeerId; // The sender of the message.

    public override void Serialize(LiteNetLib.Utils.NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(OwnerPeerId);
    }

    public override void Deserialize(LiteNetLib.Utils.NetDataReader reader)
    {
        OwnerPeerId = reader.GetInt();
    }
}