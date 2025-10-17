using UnityEngine;
using FishNet.Object;

[DisallowMultipleComponent]
public class PlayerShoot : NetworkBehaviour
{
    [Header("Refs")]
    public Transform orientation;       // your existing orientation child (aim basis)
    public PlayerInputs input;          // same PlayerInputs you already use
    public PlayerHealth health;         // to check alive
    public PlayerTeam team;             // for friendly-fire check

    [Header("Tuning")]
    public int damage = 100;            // one-shot prototype
    public float maxRange = 150f;
    public float reloadSeconds = 6f;    // musket fantasy
    public LayerMask playerHitboxMask;  // set to PlayerHitbox layer in inspector

    [Header("Debug")]
    public bool allowFriendlyFire = false;

    // runtime
    private bool _isReloading;
    private float _nextAllowedShotTime;
    private bool _lastFireHeld;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // cache fallbacks if not wired in inspector
        if (input == null) input = GetComponent<PlayerInputs>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (team == null) team = GetComponent<PlayerTeam>();
        if (orientation == null) orientation = transform; // fallback, but please assign the real one
        enabled = true;
    }

    public override void OnStartClient()
    {
        // server-authoritative; no client logic yet
        if (!IsServerInitialized) enabled = false;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (health != null && !health.IsAlive) return;

        if (input == null || orientation == null) return;

        // edge detect on the server from input forwarded via PlayerInputs ServerRpc
        bool fireHeld = input.firePressed;
        bool firePressedThisFrame = fireHeld && !_lastFireHeld;
        _lastFireHeld = fireHeld;

        if (firePressedThisFrame)
            TryFireServer();
    }

    private void TryFireServer()
    {
        if (_isReloading) return;
        if (Time.time < _nextAllowedShotTime) return;
        if (orientation == null) return;

        // Build a ray from orientation forward
        Vector3 origin = orientation.position;
        Vector3 dir = orientation.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, playerHitboxMask, QueryTriggerInteraction.Ignore))
        {
            // Ignore self hits
            if (hit.collider.transform.root == transform.root)
            {
                // do nothing
            }
            else
            {
                // Find a PlayerHealth on the hit object
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    // Team check
                    if (allowFriendlyFire || !SameTeamAs(targetHealth))
                        targetHealth.TakeDamageServer(damage, NetworkObject);
                }
            }
        }

        // Single-shot start reload
        _isReloading = true;
        _nextAllowedShotTime = Time.time + 0.05f; // tiny anti-spam cushion
        Invoke(nameof(FinishReloadServer), Mathf.Max(0f, reloadSeconds));
    }

    private bool SameTeamAs(PlayerHealth other)
    {
        if (team == null) return false;
        var otherTeam = other.GetComponent<PlayerTeam>();
        if (otherTeam == null) return false;
        return otherTeam.team.Value == team.team.Value && team.team.Value != Team.None;
    }

    private void FinishReloadServer()
    {
        if (!IsServerInitialized) return;
        _isReloading = false;
    }
}
