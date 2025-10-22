using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections;
using FishNet.Component.Transforming;

public enum MatchState { PreRound, Live, PostRound }

[DisallowMultipleComponent]
public class MatchController : NetworkSingleton<MatchController>
{
    [Header("Spawns")]
    [SerializeField] private Transform teamASpawn;
    [SerializeField] private Transform teamBSpawn;

    [Header("Managers (auto-cached)")]
    [SerializeField] private TeamManager teamManager;

    [Header("Match Config")]
    [SerializeField] private int roundSeconds = 600;
    [SerializeField] private int intermissionSeconds = 5;
    [SerializeField] private int startingReservesTeamA = 30;
    [SerializeField] private int startingReservesTeamB = 30;

    private readonly SyncVar<int> remainingSeconds = new();
    private readonly SyncVar<int> teamACount = new();
    private readonly SyncVar<int> teamBCount = new();

    private readonly SyncVar<int> reservesA = new();
    private readonly SyncVar<int> reservesB = new();
    private readonly SyncVar<int> aliveA = new();
    private readonly SyncVar<int> aliveB = new();

    private readonly SyncVar<MatchState> state = new();

    public int RemainingSeconds => remainingSeconds.Value;
    public int TeamACount => teamACount.Value;
    public int TeamBCount => teamBCount.Value;
    public int ReservesA => reservesA.Value;
    public int ReservesB => reservesB.Value;
    public int AliveA => aliveA.Value;
    public int AliveB => aliveB.Value;
    public MatchState State => state.Value;

    private float _accum;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (teamManager == null)
            teamManager = GetComponent<TeamManager>() ?? gameObject.AddComponent<TeamManager>();

        state.Value = MatchState.Live;
        remainingSeconds.Value = Mathf.Max(1, roundSeconds);

        PushCountsImmediate();
        reservesA.Value = Mathf.Max(0, startingReservesTeamA);
        reservesB.Value = Mathf.Max(0, startingReservesTeamB);

        aliveA.Value = 0;
        aliveB.Value = 0;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (state.Value != MatchState.Live) return;

        _accum += Time.deltaTime;
        if (_accum >= 1f)
        {
            _accum -= 1f;
            remainingSeconds.Value -= 1;
            if (remainingSeconds.Value <= 0)
                remainingSeconds.Value = Mathf.Max(1, roundSeconds);
        }

        PushCountsIfChanged();
    }

    public void ServerJoinTeam(PlayerTeam player, Team desired)
    {
        if (!IsServerInitialized || player == null || desired == Team.None) return;
        EnsureManagers();
        teamManager.Join(player, desired);
        player.team.Value = desired;
        PushCountsIfChanged();
    }

    public bool ServerCanTeamSpawn(Team team)
    {
        if (!IsServerInitialized) return false;
        if (state.Value != MatchState.Live) return false;
        return team switch
        {
            Team.TeamA => reservesA.Value > 0,
            Team.TeamB => reservesB.Value > 0,
            _ => false
        };
    }

    public Transform GetSpawnForTeam(Team team)
    {
        if (team == Team.TeamA) return teamASpawn;
        if (team == Team.TeamB) return teamBSpawn;
        return null;
    }

    public void ServerOnPlayerSpawned(PlayerTeam player, bool consumeReserve = true)
    {
        if (!IsServerInitialized || player == null) return;

        switch (player.team.Value)
        {
            case Team.TeamA:
                if (consumeReserve && reservesA.Value > 0) reservesA.Value -= 1;
                aliveA.Value = Mathf.Max(0, aliveA.Value + 1);
                break;
            case Team.TeamB:
                if (consumeReserve && reservesB.Value > 0) reservesB.Value -= 1;
                aliveB.Value = Mathf.Max(0, aliveB.Value + 1);
                break;
        }

        PushCountsImmediate();
        CheckEliminationWin();
    }

    public void ServerOnPlayerDied(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;

        switch (player.team.Value)
        {
            case Team.TeamA: aliveA.Value = Mathf.Max(0, aliveA.Value - 1); break;
            case Team.TeamB: aliveB.Value = Mathf.Max(0, aliveB.Value - 1); break;
        }

        PushCountsImmediate();

        CheckEliminationWin();
    }

    private void EnsureManagers()
    {
        if (teamManager == null)
            teamManager = GetComponent<TeamManager>() ?? gameObject.AddComponent<TeamManager>();
    }

    private void PushCountsImmediate()
    {
        var (a, b) = teamManager != null ? teamManager.GetCounts() : (0, 0);
        teamACount.Value = a;
        teamBCount.Value = b;
    }

    private void PushCountsIfChanged()
    {
        var (a, b) = teamManager != null ? teamManager.GetCounts() : (0, 0);
        if (teamACount.Value != a) teamACount.Value = a;
        if (teamBCount.Value != b) teamBCount.Value = b;
    }

    private void CheckEliminationWin()
    {
        bool aEliminated = (reservesA.Value <= 0 && aliveA.Value <= 0);
        bool bEliminated = (reservesB.Value <= 0 && aliveB.Value <= 0);

        if (aEliminated && bEliminated)
        {
            EndMatch(Team.None);
        }
        else if (aEliminated)
        {
            EndMatch(Team.TeamB);
        }
        else if (bEliminated)
        {
            EndMatch(Team.TeamA);
        }
    }

    private void EndMatch(Team winner)
    {
        if (state.Value == MatchState.PostRound) return;

        state.Value = MatchState.PostRound;
        Rpc_OnMatchEnded(winner);

        StartCoroutine(IntermissionThenRestart());
    }

    private IEnumerator IntermissionThenRestart()
    {
        FreezeAllPlayers();
        yield return new WaitForSeconds(intermissionSeconds);
        StartNewRound();
    }

    private void FreezeAllPlayers()
    {
        var players = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ph in players)
            ph.ServerForceAlive(false);
    }

    private void StartNewRound()
    {
        state.Value = MatchState.Live;
        remainingSeconds.Value = Mathf.Max(1, roundSeconds);
        reservesA.Value = Mathf.Max(0, startingReservesTeamA);
        reservesB.Value = Mathf.Max(0, startingReservesTeamB);
        aliveA.Value = 0;
        aliveB.Value = 0;

        var teams = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pt in teams)
        {
            var spawn = GetSpawnForTeam(pt.team.Value);
            if (spawn == null) continue;

            var pos = spawn.position;
            var rot = spawn.rotation;

            var nt = pt.GetComponent<NetworkTransform>();
            if (nt != null)
            {
                pt.transform.SetPositionAndRotation(pos, rot);
                nt.Teleport();
            }
            else
            {
                pt.transform.SetPositionAndRotation(pos, rot);
            }

            var ph = pt.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.ServerRestoreFull();
            }

            ServerOnPlayerSpawned(pt, consumeReserve: false);
        }
    }


    [ObserversRpc(BufferLast = true)]
    private void Rpc_OnMatchEnded(Team winner)
    {
        // TODO: hook UI — show winner or draw.
    }
}
