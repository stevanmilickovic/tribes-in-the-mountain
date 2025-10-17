using System.Collections;
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
    [Tooltip("Seconds to wait before respawn.")]
    public float respawnDelay = 5f;

    [Header("Optional Visual Toggle")]
    [Tooltip("Optional root for renderers to hide on death (leave null to keep visible).")]
    public Transform visualsRoot;

    // Cached
    private Rigidbody _rb;
    private PlayerMovement _movement;
    private PlayerRotate _rotate;
    private PlayerTeam _team;

    [SerializeField] private MonoBehaviour spawnProviderBehaviour;
    private ITeamSpawnProvider _spawnProvider;

    public bool IsAlive => currentHealth.Value > 0;

    public override void OnStartServer()
    {
        if (_spawnProvider == null)
        {
            _spawnProvider = spawnProviderBehaviour as ITeamSpawnProvider;

            if (_spawnProvider == null)
                _spawnProvider = FindObjectOfType<MonoBehaviour>(true) as ITeamSpawnProvider;
        }

        base.OnStartServer();
        CacheRefs();
        currentHealth.Value = Mathf.Max(1, maxHealth);
        SetAliveServer(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        CacheRefs(); // For local gates/FX if needed
    }

    private void CacheRefs()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        if (_movement == null) _movement = GetComponent<PlayerMovement>();
        if (_rotate == null) _rotate = GetComponent<PlayerRotate>();
        if (_team == null) _team = GetComponent<PlayerTeam>();
        if (visualsRoot == null) visualsRoot = transform; // safe default
    }

    /// <summary>
    /// Server-only damage entry point.
    /// </summary>
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
        SetAliveServer(false);

        // Stop motion
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            // Keep non-kinematic so the teleport later is clean; no ragdoll in v1
        }

        // (Optional) notify clients for UI/hitfeed
        Rpc_OnDied();

        // Start respawn
        StartCoroutine(RespawnRoutineServer());
    }

    private IEnumerator RespawnRoutineServer()
    {
        float t = Mathf.Max(0f, respawnDelay);
        if (t > 0f) yield return new WaitForSeconds(t);

        // Choose spawn based on team
        Team team = (_team != null) ? _team.team.Value : Team.None;
        Transform spawn = _spawnProvider?.GetSpawn(team, NetworkObject);

        // Fallback: if no team or spawn found, just keep current transform
        Vector3 pos = (spawn ? spawn.position : transform.position);
        Quaternion rot = (spawn ? spawn.rotation : transform.rotation);
        transform.SetPositionAndRotation(pos, rot);

        // Teleport & reset physics
        transform.SetPositionAndRotation(pos, rot);
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        // Restore health and re-enable control
        currentHealth.Value = maxHealth;
        SetAliveServer(true);

        // (Optional) notify owner for UI
        Rpc_OnRespawned(Owner, pos, rot);
    }

    private void SetAliveServer(bool alive)
    {
        // Gate server-authoritative controllers
        if (_movement != null) _movement.enabled = alive;
        if (_rotate != null) _rotate.enabled = alive;

        // Show/Hide visuals (optional)
        SetRenderersEnabled(visualsRoot, alive);
    }

    private void SetRenderersEnabled(Transform root, bool enabled)
    {
        if (root == null) return;
        var rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends) r.enabled = enabled;
    }

    [ObserversRpc(BufferLast = false)]
    private void Rpc_OnDied()
    {
        // Hook UI/FX here later (fade out, death text, etc.)
        // Intentionally minimal for prototype.
    }

    [TargetRpc]
    private void Rpc_OnRespawned(NetworkConnection conn, Vector3 pos, Quaternion rot)
    {
        // Owner-only UI hook (e.g., fade in, “Respawned” toast).
        // CameraBinder remains valid since we didn’t destroy the object.
    }
}
