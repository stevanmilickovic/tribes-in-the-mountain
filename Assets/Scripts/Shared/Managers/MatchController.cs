using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchState { PreRound, Live, PostRound }

[DisallowMultipleComponent]
public class MatchController : NetworkSingleton<MatchController>
{
    [Header("Spawns")]
    [SerializeField] private Transform[] teamASpawns;
    [SerializeField] private Transform[] teamBSpawns;

    private int _aIndex = 0;
    private int _bIndex = 0;

    [Header("Capture Zones")]
    [SerializeField] private CaptureZone captureZone;

    [Header("Managers (auto-cached)")]
    [SerializeField] private TeamManager teamManager;

    [Header("Match Config")]
    [SerializeField] private int roundSeconds = 600;
    [SerializeField] private int intermissionSeconds = 5;
    [SerializeField] private int startingReservesTeamA = 30;
    [SerializeField] private int startingReservesTeamB = 30;

    [Header("Corpse Prefabs")]
    [SerializeField] private GameObject montenegrinCorpsePrefab;
    [SerializeField] private GameObject ottomanCorpsePrefab;

    public HUDController hud;

    private readonly SyncVar<int> remainingSeconds = new();
    private readonly SyncVar<int> teamACount = new();
    private readonly SyncVar<int> teamBCount = new();

    private readonly SyncVar<int> reservesA = new();
    private readonly SyncVar<int> reservesB = new();
    private readonly SyncVar<int> aliveA = new();
    private readonly SyncVar<int> aliveB = new();

    private readonly SyncVar<MatchState> state = new();

    private readonly List<GameObject> corpses = new();

    public int RemainingSeconds => remainingSeconds.Value;
    public int TeamACount => teamACount.Value;
    public int TeamBCount => teamBCount.Value;
    public int ReservesA => reservesA.Value;
    public int ReservesB => reservesB.Value;
    public int AliveA => aliveA.Value;
    public int AliveB => aliveB.Value;
    public MatchState State => state.Value;

    private float _accum;

    [SerializeField] private int maxTeamA = 10;
    [SerializeField] private int maxTeamB = 10;

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

    public bool CanJoinTeam(Team team)
    {
        return team switch
        {
            Team.TeamA => TeamACount < maxTeamA,
            Team.TeamB => TeamBCount < maxTeamB,
            _ => false
        };
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
        if (team == Team.None)
            return null;

        Transform[] arr = (team == Team.TeamA) ? teamASpawns : teamBSpawns;

        if (arr == null || arr.Length == 0)
            return null;

        if (team == Team.TeamA)
        {
            Transform t = arr[_aIndex % arr.Length];
            _aIndex++;
            return t;
        }
        else
        {
            Transform t = arr[_bIndex % arr.Length];
            _bIndex++;
            return t;
        }
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

    public void ServerOnZoneCaptured(Team capturingTeam)
    {
        if (!IsServerInitialized || state.Value != MatchState.Live) return;
        Debug.Log($"Zone captured by {capturingTeam}, ending match.");
        EndMatch(capturingTeam);
    }

    private void EndMatch(Team winner)
    {
        if (state.Value == MatchState.PostRound) return;

        foreach (var ph in FindObjectsOfType<PlayerHealth>())
            ph.CancelRespawn();

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
        Rpc_StartNewRound();

        Rpc_ClearCorpses();

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

            var ph = pt.GetComponent<PlayerHealth>();
            ph.ServerForceAlive(true);
            ph.ServerRestoreFull();
            ph.EnableRespawn();

            pt.GetComponent<PlayerMotor>().Teleport(pos, rot);

            ServerOnPlayerSpawned(pt, consumeReserve: false);
        }

        if (captureZone != null)
        {
            captureZone.ResetZone();
        }
    }

    public void ServerOnPlayerDisconnected(PlayerTeam pt)
    {
        if (!IsServerInitialized || pt == null) return;

        switch (pt.team.Value)
        {
            case Team.TeamA:
                aliveA.Value = Mathf.Max(0, aliveA.Value - 1);
                reservesA.Value += 1;
                break;

            case Team.TeamB:
                aliveB.Value = Mathf.Max(0, aliveB.Value - 1);
                reservesB.Value += 1;
                break;
        }

        PushCountsImmediate();
        CheckEliminationWin();
    }

    [Server]
    public void SpawnCorpseFor(PlayerHealth ph)
    {
        if (ph == null) return;

        var pt = ph.GetComponent<PlayerTeam>();
        var animDriver = ph.GetComponent<PlayerAnimationDriver>();

        if (pt == null || animDriver == null) return;

        Team team = pt.team.Value;
        Vector3 pos = ph.transform.position;
        Quaternion rot = ph.transform.rotation;
        string deathAnim = animDriver.lastDeathAnim;

        RpcSpawnCorpse(team, pos, rot, deathAnim);
    }


    [ObserversRpc(BufferLast = false)]
    private void RpcSpawnCorpse(Team team, Vector3 pos, Quaternion rot, string deathAnim)
    {
        GameObject prefab = null;

        if (team == Team.TeamA)
            prefab = montenegrinCorpsePrefab;
        else if (team == Team.TeamB)
            prefab = ottomanCorpsePrefab;

        if (prefab == null) return;

        GameObject corpse = Instantiate(prefab, pos, rot);
        corpse.transform.SetParent(null, true);

        Animator anim = corpse.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Play(deathAnim, 0, 1f);
            anim.Update(0f);
        }

        corpses.Add(corpse);
    }

    [ObserversRpc(BufferLast = false)]
    private void Rpc_ClearCorpses()
    {
        foreach (var c in corpses)
        {
            if (c != null)
                Destroy(c);
        }
        corpses.Clear();
    }


    [ObserversRpc]
    private void Rpc_OnMatchEnded(Team winner)
    {
        if (hud != null)
            hud.ShowVictory(winner);
    }

    [ObserversRpc]
    private void Rpc_StartNewRound()
    {
        if (hud != null)
            hud.HideVictory();
    }
}
