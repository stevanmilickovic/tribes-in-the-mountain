using UnityEngine;
using FishNet.Object;

[DisallowMultipleComponent]
public class PlayerShoot : NetworkBehaviour
{
    [Header("Refs")]
    public Transform orientation;
    public PlayerInputs input;
    public PlayerHealth health;
    public PlayerTeam team;

    [Header("Tuning")]
    public int damage = 100;
    public float maxRange = 150f;
    public float reloadSeconds = 6f;
    public LayerMask playerHitboxMask;

    [Header("Debug")]
    public bool allowFriendlyFire = false;

    private bool _isReloading;
    private float _nextAllowedShotTime;
    private bool _lastFireHeld;

    public override void OnStartServer()
    {
        base.OnStartServer();
        enabled = true;
    }

    public override void OnStartClient()
    {
        if (!IsServerInitialized)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (health != null && !health.IsAlive) return;
        if (input == null || orientation == null) return;

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

        Vector3 origin = orientation.position;
        Vector3 dir = orientation.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, playerHitboxMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root == transform.root)
            {
                // do nothing
            }
            else
            {
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    if (allowFriendlyFire || !SameTeamAs(targetHealth))
                        targetHealth.TakeDamageServer(damage, NetworkObject);
                }
            }
        }

        _isReloading = true;
        _nextAllowedShotTime = Time.time + 0.05f;
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
