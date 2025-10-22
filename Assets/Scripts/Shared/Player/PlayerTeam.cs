using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public enum Team { None, TeamA, TeamB }

public class PlayerTeam : NetworkBehaviour
{
    public readonly SyncVar<Team> team = new();

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    [ServerRpc(RequireOwnership = true)]
    public void JoinTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (desired == Team.None) return;
        if (team.Value == desired) return;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desired);
    }

    public void ServerSetTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (desired == Team.None) return;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desired);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (MatchController.TryGet(out var match))
        {
            if (team.Value != Team.None)
            {
                var tm = match.GetComponent<TeamManager>();
                if (tm != null) tm.Leave(this);
            }
        }
    }
}
