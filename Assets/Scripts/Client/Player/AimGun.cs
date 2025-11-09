using UnityEngine;
using FishNet.Object;

public class AimGun : NetworkBehaviour
{
    public Transform aimTransform, bone;
    public Vector3 targetPosition;
    public PlayerMotor playerMotor;
    public int iterations = 10;
    [Range(0, 1)] public float weight = 1;
    public bool DoGunAim;

    private Vector3 _remoteTargetPos;
    private float _nextAimSend;

    void LateUpdate()
    {
        DoGunAim = playerMotor.IsAiming.Value;
        if (!DoGunAim) return;

        if (playerMotor.IsOwner)
        {
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

    private void AimAtTarget(Transform bone, Vector3 targetPos, float w)
    {
        for (int i = 0; i < iterations; i++)
        {
            Vector3 aimDirection = aimTransform.forward;
            Vector3 targetDirection = targetPos - aimTransform.position;
            Quaternion aimTowards = Quaternion.FromToRotation(aimDirection, targetDirection);
            Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, aimTowards, (w / iterations) * 3.5f);
            bone.rotation = blendedRotation * bone.rotation;
        }
    }
}
