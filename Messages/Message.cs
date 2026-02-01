
using LiteNetLib.Utils;

public abstract class Message
{
    public enum Direction
    {
        ServerToClient,
        ClientToServer
    }

    public abstract string MessageType { get; }
    public abstract Direction SyncDirection { get; }

    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(MessageType);
    }

    public abstract void Deserialize(NetDataReader reader);
}