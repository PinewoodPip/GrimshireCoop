
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages;

public abstract class NetObjectMessage : OwnedMessage
{
    public NetId NetId;

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
