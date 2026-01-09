using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Connection;

public class LobbySelectionGateway : NetworkSingleton<LobbySelectionGateway>
{
    [SerializeField] private GameObject teamAPlayerPrefab;
    [SerializeField] private GameObject teamBPlayerPrefab;

    private readonly Dictionary<NetworkConnection, Team> _pending = new();
    private readonly Dictionary<NetworkConnection, PlayerTeam> _spawnedPlayers = new();

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
        _spawnedPlayers.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitTeamChoice(Team team, NetworkConnection conn = null)
    {
        if (!IsServerStarted) return;
        if (conn == null) return;
        if (team == Team.None) return;

        if (match == null) return;
        if (!match.CanJoinTeam(team)) return;

        _pending[conn] = team;
        TrySpawnFor(conn);
    }

    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
            return;

        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            _pending.Remove(conn);

            if (_spawnedPlayers.TryGetValue(conn, out var pt))
            {
                if (match != null)
                    match.ServerOnPlayerDisconnected(pt);

                if (pt != null && pt.gameObject != null && NetworkManager != null)
                    NetworkManager.ServerManager.Despawn(pt.gameObject);

                _spawnedPlayers.Remove(conn);
            }
        }
    }

    private void TrySpawnFor(NetworkConnection conn)
    {
        if (!IsServerStarted) return;
        if (conn == null) return;

        if (!_pending.TryGetValue(conn, out var team))
            return;

        if (match == null) return;
        if (!match.ServerCanTeamSpawn(team)) return;

        Transform sp = match.GetSpawnForTeam(team);

        GameObject playerPrefab = (team == Team.TeamA) ? teamAPlayerPrefab : teamBPlayerPrefab;
        if (playerPrefab == null) return;

        GameObject go = (sp != null)
            ? Instantiate(playerPrefab, sp.position, sp.rotation)
            : Instantiate(playerPrefab);

        NetworkManager.ServerManager.Spawn(go, conn);

        var pt = go.GetComponent<PlayerTeam>();
        if (pt != null)
        {
            _spawnedPlayers[conn] = pt;
            match.ServerJoinTeam(pt, team);

            match.ServerOnPlayerSpawned(pt, consumeReserve: false);
        }

        _pending.Remove(conn);
    }
}
