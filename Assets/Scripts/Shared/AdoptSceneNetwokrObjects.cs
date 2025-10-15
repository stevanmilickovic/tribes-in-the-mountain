using UnityEngine;
using FishNet;
using FishNet.Object;

public class AdoptSceneNetworkObjects : MonoBehaviour
{
    [SerializeField] private NetworkObject[] targets;
    private bool _done;

    private void Update()
    {
        var nm = InstanceFinder.NetworkManager;
        if (_done || nm == null) return;
        if (!nm.ServerManager.Started) return;

        for (int i = 0; i < targets.Length; i++)
        {
            var no = targets[i];
            if (no != null && !no.IsSpawned)
                nm.ServerManager.Spawn(no.gameObject);
        }
        _done = true;
    }
}
