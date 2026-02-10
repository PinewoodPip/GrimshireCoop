using System;
using System.Collections.Generic;
using GrimshireCoop.Messages;

namespace GrimshireCoop;

/// <summary>
/// Pooling system for net messages.
/// </summary>
public static class NetMessagePool
{
    private static readonly Dictionary<Type, Stack<Message>> Pools = [];

    public static T Get<T>() where T : Message, new()
    {
        return (T)Get(typeof(T));
    }

    /// <summary>
    /// Returns a free instance of a message.
    /// </summary>
    public static Message Get(Type messageType)
    {
        // Register the message type if it hasn't been registered yet
        if (!Pools.TryGetValue(messageType, out Stack<Message> pool))
        {
            pool = RegisterMessageType(messageType);
        }

        // Get a free message
        if (pool.Count > 0)
        {
            Message pooledMsg = pool.Pop();
            pooledMsg.Reset();
            return pooledMsg;
        }
        else
        {
            // Create new message
            Message newMsg = (Message)Activator.CreateInstance(messageType);
            newMsg.Reset();
            return newMsg;
        }
    }

    /// <summary>
    /// Marks a message as free and available for reuse by Get().
    /// A message should not be reused directly after being freed.
    /// </summary>
    public static void Release(Message msg)
    {
        // Ensure pool exists
        // This case should only occur if releasing a message that did not come from the pool
        Type messageType = msg.GetType();
        if (!Pools.TryGetValue(messageType, out Stack<Message> pool))
        {
            pool = RegisterMessageType(messageType);
        }

        msg.Reset();
        pool.Push(msg);
    }

    private static Stack<Message> RegisterMessageType(Type messageType)
    {
        if (!typeof(Message).IsAssignableFrom(messageType))
        {
            throw new ArgumentException($"Type {messageType.Name} does not inherit from Message.");
        }

        Pools[messageType] = [];

        return Pools[messageType];
    }
}
