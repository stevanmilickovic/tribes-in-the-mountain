using UnityEngine;
using FishNet.Object;

public class LobbySpawnProvider : MonoBehaviour, ITeamSpawnProvider
{
    [SerializeField] private LobbySelectionGateway gateway;

    public Transform GetSpawn(Team team, NetworkObject player)
    {
        return gateway != null
            ? gateway.GetSpawnForTeam(team)
            : null;
    }
}