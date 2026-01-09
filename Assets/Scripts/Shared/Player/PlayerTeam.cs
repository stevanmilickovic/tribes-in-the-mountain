using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public enum Team { None, TeamA, TeamB }

public class PlayerTeam : NetworkBehaviour
{
    public readonly SyncVar<Team> team = new();

    private readonly SyncVar<int> preferredSpawnZoneIndex = new(-1);
    public int PreferredSpawnZoneIndex => preferredSpawnZoneIndex.Value;

    public override void OnStartServer()
    {
        base.OnStartServer();
        preferredSpawnZoneIndex.Value = -1;
    }

    [ServerRpc(RequireOwnership = true)]
    public void JoinTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (desired == Team.None) return;
        if (team.Value == desired) return;

        preferredSpawnZoneIndex.Value = -1;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desired);
    }

    public void ServerSetTeam(Team desired)
    {
        if (!IsServerInitialized) return;
        if (desired == Team.None) return;

        preferredSpawnZoneIndex.Value = -1;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desired);
    }

    [ServerRpc(RequireOwnership = true)]
    public void SetPreferredSpawnZone(int zoneIndex)
    {
        if (!IsServerInitialized) return;

        if (zoneIndex < 0)
        {
            preferredSpawnZoneIndex.Value = -1;
            return;
        }

        var match = MatchController.Instance;
        if (match == null) return;
        if (match.zones == null) return;
        if (zoneIndex >= match.zones.Length) return;

        var z = match.zones[zoneIndex];
        if (z == null) return;

        if (team.Value == Team.None) return;
        if (z.Owner != team.Value) return;

        var spawns = z.Spawns;
        if (spawns == null || spawns.Length == 0) return;

        preferredSpawnZoneIndex.Value = zoneIndex;
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
