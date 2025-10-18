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

    private Rigidbody _rb;
    private PlayerMovement _movement;
    private PlayerRotate _rotate;
    private PlayerTeam _team;

    public bool IsAlive => currentHealth.Value > 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth.Value = Mathf.Max(1, maxHealth);
        SetAliveServer(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
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
        SetAliveServer(false);

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        Rpc_OnDied();

        StartCoroutine(RespawnRoutineServer());
    }

    private IEnumerator RespawnRoutineServer()
    {
        float t = Mathf.Max(0f, respawnDelay);
        if (t > 0f) yield return new WaitForSeconds(t);

        Team team = (_team != null) ? _team.team.Value : Team.None;
        Transform spawn = LobbySelectionGateway.Instance.GetSpawnForTeam(team);

        // Fallback: if no team or spawn found, just keep current transform
        Vector3 pos = (spawn ? spawn.position : transform.position);
        Quaternion rot = (spawn ? spawn.rotation : transform.rotation);
        transform.SetPositionAndRotation(pos, rot);

        transform.SetPositionAndRotation(pos, rot);
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        currentHealth.Value = maxHealth;
        SetAliveServer(true);

        Rpc_OnRespawned(Owner, pos, rot);
    }

    private void SetAliveServer(bool alive)
    {
        if (_movement != null) _movement.enabled = alive;
        if (_rotate != null) _rotate.enabled = alive;

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
