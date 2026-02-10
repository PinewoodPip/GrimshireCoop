
using LiteNetLib.Utils;

namespace GrimshireCoop.Messages;

public abstract class Message
{
    public enum Direction
    {
        ServerToClient,
        ClientToServer,
        ClientToPeers, // Sent from a client to all others (forward by server)
    }

    public abstract string MessageType { get; } // TODO build an index of these so we don't serialize strings with each message

    /// <summary>
    /// The flow direction of the message; ie. who sends it and to whom.
    /// </summary>
    public abstract Direction SyncDirection { get; }

    public virtual void Serialize(NetDataWriter writer)
    {
        writer.Put(MessageType);
    }

    public abstract void Deserialize(NetDataReader reader);

    /// <summary>
    /// Resets the message's fields to their default values.
    /// Intended to be used with pooling patterns.
    /// </summary>
    public virtual void Reset()
    {
        
    }
}