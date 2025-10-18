using UnityEngine;
using FishNet.Object;

[DisallowMultipleComponent]
public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public static T Instance { get; private set; }

    protected virtual bool PersistWithDontDestroyOnLoad => false;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)(object)this;
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static bool TryGet(out T inst)
    {
        inst = Instance != null ? Instance : FindFirstObjectByType<T>();
        if (inst != null && Instance == null) Instance = inst;
        return inst != null;
    }
}
