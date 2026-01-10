using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerTeam : NetworkBehaviour
{
    public readonly SyncVar<string> teamId = new(TeamDatabase.NeutralId);

    private readonly SyncVar<int> preferredSpawnZoneIndex = new(-1);
    public int PreferredSpawnZoneIndex => preferredSpawnZoneIndex.Value;

    public string TeamId => teamId.Value;

    public override void OnStartServer()
    {
        base.OnStartServer();
        preferredSpawnZoneIndex.Value = -1;
        teamId.Value = TeamDatabase.NeutralId;
    }

    [ServerRpc(RequireOwnership = true)]
    public void JoinTeam(string desiredTeamId)
    {
        if (!IsServerInitialized) return;
        if (string.IsNullOrWhiteSpace(desiredTeamId)) return;
        if (desiredTeamId == TeamDatabase.NeutralId) return;
        if (teamId.Value == desiredTeamId) return;

        var db = TeamDatabase.Instance;
        if (db == null || !db.IsValidTeamId(desiredTeamId)) return;

        preferredSpawnZoneIndex.Value = -1;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desiredTeamId);
    }

    public void ServerSetTeamId(string desiredTeamId)
    {
        if (!IsServerInitialized) return;
        if (string.IsNullOrWhiteSpace(desiredTeamId)) return;
        if (desiredTeamId == TeamDatabase.NeutralId) return;

        var db = TeamDatabase.Instance;
        if (db == null || !db.IsValidTeamId(desiredTeamId)) return;

        preferredSpawnZoneIndex.Value = -1;

        if (MatchController.TryGet(out var match))
            match.ServerJoinTeam(this, desiredTeamId);
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

        if (string.IsNullOrWhiteSpace(teamId.Value) || teamId.Value == TeamDatabase.NeutralId) return;
        if (z.TeamOwnerId != teamId.Value) return;

        var spawns = z.Spawns;
        if (spawns == null || spawns.Length == 0) return;

        preferredSpawnZoneIndex.Value = zoneIndex;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (MatchController.TryGet(out var match))
        {
            if (!string.IsNullOrWhiteSpace(teamId.Value) && teamId.Value != TeamDatabase.NeutralId)
            {
                var tm = match.GetComponent<TeamManager>();
                if (tm != null) tm.Leave(this);
            }
        }
    }
}
