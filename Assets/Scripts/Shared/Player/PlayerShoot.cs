using UnityEngine;
using FishNet.Object;
using FishNet.Managing.Timing;

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

    public struct ShootData
    {
        public bool firePressed;
    }

    public override void OnStartClient()
    {
        if (!IsOwner) return;
        TimeManager.OnTick += OnTick;
    }

    public override void OnStopClient()
    {
        if (IsOwner && TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        if (!IsOwner) return;
        if (health != null && !health.IsAlive) return;
        if (input == null || orientation == null) return;

        ShootData sd = new ShootData { firePressed = input.firePressed };
        if (sd.firePressed)
            TryFire();
    }

    private void TryFire()
    {
        if (_isReloading) return;
        if (Time.time < _nextAllowedShotTime) return;

        ServerTryFire();
    }

    [ServerRpc]
    private void ServerTryFire()
    {
        if (_isReloading) return;
        if (Time.time < _nextAllowedShotTime) return;
        if (orientation == null) return;
        if (health != null && !health.IsAlive) return;

        Vector3 origin = orientation.position;
        Vector3 dir = orientation.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, playerHitboxMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.root != transform.root)
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

    [Server]
    private void FinishReloadServer()
    {
        _isReloading = false;
    }
}
