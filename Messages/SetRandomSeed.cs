
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Messages.Host;

public class SetRandomSeed : OwnedMessage
{
    public override string MessageType => "Host.SetRandomSeed";

    public override Direction SyncDirection => Direction.ServerToClient;

    public Random.State RandomState;

    public SetRandomSeed() { }

    public SetRandomSeed(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        writer.PutRandomState(RandomState);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        RandomState = reader.GetRandomState();
    }
}
