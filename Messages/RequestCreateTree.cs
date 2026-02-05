using GrimshireCoop.Network.Messages;
using LiteNetLib.Utils;
using UnityEngine;
using static Message;

namespace GrimshireCoop.Messages.Shared;

public class RequestCreateTree : NetObjectMessage // TODO these need to be replicated from host => client when client enters hosts's scene
{
    public override string MessageType => "Shared.RequestCreateTree";

    public override Direction SyncDirection => Direction.ClientToServer;

    public PersistentTreeData TreeData;

    public RequestCreateTree() { }

    public RequestCreateTree(NetDataReader reader) => Deserialize(reader);

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        SerializeTreeData(writer, TreeData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        TreeData = DeserializeTreeData(reader);
    }

    private static void SerializeTreeData(NetDataWriter writer, PersistentTreeData data)
    {
        writer.Put(data.objID);
        writer.Put(data.treeDataRefID);
        Debug.Log($"Serializing TreeData with refID {data.treeDataRefID}");
        writer.Put(data.daysOld);
        writer.Put(data.daysSincePickedFruit);
        writer.Put(data.nameOfScene);
        writer.Put(data.posX);
        writer.Put(data.posY);
        writer.Put(data.shookToday);
        writer.Put(data.isJustAStump);
        writer.Put(data.regrowingTree);
        writer.Put(data.isABush);
        writer.Put(data.isDead);
        writer.Put(data.serializedFruitData);

        // Serialize fruitsList as a jagged array
        if (data.fruitsList != null)
        {
            writer.Put(true);
            writer.Put(data.fruitsList.GetLength(0));
            writer.Put(data.fruitsList.GetLength(1));
            for (int i = 0; i < data.fruitsList.GetLength(0); i++)
            {
                for (int j = 0; j < data.fruitsList.GetLength(1); j++)
                {
                    writer.Put(data.fruitsList[i, j]);
                }
            }
        }
        else
        {
            writer.Put(false);
        }
    }

    private static PersistentTreeData DeserializeTreeData(NetDataReader reader)
    {
        PersistentTreeData data = new();
        data.objID = reader.GetInt();
        data.treeDataRefID = reader.GetInt();
        Debug.Log($"Deserializing TreeData with refID {data.treeDataRefID}");
        data.daysOld = reader.GetInt();
        data.daysSincePickedFruit = reader.GetInt();
        data.nameOfScene = reader.GetString();
        data.posX = reader.GetFloat();
        data.posY = reader.GetFloat();
        data.shookToday = reader.GetBool();
        data.isJustAStump = reader.GetBool();
        data.regrowingTree = reader.GetBool();
        data.isABush = reader.GetBool();
        data.isDead = reader.GetBool();
        data.serializedFruitData = reader.GetString();

        // Deserialize fruitsList
        bool hasFruitsList = reader.GetBool();
        if (hasFruitsList)
        {
            int rows = reader.GetInt();
            int cols = reader.GetInt();
            data.fruitsList = new float[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    data.fruitsList[i, j] = reader.GetFloat();
                }
            }
        }

        return data;
    }
}
