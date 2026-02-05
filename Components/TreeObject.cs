
using GrimshireCoop.Messages.Shared;
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
}