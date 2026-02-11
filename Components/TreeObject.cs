
using GrimshireCoop.Messages.Client;
using LiteNetLib.Utils;
using UnityEngine;
using static GrimshireCoop.Utils;

namespace GrimshireCoop.Components;

public class NetTreeObject : WrappedNetBehaviour<TreeObject>
{
    public override string NetTypeID => "TreeObject";

    public TreeObject Tree => WrappedComponent;

    public override void OnAction(ObjectAction action)
    {
        if (action.Action == "UseAxe")
        {
            NetTreeManager.ignoreHooks = true;
            Tree.UseAxe(1, 0); // TODO this has side effects on player; replace!
            NetTreeManager.ignoreHooks = false;
        }
    }

    public override byte[] GetReplicationData()
    {
        NetDataWriter writer = new NetDataWriter();
        PersistentTreeData data = Tree.PTreeDataContainer;
        SerializeTreeData(writer, data);
        return writer.CopyData();
    }

    public override void ApplyReplicationData(byte[] data)
    {
        NetDataReader reader = new NetDataReader(data);
        PersistentTreeData treeData = DeserializeTreeData(reader);
        Tree.PTreeDataContainer = treeData;
    }

    public static void SerializeTreeData(NetDataWriter writer, PersistentTreeData data)
    {
        writer.Put(data.objID);
        writer.Put(data.treeDataRefID);
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

    public static PersistentTreeData DeserializeTreeData(NetDataReader reader)
    {
        PersistentTreeData data = new();
        data.objID = reader.GetInt();
        data.treeDataRefID = reader.GetInt();
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

    public static NetTreeObject Instantiate()
    {
        PersistentTreeManager NetTreeManager = GameObject.FindObjectOfType<PersistentTreeManager>(); // TODO cache all these singletons that the game does not
        GameObject prefab = GetField<GameObject>(NetTreeManager, "treeObjPrefab");
        GameObject instance = GameObject.Instantiate(prefab);
        return instance.AddComponent<NetTreeObject>();
    }
}