using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public enum Team { None, TeamA, TeamB }

public class PlayerTeam : NetworkBehaviour
{
    public readonly SyncVar<Team> team = new();

    private GameMode _gameMode;
    private TeamManager _teams;

    public override void OnStartServer()
    {
        base.OnStartServer();
        CacheModeRefs();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerInitialized) CacheModeRefs();
    }

    private void CacheModeRefs()
    {
        _gameMode = FindObjectOfType<GameMode>();
        _teams = _gameMode != null ? _gameMode.GetComponent<TeamManager>() : null;
    }

    [ServerRpc(RequireOwnership = true)]
    public void JoinTeam(Team desired)
    {
        if (_teams == null) CacheModeRefs();
        if (_teams == null) return;
        if (desired == Team.None) return;
        if (team.Value == desired) return;
        _teams.Join(this, desired);
        team.Value = desired;
    }

    public void ServerSetTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (_teams == null) CacheModeRefs();
        if (_teams == null) return;
        if (desired == Team.None) return;
        _teams.Join(this, desired);
        team.Value = desired;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (_teams == null) CacheModeRefs();
        if (_teams != null) _teams.Leave(this);
    }
}
