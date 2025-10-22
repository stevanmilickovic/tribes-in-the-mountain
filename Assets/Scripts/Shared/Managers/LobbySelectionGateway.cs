using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Connection;

public class LobbySelectionGateway : NetworkSingleton<LobbySelectionGateway>
{
    [SerializeField] private GameObject playerPrefab;

    private readonly Dictionary<NetworkConnection, Team> _pending = new Dictionary<NetworkConnection, Team>();

    private MatchController match => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (NetworkManager != null)
            NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (NetworkManager != null)
            NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        _pending.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitTeamChoice(Team team, NetworkConnection conn = null)
    {
        if (!IsServerStarted) return;
        if (team == Team.None || conn == null) return;

        _pending[conn] = team;
        TrySpawnFor(conn);
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started) return;
        if (args.ConnectionState == RemoteConnectionState.Stopped)
            _pending.Remove(conn);
    }

    private void TrySpawnFor(NetworkConnection conn)
    {
        if (!IsServerStarted) return;
        if (!_pending.TryGetValue(conn, out var team)) return;
        if (playerPrefab == null) return;

        if (!match.ServerCanTeamSpawn(team)) return;

        Transform sp = match.GetSpawnForTeam(team);

        GameObject go = (sp != null)
            ? Instantiate(playerPrefab, sp.position, sp.rotation)
            : Instantiate(playerPrefab);

        base.NetworkManager.ServerManager.Spawn(go, conn);

        var pt = go.GetComponent<PlayerTeam>();
        if (pt != null)
        {
            match.ServerJoinTeam(pt, team);
            match.ServerOnPlayerSpawned(pt);
        }

        _pending.Remove(conn);
    }
}
