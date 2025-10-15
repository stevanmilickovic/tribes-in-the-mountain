using UnityEngine;
using FishNet.Object;

public class GatewaySpawner : MonoBehaviour
{
    [SerializeField] private LobbySelectionGateway gatewayPrefab;
    private LobbySelectionGateway _spawned;

    private void Start()
    {
        var nm = FishNet.InstanceFinder.NetworkManager;
        if (nm == null) return;
        nm.ServerManager.OnServerConnectionState += (t) =>
        {
            if (nm.ServerManager.Started && _spawned == null)
            {
                _spawned = Instantiate(gatewayPrefab);
                nm.ServerManager.Spawn(_spawned.gameObject);
            }
        };
    }
}
