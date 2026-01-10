using UnityEngine;
using FishNet.Object;

public class AimGun : NetworkBehaviour
{
    public Transform aimTransform;
    public Transform bone;
    public Vector3 targetPosition;
    public PlayerMotor playerMotor;
    public int iterations = 10;
    [Range(0, 1)] public float weight = 1;

    private Vector3 _remoteTargetPos;
    private float _nextAimSend;

    void LateUpdate()
    {
        if (playerMotor == null) return;
        if (!playerMotor.IsAiming.Value) return;
        if (bone == null || aimTransform == null) return;

        if (playerMotor.IsOwner)
        {
            if (playerMotor.target != null)
                targetPosition = playerMotor.target.position;

            AimAtTarget(bone, targetPosition, weight);

            if (IsSpawned && Time.time >= _nextAimSend)
            {
                _nextAimSend = Time.time + 0.05f;
                RpcSendTargetPos(targetPosition);
            }
        }
        else
        {
            targetPosition = Vector3.Lerp(targetPosition, _remoteTargetPos, Time.deltaTime * 40f);
            AimAtTarget(bone, targetPosition, weight);
        }
    }

    public void BindAimRig(PlayerMotor motor, Transform newAimTransform, Transform newBone)
    {
        if (motor != null)
            playerMotor = motor;

        aimTransform = newAimTransform;
        bone = newBone;
    }

    [ServerRpc(RequireOwnership = true)]
    private void RpcSendTargetPos(Vector3 pos)
    {
        RpcUpdateTargetPos(pos);
    }

    [ObserversRpc(BufferLast = true, ExcludeOwner = true)]
    private void RpcUpdateTargetPos(Vector3 pos)
    {
        _remoteTargetPos = pos;
    }

    private void AimAtTarget(Transform b, Vector3 targetPos, float w)
    {
        if (b == null || aimTransform == null) return;

        Vector3 origin = aimTransform.position;
        Vector3 targetDirection = targetPos - origin;
        if (targetDirection.sqrMagnitude < 0.000001f) return;

        for (int i = 0; i < iterations; i++)
        {
            Vector3 aimDirection = aimTransform.forward;
            Quaternion aimTowards = Quaternion.FromToRotation(aimDirection, targetDirection);
            Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, (w / iterations) * 3.5f);
            b.rotation = blendedRotation * b.rotation;
        }
    }
}
