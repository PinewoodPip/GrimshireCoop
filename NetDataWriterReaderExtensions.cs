using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop;

public static class NetDataWriterReaderExtensions
{
    public static void PutVector2(this NetDataWriter writer, Vector2 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
    }

    public static Vector2 GetVector2(this NetDataReader reader)
    {
        return new Vector2(reader.GetFloat(), reader.GetFloat());
    }

    public static void PutVector3(this NetDataWriter writer, Vector3 vector)
    {
        writer.Put(vector.x);
        writer.Put(vector.y);
        writer.Put(vector.z);
    }

    public static Vector3 GetVector3(this NetDataReader reader)
    {
        return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
    }

    public static void PutDict<K, V>(this NetDataWriter writer, Dictionary<K, V> dict) where K : struct, INetSerializable where V : struct, INetSerializable
    {
        writer.Put(dict.Count);
        foreach (var kvp in dict)
        {
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }

    public static Dictionary<K, V> GetDict<K, V>(this NetDataReader reader) where K : struct, INetSerializable where V : struct, INetSerializable
    {
        int count = reader.GetInt();
        var dict = new Dictionary<K, V>();
        for (int i = 0; i < count; i++)
        {
            K key = reader.Get<K>();
            V value = reader.Get<V>();
            dict[key] = value;
        }
        return dict;
    }

    public static void PutDict(this NetDataWriter writer, Dictionary<int, string> dict)
    {
        writer.Put(dict.Count);
        foreach (var kvp in dict)
        {
            writer.Put(kvp.Key);
            writer.Put(kvp.Value);
        }
    }

    public static Dictionary<int, string> GetDict(this NetDataReader reader)
    {
        int count = reader.GetInt();
        var dict = new Dictionary<int, string>();
        for (int i = 0; i < count; i++)
        {
            int key = reader.GetInt();
            string value = reader.GetString();
            dict[key] = value;
        }
        return dict;
    }

    public static void PutRandomState(this NetDataWriter writer, Random.State state)
    {
        writer.Put(state.s0);
        writer.Put(state.s1);
        writer.Put(state.s2);
        writer.Put(state.s3);
    }

    public static Random.State GetRandomState(this NetDataReader reader)
    {
        Random.State state = new()
        {
            s0 = reader.GetInt(),
            s1 = reader.GetInt(),
            s2 = reader.GetInt(),
            s3 = reader.GetInt()
        };
        return state;
    }
}