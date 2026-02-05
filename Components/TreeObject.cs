
using GrimshireCoop.Messages.Shared;
using LiteNetLib.Utils;
using UnityEngine;

namespace GrimshireCoop.Components;

public class TreeObject : NetworkedBehaviour
{
    public override string NetTypeID => "TreeObject";

    public global::TreeObject Tree => GetComponent<global::TreeObject>(); // TODO cache

    public override void OnAction(ObjectAction action)
    {
        if (action.Action == "UseAxe")
        {
            TreeManager.ignoreHooks = true;
            Tree.UseAxe(1, 0); // TODO this has side effects on player; replace!
            TreeManager.ignoreHooks = false;
        }
    }

    public override byte[] GetReplicationData()
    {
        NetDataWriter writer = new NetDataWriter();
        PersistentTreeData data = Tree.PTreeDataContainer;
        RequestCreateTree.SerializeTreeData(writer, data);
        return writer.CopyData();
    }

    public override void ApplyReplicationData(byte[] data)
    {
        NetDataReader reader = new NetDataReader(data);
        PersistentTreeData treeData = RequestCreateTree.DeserializeTreeData(reader);
        Tree.PTreeDataContainer = treeData;
    }
}