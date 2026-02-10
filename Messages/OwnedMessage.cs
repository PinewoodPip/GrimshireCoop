
namespace GrimshireCoop.Messages;

public abstract class OwnedMessage : Message
{
    /// <summary>
    /// Whether this message is only relevant to clients within the same scene.
    /// Used to reduce bandwidth by avoiding forwarding messages to clients in other scenes.
    /// </summary>
    public abstract bool IsLocal { get; }

    /// <summary>
    /// The sender of the message.
    /// </summary>
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

    public override void Reset()
    {
        base.Reset();
        OwnerPeerId = default;
    }
}