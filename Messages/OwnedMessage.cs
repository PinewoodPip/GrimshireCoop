
namespace GrimshireCoop.Network.Messages;

public abstract class OwnedMessage : Message
{
    public PeerId OwnerPeerId;

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