using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public enum Team { None, TeamA, TeamB }

public class PlayerTeam : NetworkBehaviour
{
    public readonly SyncVar<Team> team = new();

    private GameMode gameMode;
    private TeamManager teams;

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
        gameMode = FindObjectOfType<GameMode>();
        teams = gameMode != null ? gameMode.GetComponent<TeamManager>() : null;
    }

    [ServerRpc(RequireOwnership = true)]
    public void JoinTeam(Team desired)
    {
        if (teams == null) CacheModeRefs();
        if (teams == null) return;
        if (desired == Team.None) return;
        if (team.Value == desired) return;
        teams.Join(this, desired);
        team.Value = desired;
    }

    public void ServerSetTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (teams == null) CacheModeRefs();
        if (teams == null) return;
        if (desired == Team.None) return;
        teams.Join(this, desired);
        team.Value = desired;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (teams == null) CacheModeRefs();
        if (teams != null) teams.Leave(this);
    }
}
