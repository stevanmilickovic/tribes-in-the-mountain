using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Connection;

public class LobbySelectionGateway : NetworkSingleton<LobbySelectionGateway>
{
    [SerializeField] private GameObject playerPrefab;

    private readonly Dictionary<NetworkConnection, string> _pending = new();
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
    public void SubmitTeamChoice(string teamId, NetworkConnection conn = null)
    {
        if (!IsServerStarted) return;
        if (conn == null) return;
        if (string.IsNullOrWhiteSpace(teamId)) return;
        if (teamId == TeamDatabase.NeutralId) return;

        var db = TeamDatabase.Instance;
        if (db == null || !db.IsValidTeamId(teamId)) return;

        if (match == null) return;
        if (!match.CanJoinTeam(teamId)) return;

        if (_spawnedPlayers.TryGetValue(conn, out var existing) && existing != null)
        {
            existing.ServerSetTeamId(teamId);
            return;
        }

        _pending[conn] = teamId;
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

        if (!_pending.TryGetValue(conn, out var teamId))
            return;

        if (match == null) return;
        if (!match.ServerCanTeamSpawn(teamId)) return;

        if (playerPrefab == null) return;

        Transform sp = match.GetSpawnForTeam(teamId);

        GameObject go = (sp != null)
            ? Instantiate(playerPrefab, sp.position, sp.rotation)
            : Instantiate(playerPrefab);

        NetworkManager.ServerManager.Spawn(go, conn);

        var pt = go.GetComponent<PlayerTeam>();
        if (pt != null)
        {
            _spawnedPlayers[conn] = pt;
            match.ServerJoinTeam(pt, teamId);
            match.ServerOnPlayerSpawned(pt, consumeReserve: false);
        }

        _pending.Remove(conn);
    }
}
