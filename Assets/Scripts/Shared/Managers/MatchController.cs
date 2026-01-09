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
    [SerializeField] private int maxTeamA = 10;
    [SerializeField] private int maxTeamB = 10;

    [Header("Managers (auto-cached)")]
    [SerializeField] private TeamManager teamManager;

    [Header("Match Config")]
    [SerializeField] private int roundSeconds = 600;
    [SerializeField] private int intermissionSeconds = 5;

    [Header("Reserves")]
    [SerializeField] private int startingReservesTeamA = 30;
    [SerializeField] private int startingReservesTeamB = 30;

    [Header("Bleed")]
    [SerializeField] private bool includeUncapturableZonesInBleed = false;
    [SerializeField] private float bleedReservesPerSecondPerZoneDiff = 0.2f;

    [Header("Corpse Prefabs")]
    [SerializeField] private GameObject montenegrinCorpsePrefab;
    [SerializeField] private GameObject ottomanCorpsePrefab;

    [Header("Capture Zones")]
    [SerializeField] public CaptureZone[] zones;

    private Team[] zoneOwners;
    private int[] zoneSpawnIndices;

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

    private float _secondAccum;
    private float _bleedFracA;
    private float _bleedFracB;

    public override void OnStartServer()
    {
        base.OnStartServer();

        EnsureManagers();

        state.Value = MatchState.Live;
        remainingSeconds.Value = Mathf.Max(1, roundSeconds);

        reservesA.Value = Mathf.Max(0, startingReservesTeamA);
        reservesB.Value = Mathf.Max(0, startingReservesTeamB);

        aliveA.Value = 0;
        aliveB.Value = 0;

        zoneOwners = new Team[zones.Length];
        zoneSpawnIndices = new int[zones.Length];

        for (int i = 0; i < zones.Length; i++)
        {
            zoneOwners[i] = (zones[i] != null) ? zones[i].InitialOwner : Team.None;
            zoneSpawnIndices[i] = 0;
        }

        PushCountsImmediate();
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (state.Value != MatchState.Live) return;

        _secondAccum += Time.deltaTime;
        if (_secondAccum >= 1f)
        {
            _secondAccum -= 1f;

            remainingSeconds.Value -= 1;
            if (remainingSeconds.Value <= 0)
                remainingSeconds.Value = Mathf.Max(1, roundSeconds);

            ApplyZoneBleed(1f);
            CheckReservesWin();
            PushCountsIfChanged();
        }
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

    public bool TryGetSpawnForTeamAtZone(Team team, int zoneIndex, out Transform spawn)
    {
        spawn = null;

        if (zoneOwners == null) return false;
        if (zoneIndex < 0 || zoneIndex >= zones.Length) return false;

        var z = zones[zoneIndex];
        if (z == null) return false;

        if (zoneOwners[zoneIndex] != team) return false;

        var spawns = z.Spawns;
        if (spawns == null || spawns.Length == 0) return false;

        int i = zoneSpawnIndices[zoneIndex] % spawns.Length;
        zoneSpawnIndices[zoneIndex]++;

        spawn = spawns[i];
        return spawn != null;
    }

    public Transform GetSpawnForTeam(Team team)
    {
        if (zoneOwners == null) return null;

        if (team == Team.TeamB)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (zoneOwners[i] == Team.TeamB)
                {
                    if (TryGetSpawnForTeamAtZone(team, i, out var sp))
                        return sp;
                }
            }
        }
        else if (team == Team.TeamA)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zoneOwners[i] == Team.TeamA)
                {
                    if (TryGetSpawnForTeamAtZone(team, i, out var sp))
                        return sp;
                }
            }
        }

        return null;
    }

    public void ServerOnPlayerSpawned(PlayerTeam player, bool consumeReserve = false)
    {
        if (!IsServerInitialized || player == null) return;

        switch (player.team.Value)
        {
            case Team.TeamA:
                aliveA.Value = Mathf.Max(0, aliveA.Value + 1);
                break;
            case Team.TeamB:
                aliveB.Value = Mathf.Max(0, aliveB.Value + 1);
                break;
        }

        PushCountsImmediate();
        CheckReservesWin();
    }

    public void ServerOnPlayerDied(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;

        switch (player.team.Value)
        {
            case Team.TeamA:
                aliveA.Value = Mathf.Max(0, aliveA.Value - 1);
                reservesA.Value = Mathf.Max(0, reservesA.Value - 1);
                break;

            case Team.TeamB:
                aliveB.Value = Mathf.Max(0, aliveB.Value - 1);
                reservesB.Value = Mathf.Max(0, reservesB.Value - 1);
                break;
        }

        PushCountsImmediate();
        CheckReservesWin();
    }

    public void ServerOnPlayerDisconnected(PlayerTeam pt)
    {
        if (!IsServerInitialized || pt == null) return;

        switch (pt.team.Value)
        {
            case Team.TeamA:
                aliveA.Value = Mathf.Max(0, aliveA.Value - 1);
                reservesA.Value = Mathf.Max(0, reservesA.Value - 1);
                break;

            case Team.TeamB:
                aliveB.Value = Mathf.Max(0, aliveB.Value - 1);
                reservesB.Value = Mathf.Max(0, reservesB.Value - 1);
                break;
        }

        PushCountsImmediate();
        CheckReservesWin();
    }

    private void ApplyZoneBleed(float dt)
    {
        if (bleedReservesPerSecondPerZoneDiff <= 0f) return;

        int a = 0;
        int b = 0;

        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            if (z == null) continue;

            if (!includeUncapturableZonesInBleed && z.Uncapturable)
                continue;

            Team o = zoneOwners != null ? zoneOwners[i] : z.Owner;
            if (o == Team.TeamA) a++;
            else if (o == Team.TeamB) b++;
        }

        int diff = a - b;
        if (diff == 0) return;

        float bleed = Mathf.Abs(diff) * bleedReservesPerSecondPerZoneDiff * dt;

        if (diff > 0)
            _bleedFracB += bleed;
        else
            _bleedFracA += bleed;

        int drainA = Mathf.FloorToInt(_bleedFracA);
        int drainB = Mathf.FloorToInt(_bleedFracB);

        if (drainA > 0)
        {
            _bleedFracA -= drainA;
            reservesA.Value = Mathf.Max(0, reservesA.Value - drainA);
        }

        if (drainB > 0)
        {
            _bleedFracB -= drainB;
            reservesB.Value = Mathf.Max(0, reservesB.Value - drainB);
        }
    }

    private void CheckReservesWin()
    {
        if (state.Value == MatchState.PostRound) return;

        bool aOut = reservesA.Value <= 0;
        bool bOut = reservesB.Value <= 0;

        if (aOut && bOut) EndMatch(Team.None);
        else if (aOut) EndMatch(Team.TeamB);
        else if (bOut) EndMatch(Team.TeamA);
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

    public int GetZoneIndex(CaptureZone z)
    {
        for (int i = 0; i < zones.Length; i++)
            if (zones[i] == z)
                return i;
        return -1;
    }

    public void ServerOnZoneCaptured(CaptureZone z)
    {
        if (!IsServerInitialized || z == null) return;

        int index = GetZoneIndex(z);
        if (index < 0) return;

        zoneOwners[index] = z.Owner;
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

        _bleedFracA = 0f;
        _bleedFracB = 0f;

        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i] == null) continue;
            zones[i].ResetZone();
            zoneOwners[i] = zones[i].InitialOwner;
            zoneSpawnIndices[i] = 0;
        }

        var teams = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pt in teams)
        {
            var spawn = GetSpawnForTeam(pt.team.Value);
            if (spawn == null) continue;

            var ph = pt.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.ServerForceAlive(true);
                ph.ServerRestoreFull();
                ph.EnableRespawn();
            }

            pt.GetComponent<PlayerMotor>()?.Teleport(spawn.position, spawn.rotation);

            ServerOnPlayerSpawned(pt, consumeReserve: false);
        }
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

        if (team == Team.TeamA) prefab = montenegrinCorpsePrefab;
        else if (team == Team.TeamB) prefab = ottomanCorpsePrefab;

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
