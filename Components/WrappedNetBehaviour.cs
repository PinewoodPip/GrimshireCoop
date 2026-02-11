
using UnityEngine;

namespace GrimshireCoop.Components;

public abstract class WrappedNetBehaviour<T> : NetBehaviour where T : MonoBehaviour
{
    public T WrappedComponent {
        get
        {
            if (cachedWrappedComponent == null)
            {
                cachedWrappedComponent = GetComponent<T>();
            }
            return cachedWrappedComponent;
        }
    }

    private T cachedWrappedComponent;
}