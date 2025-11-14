using UnityEngine;
using FishNet.Object.Prediction;
using System.Collections;

public class PlayerShoot
{
    public int damage = 100;
    public float maxRange = 150f;
    public float reloadSeconds = 6f;
    public uint fireCooldownTicks = 3;
    public bool allowFriendlyFire = false;

    public void ProcessFire(InputData rd, ref bool isReloading, ref uint nextAllowedFireTick, uint currentTick, PlayerMotor motor, PredictionRigidbody body)
    {
        //if (isReloading) return;
        if (!rd.FirePressedEdge) return;
        //if (currentTick < nextAllowedFireTick) return;

        nextAllowedFireTick = currentTick + fireCooldownTicks;
        isReloading = true;

        if (motor.IsServerInitialized)
            motor.StartCoroutine(FinishReload(motor, reloadSeconds));

        if (!motor.IsServerInitialized) return;
        if (motor.Health != null && !motor.Health.IsAlive) return;
        if (motor.Orientation == null) return;

        Vector3 origin = motor.aimGun.aimTransform.position;
        Vector3 dir = (motor.aimGun.targetPosition - origin).normalized;

        motor.RpcOnFire(dir);

        Debug.DrawRay(origin, dir * maxRange, Color.red, 1f);
        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"Hit {hit.collider.name}");
            if (hit.collider.transform.root != motor.transform.root)
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
        if (!motor.IsServerInitialized) yield break;
        motor.SetReloading(false);
    }

    private bool SameTeamAs(PlayerTeam myTeam, PlayerHealth otherHealth)
    {
        if (myTeam == null) return false;
        var otherTeam = otherHealth.GetComponent<PlayerTeam>();
        if (otherTeam == null) return false;
        return otherTeam.team.Value == myTeam.team.Value && myTeam.team.Value != Team.None;
    }
}
