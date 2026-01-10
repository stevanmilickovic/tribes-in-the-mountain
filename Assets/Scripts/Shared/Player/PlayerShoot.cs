using UnityEngine;
using FishNet.Object.Prediction;
using System.Collections;

public class PlayerShoot
{
    public int damage = 100;
    public float maxRange = 150f;
    public float reloadSeconds = 4.5f;
    public uint fireCooldownTicks = 3;
    public bool allowFriendlyFire = false;
    public float hipfireSpreadDegrees = 6f;
    public float aimingSpreadDegrees = 1.5f;

    public void ProcessFire(InputData rd, ref bool isReloading, ref uint nextAllowedFireTick, uint currentTick, PlayerMotor motor, PredictionRigidbody body, bool hasAmmo)
    {
        if (motor == null) return;

        if (rd.ReloadPressed && !isReloading)
        {
            if (!hasAmmo)
            {
                motor.SetReloading(true);
                if (motor.IsServerInitialized)
                    motor.StartCoroutine(FinishReload(motor, reloadSeconds));
            }
            return;
        }

        if (isReloading) return;
        if (!hasAmmo) return;

        if (!motor.IsAiming.Value) return;
        if (!rd.FirePressedEdge) return;

        if (currentTick < nextAllowedFireTick) return;

        if (!motor.IsServerInitialized)
            return;

        if (motor.Health != null && !motor.Health.IsAlive) return;

        if (motor.aimGun == null) return;
        if (motor.aimGun.aimTransform == null) return;

        Vector3 origin = motor.aimGun.aimTransform.position;
        Vector3 toTarget = motor.aimGun.targetPosition - origin;
        if (toTarget.sqrMagnitude < 0.000001f) return;

        motor.ConsumeAmmo();
        nextAllowedFireTick = currentTick + fireCooldownTicks;

        Vector3 dir = toTarget.normalized;

        float spread = motor.IsAiming.Value ? aimingSpreadDegrees : hipfireSpreadDegrees;
        dir = ApplySpread(dir, spread).normalized;

        motor.RpcOnFire(dir);

        if (!motor.IsServerInitialized) return;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && hit.collider.transform.root != motor.transform.root)
            {
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                if (targetHealth != null && targetHealth.IsAlive)
                {
                    if (allowFriendlyFire || !SameTeamAs(motor.Team, targetHealth))
                        targetHealth.TakeDamageServer(damage, motor.NetworkObject);
                }
            }
        }
    }

    private IEnumerator FinishReload(PlayerMotor motor, float wait)
    {
        yield return new WaitForSeconds(wait);
        if (motor == null || !motor.IsServerInitialized) yield break;
        motor.SetReloading(false);
        motor.RestoreAmmo();
    }

    private bool SameTeamAs(PlayerTeam myTeam, PlayerHealth otherHealth)
    {
        if (myTeam == null || otherHealth == null) return false;

        string myId = myTeam.TeamId;
        if (string.IsNullOrWhiteSpace(myId) || myId == TeamDatabase.NeutralId) return false;

        var otherTeam = otherHealth.GetComponent<PlayerTeam>();
        if (otherTeam == null) return false;

        string otherId = otherTeam.TeamId;
        if (string.IsNullOrWhiteSpace(otherId) || otherId == TeamDatabase.NeutralId) return false;

        return otherId == myId;
    }

    private Vector3 ApplySpread(Vector3 forward, float spreadDeg)
    {
        Vector2 random = Random.insideUnitCircle * spreadDeg;
        Quaternion rot = Quaternion.Euler(random.x, random.y, 0f);
        return rot * forward;
    }
}
