using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MatchState { PreRound, Live, PostRound }

[DisallowMultipleComponent]
public class MatchController : NetworkSingleton<MatchController>
{
    public const string TeamAId = "team_a";
    public const string TeamBId = "team_b";

    [SerializeField] private int maxTeamA = 10;
    [SerializeField] private int maxTeamB = 10;

    [Header("Match Config")]
    [SerializeField] private int roundSeconds = 600;
    [SerializeField] private int intermissionSeconds = 5;

    [Header("Reserves")]
    [SerializeField] private int startingReservesTeamA = 30;
    [SerializeField] private int startingReservesTeamB = 30;

    [Header("Bleed")]
    [SerializeField] private bool includeUncapturableZonesInBleed = false;
    [SerializeField] private float bleedReservesPerSecondPerZoneDiff = 0.2f;

    [Header("Capture Zones")]
    [SerializeField] public CaptureZone[] zones;

    [Header("Corpse")]
    [SerializeField] private GameObject universalCorpsePrefab;

    private string[] zoneOwners;
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

    private readonly HashSet<PlayerTeam> _teamAPlayers = new();
    private readonly HashSet<PlayerTeam> _teamBPlayers = new();

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

        state.Value = MatchState.Live;
        remainingSeconds.Value = Mathf.Max(1, roundSeconds);

        reservesA.Value = Mathf.Max(0, startingReservesTeamA);
        reservesB.Value = Mathf.Max(0, startingReservesTeamB);

        aliveA.Value = 0;
        aliveB.Value = 0;

        zoneOwners = new string[zones != null ? zones.Length : 0];
        zoneSpawnIndices = new int[zones != null ? zones.Length : 0];

        for (int i = 0; i < zoneOwners.Length; i++)
        {
            var z = zones[i];
            zoneOwners[i] = z != null ? NormalizeTeamId(z.InitialOwnerId) : TeamDatabase.NeutralId;
            zoneSpawnIndices[i] = 0;
        }

        RebuildTeamMembership();
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

    public bool CanJoinTeam(string teamId)
    {
        teamId = NormalizeTeamId(teamId);
        return teamId switch
        {
            TeamAId => TeamACount < maxTeamA,
            TeamBId => TeamBCount < maxTeamB,
            _ => false
        };
    }

    public void ServerJoinTeam(PlayerTeam player, string desiredTeamId)
    {
        if (!IsServerInitialized || player == null) return;

        desiredTeamId = NormalizeTeamId(desiredTeamId);
        if (desiredTeamId == TeamDatabase.NeutralId) return;

        RemoveFromTeams(player);

        if (desiredTeamId == TeamAId) _teamAPlayers.Add(player);
        else if (desiredTeamId == TeamBId) _teamBPlayers.Add(player);
        else return;

        player.teamId.Value = desiredTeamId;

        PushCountsIfChanged();
    }

    public bool ServerCanTeamSpawn(string teamId)
    {
        if (!IsServerInitialized) return false;
        if (state.Value != MatchState.Live) return false;

        teamId = NormalizeTeamId(teamId);

        return teamId switch
        {
            TeamAId => reservesA.Value > 0,
            TeamBId => reservesB.Value > 0,
            _ => false
        };
    }

    public bool TryGetSpawnForTeamAtZone(string teamId, int zoneIndex, out Transform spawn)
    {
        spawn = null;

        teamId = NormalizeTeamId(teamId);

        if (zoneOwners == null) return false;
        if (zones == null) return false;
        if (zoneIndex < 0 || zoneIndex >= zones.Length) return false;

        var z = zones[zoneIndex];
        if (z == null) return false;

        if (NormalizeTeamId(zoneOwners[zoneIndex]) != teamId) return false;

        var spawns = z.Spawns;
        if (spawns == null || spawns.Length == 0) return false;

        int i = zoneSpawnIndices[zoneIndex] % spawns.Length;
        zoneSpawnIndices[zoneIndex]++;

        spawn = spawns[i];
        return spawn != null;
    }

    public Transform GetSpawnForTeam(string teamId)
    {
        if (zoneOwners == null || zones == null) return null;

        teamId = NormalizeTeamId(teamId);

        if (teamId == TeamBId)
        {
            for (int i = zones.Length - 1; i >= 0; i--)
            {
                if (NormalizeTeamId(zoneOwners[i]) == TeamBId)
                {
                    if (TryGetSpawnForTeamAtZone(teamId, i, out var sp))
                        return sp;
                }
            }
        }
        else if (teamId == TeamAId)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (NormalizeTeamId(zoneOwners[i]) == TeamAId)
                {
                    if (TryGetSpawnForTeamAtZone(teamId, i, out var sp))
                        return sp;
                }
            }
        }

        return null;
    }

    public void ServerOnPlayerSpawned(PlayerTeam player, bool consumeReserve = false)
    {
        if (!IsServerInitialized || player == null) return;

        string tid = NormalizeTeamId(player.TeamId);

        if (consumeReserve)
        {
            if (tid == TeamAId) reservesA.Value = Mathf.Max(0, reservesA.Value - 1);
            else if (tid == TeamBId) reservesB.Value = Mathf.Max(0, reservesB.Value - 1);
        }

        if (tid == TeamAId) aliveA.Value = Mathf.Max(0, aliveA.Value + 1);
        else if (tid == TeamBId) aliveB.Value = Mathf.Max(0, aliveB.Value + 1);

        PushCountsImmediate();
        CheckReservesWin();
    }

    public void ServerOnPlayerDied(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;

        string tid = NormalizeTeamId(player.TeamId);

        if (tid == TeamAId)
        {
            aliveA.Value = Mathf.Max(0, aliveA.Value - 1);
            reservesA.Value = Mathf.Max(0, reservesA.Value - 1);
        }
        else if (tid == TeamBId)
        {
            aliveB.Value = Mathf.Max(0, aliveB.Value - 1);
            reservesB.Value = Mathf.Max(0, reservesB.Value - 1);
        }

        PushCountsImmediate();
        CheckReservesWin();
    }

    public void ServerOnPlayerDisconnected(PlayerTeam pt)
    {
        if (!IsServerInitialized || pt == null) return;

        string tid = NormalizeTeamId(pt.TeamId);

        if (tid == TeamAId)
        {
            aliveA.Value = Mathf.Max(0, aliveA.Value - 1);
            reservesA.Value = Mathf.Max(0, reservesA.Value - 1);
        }
        else if (tid == TeamBId)
        {
            aliveB.Value = Mathf.Max(0, aliveB.Value - 1);
            reservesB.Value = Mathf.Max(0, reservesB.Value - 1);
        }

        RemoveFromTeams(pt);

        PushCountsImmediate();
        CheckReservesWin();
    }

    private void ApplyZoneBleed(float dt)
    {
        if (bleedReservesPerSecondPerZoneDiff <= 0f) return;
        if (zones == null || zoneOwners == null) return;

        int a = 0;
        int b = 0;

        for (int i = 0; i < zones.Length; i++)
        {
            var z = zones[i];
            if (z == null) continue;

            if (!includeUncapturableZonesInBleed && z.Uncapturable)
                continue;

            string o = NormalizeTeamId(zoneOwners[i]);
            if (o == TeamAId) a++;
            else if (o == TeamBId) b++;
        }

        int diff = a - b;
        if (diff == 0) return;

        float bleed = Mathf.Abs(diff) * bleedReservesPerSecondPerZoneDiff * dt;

        if (diff > 0) _bleedFracB += bleed;
        else _bleedFracA += bleed;

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

        if (aOut && bOut) EndMatch(TeamDatabase.NeutralId);
        else if (aOut) EndMatch(TeamBId);
        else if (bOut) EndMatch(TeamAId);
    }

    private void PushCountsImmediate()
    {
        teamACount.Value = _teamAPlayers.Count;
        teamBCount.Value = _teamBPlayers.Count;
    }

    private void PushCountsIfChanged()
    {
        int a = _teamAPlayers.Count;
        int b = _teamBPlayers.Count;
        if (teamACount.Value != a) teamACount.Value = a;
        if (teamBCount.Value != b) teamBCount.Value = b;
    }

    public int GetZoneIndex(CaptureZone z)
    {
        if (zones == null) return -1;
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

        zoneOwners[index] = NormalizeTeamId(z.TeamOwnerId);
    }

    private void EndMatch(string winnerTeamId)
    {
        if (state.Value == MatchState.PostRound) return;

        foreach (var ph in FindObjectsOfType<PlayerHealth>())
            ph.CancelRespawn();

        state.Value = MatchState.PostRound;
        Rpc_OnMatchEnded(winnerTeamId);

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

        if (zones != null)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null) continue;
                zones[i].ResetZone();
                zoneOwners[i] = NormalizeTeamId(zones[i].InitialOwnerId);
                zoneSpawnIndices[i] = 0;
            }
        }

        RebuildTeamMembership();
        PushCountsImmediate();

        var teams = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pt in teams)
        {
            if (pt == null) continue;

            var spawn = GetSpawnForTeam(pt.TeamId);
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

        string teamId = NormalizeTeamId(pt.TeamId);
        Vector3 pos = ph.transform.position;
        Quaternion rot = ph.transform.rotation;
        string deathAnim = animDriver.lastDeathAnim;

        RpcSpawnCorpse(teamId, pos, rot, deathAnim);
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcSpawnCorpse(string teamId, Vector3 pos, Quaternion rot, string deathAnim)
    {
        var db = TeamDatabase.Instance;
        if (db == null) return;

        var def = db.Get(teamId);
        if (def == null) return;

        if (universalCorpsePrefab == null) return;

        GameObject corpse = Instantiate(universalCorpsePrefab, pos, rot);
        corpse.transform.SetParent(null, true);

        Transform modelRoot = corpse.transform.Find("ModelRoot");
        if (modelRoot == null) modelRoot = corpse.transform;

        Transform weaponRoot = corpse.transform.Find("WeaponRoot");
        if (weaponRoot == null) weaponRoot = modelRoot;

        GameObject modelInstance = null;
        GameObject weaponInstance = null;

        if (!string.IsNullOrWhiteSpace(def.ModelKey))
        {
            var modelPrefab = Resources.Load<GameObject>(def.ModelKey);
            if (modelPrefab != null)
            {
                modelInstance = Instantiate(modelPrefab, modelRoot);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;
            }
        }

        Transform weaponParent = weaponRoot;

        if (modelInstance != null && !string.IsNullOrWhiteSpace(def.WeaponSocketPath))
        {
            var socket = modelInstance.transform.Find(def.WeaponSocketPath);
            if (socket != null)
                weaponParent = socket;
        }

        if (!string.IsNullOrWhiteSpace(def.WeaponKey))
        {
            var weaponPrefab = Resources.Load<GameObject>(def.WeaponKey);
            if (weaponPrefab != null)
            {
                weaponInstance = Instantiate(weaponPrefab, weaponParent);
                weaponInstance.transform.localPosition = Vector3.zero;
                weaponInstance.transform.localRotation = Quaternion.identity;
                weaponInstance.transform.localScale = Vector3.one;
            }
        }

        Animator anim = null;

        if (modelInstance != null)
        {
            if (!string.IsNullOrWhiteSpace(def.AnimatorPath))
            {
                var t = modelInstance.transform.Find(def.AnimatorPath);
                if (t != null)
                    anim = t.GetComponent<Animator>() ?? t.GetComponentInChildren<Animator>(true);
            }

            if (anim == null)
                anim = modelInstance.GetComponentInChildren<Animator>(true);
        }

        if (anim != null && !string.IsNullOrWhiteSpace(deathAnim))
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
    private void Rpc_OnMatchEnded(string winnerTeamId)
    {
        if (hud != null)
            hud.ShowVictory(winnerTeamId);
    }

    [ObserversRpc]
    private void Rpc_StartNewRound()
    {
        if (hud != null)
            hud.HideVictory();
    }

    private void RebuildTeamMembership()
    {
        _teamAPlayers.Clear();
        _teamBPlayers.Clear();

        var all = FindObjectsByType<PlayerTeam>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pt in all)
        {
            if (pt == null) continue;
            string tid = NormalizeTeamId(pt.TeamId);
            if (tid == TeamAId) _teamAPlayers.Add(pt);
            else if (tid == TeamBId) _teamBPlayers.Add(pt);
        }
    }

    private void RemoveFromTeams(PlayerTeam player)
    {
        if (player == null) return;
        _teamAPlayers.Remove(player);
        _teamBPlayers.Remove(player);
    }

    private static string NormalizeTeamId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TeamDatabase.NeutralId;

        var db = TeamDatabase.Instance;
        if (db == null) return id;

        return db.IsValidTeamId(id) ? id : TeamDatabase.NeutralId;
    }
}
