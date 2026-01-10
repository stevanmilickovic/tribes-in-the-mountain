using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

[DisallowMultipleComponent]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public readonly SyncVar<int> currentHealth = new();

    [Header("Respawn")]
    public float respawnDelay = 0f;

    [Header("Optional Visual Toggle")]
    public Transform visualsRoot;

    private bool matchEnded = false;
    private float diedAtServerTime = -999f;

    private readonly SyncVar<bool> aliveNet = new(true);
    private readonly SyncVar<bool> awaitingRespawnSelectionNet = new(false);
    private readonly SyncVar<int> preferredSpawnZoneIndex = new(-1);

    private bool corpseSpawnedThisDeath = false;

    public bool IsAlive => aliveNet.Value;
    public bool AwaitingRespawnSelection => awaitingRespawnSelectionNet.Value;
    public int PreferredSpawnZoneIndex => preferredSpawnZoneIndex.Value;

    private Rigidbody _rb => GetComponent<Rigidbody>();
    private PlayerMotor _motor => GetComponent<PlayerMotor>();
    private PlayerTeam _team => GetComponent<PlayerTeam>();
    private MatchController MatchController => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth.Value = Mathf.Max(1, maxHealth);
        preferredSpawnZoneIndex.Value = -1;
        aliveNet.Value = true;
        awaitingRespawnSelectionNet.Value = false;
        diedAtServerTime = -999f;
        corpseSpawnedThisDeath = false;
        SetAliveServer(true);
    }

    public void TakeDamageServer(int amount, NetworkObject instigator = null)
    {
        if (!IsServerInitialized) return;
        if (!IsAlive) return;
        if (amount <= 0) return;

        int newHp = Mathf.Max(0, currentHealth.Value - amount);
        currentHealth.Value = newHp;

        if (newHp <= 0)
            DieServer(instigator);
    }

    private void DieServer(NetworkObject killer)
    {
        if (!IsServerInitialized) return;
        if (!IsAlive) return;

        currentHealth.Value = 0;
        aliveNet.Value = false;
        SetAliveServer(false);

        awaitingRespawnSelectionNet.Value = true;
        diedAtServerTime = Time.time;
        corpseSpawnedThisDeath = false;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        if (_team != null && MatchController != null)
            MatchController.ServerOnPlayerDied(_team);

        Target_OnAwaitingRespawn(Owner);
    }

    [ServerRpc(RequireOwnership = true)]
    public void SetPreferredSpawnZone(int zoneIndex)
    {
        if (!IsServerInitialized) return;
        if (_team == null) return;
        if (MatchController == null) return;

        if (zoneIndex < 0)
        {
            preferredSpawnZoneIndex.Value = -1;
            return;
        }

        string tid = _team.TeamId;
        if (string.IsNullOrWhiteSpace(tid) || tid == TeamDatabase.NeutralId) return;

        if (MatchController.TryGetSpawnForTeamAtZone(tid, zoneIndex, out _))
            preferredSpawnZoneIndex.Value = zoneIndex;
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestRespawnNow()
    {
        if (!IsServerInitialized) return;
        if (matchEnded) return;
        if (IsAlive) return;
        if (!awaitingRespawnSelectionNet.Value) return;
        if (MatchController == null) return;

        float t = Mathf.Max(0f, respawnDelay);
        if (Time.time - diedAtServerTime < t) return;

        if (!corpseSpawnedThisDeath)
        {
            MatchController.SpawnCorpseFor(this);
            corpseSpawnedThisDeath = true;
        }

        string tid = _team != null ? _team.TeamId : TeamDatabase.NeutralId;

        if (!MatchController.ServerCanTeamSpawn(tid))
        {
            awaitingRespawnSelectionNet.Value = false;
            Target_OnBecameSpectator(Owner);
            return;
        }

        Transform spawn = null;
        int zi = preferredSpawnZoneIndex.Value;

        if (zi >= 0 && MatchController.TryGetSpawnForTeamAtZone(tid, zi, out var preferred))
            spawn = preferred;

        if (spawn == null)
            return;

        if (_motor != null)
            _motor.Teleport(spawn.position, spawn.rotation);

        currentHealth.Value = maxHealth;
        aliveNet.Value = true;
        SetAliveServer(true);

        awaitingRespawnSelectionNet.Value = false;

        if (_team != null)
            MatchController.ServerOnPlayerSpawned(_team, consumeReserve: false);

        Target_OnRespawnSelectionEnded(Owner);
    }

    private void SetAliveServer(bool alive)
    {
        ApplyAliveLocally(alive);
        Rpc_SetAlive(alive);
    }

    public void ServerForceAlive(bool alive)
    {
        if (!IsServerInitialized) return;
        aliveNet.Value = alive;
        SetAliveServer(alive);
    }

    public void ServerRestoreFull()
    {
        if (!IsServerInitialized) return;

        currentHealth.Value = maxHealth;
        aliveNet.Value = true;
        SetAliveServer(true);

        if (_rb)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    [ObserversRpc(BufferLast = false)]
    private void Rpc_SetAlive(bool alive) => ApplyAliveLocally(alive);

    private void ApplyAliveLocally(bool alive)
    {
        if (_motor) _motor.enabled = alive;
        SetRenderersEnabled(visualsRoot, alive);
    }

    private void SetRenderersEnabled(Transform root, bool enabled)
    {
        if (root == null) return;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = enabled;
    }

    public void CancelRespawn()
    {
        matchEnded = true;
        awaitingRespawnSelectionNet.Value = false;
    }

    public void EnableRespawn()
    {
        matchEnded = false;
    }

    [TargetRpc]
    private void Target_OnAwaitingRespawn(NetworkConnection conn)
    {
    }

    [TargetRpc]
    private void Target_OnRespawnSelectionEnded(NetworkConnection conn)
    {
    }

    [TargetRpc]
    private void Target_OnBecameSpectator(NetworkConnection conn)
    {
    }
}
