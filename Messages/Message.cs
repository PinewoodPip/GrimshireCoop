
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages;

public abstract class Message
{
    public enum Direction
    {
        ServerToClient,
        ClientToServer
    }

    public abstract string MessageType { get; } // TODO build an index of these so we don't serialize strings with each message
    public abstract Direction SyncDirection { get; }

    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(MessageType);
    }

    public abstract void Deserialize(NetDataReader reader);
}