
using LiteNetLib.Utils;

namespace GrimshireCoop.Network.Messages;

public abstract class NetObjectMessage : OwnedMessage
{
    public int NetId;

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.Put(NetId);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        NetId = reader.GetInt();
    }
}
