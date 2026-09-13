using UnityEngine;
using FishNet.Object;

[DefaultExecutionOrder(200)]
public class AimGun : NetworkBehaviour
{
    public Transform aimTransform;
    public Transform bone;
    public Vector3 targetPosition;
    public PlayerMotor playerMotor;
    public int iterations = 10;
    [Range(0, 1)] public float weight = 1;
    [Min(0.01f)] public float ownerDirectionSmoothTime = 0.04f;
    [Min(0.01f)] public float remoteDirectionSmoothTime = 0.08f;

    private Camera _ownerCamera;
    private Vector3 _remoteAimDirection;
    private Vector3 _visualAimDirection;
    private float _nextAimSend;
    private bool _hasRemoteAim;
    private bool _hasVisualAim;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
            _ownerCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (playerMotor == null)
            return;

        if (!playerMotor.IsAiming.Value)
        {
            _hasVisualAim = false;
            return;
        }

        if (bone == null || aimTransform == null)
            return;

        if (playerMotor.IsOwner)
        {
            if (_ownerCamera == null)
                _ownerCamera = Camera.main;
            if (_ownerCamera == null)
                return;

            if (playerMotor.target != null)
                targetPosition = playerMotor.target.position;

            Vector3 viewDirection = _ownerCamera.transform.forward;
            AimAtDirection(GetSmoothedVisualDirection(viewDirection, ownerDirectionSmoothTime));

            if (IsSpawned && Time.time >= _nextAimSend)
            {
                _nextAimSend = Time.time + 0.05f;
                RpcSendAim(targetPosition, viewDirection);
            }
        }
        else
        {
            if (!_hasRemoteAim)
                return;

            AimAtDirection(GetSmoothedVisualDirection(
                _remoteAimDirection, remoteDirectionSmoothTime));
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
    private void RpcSendAim(Vector3 pos, Vector3 direction)
    {
        // The dedicated server also needs the latest aim point for PlayerShoot.
        targetPosition = pos;
        RpcUpdateAim(pos, direction);
    }

    [ObserversRpc(BufferLast = true, ExcludeOwner = true)]
    private void RpcUpdateAim(Vector3 pos, Vector3 direction)
    {
        targetPosition = pos;
        _remoteAimDirection = direction;
        _hasRemoteAim = true;
    }

    private void AimAtDirection(Vector3 direction)
    {
        // The view direction stays defined even when a nearby hit is behind the
        // muzzle. A world-space hit point does not, and makes the bone flip sides.
        for (int i = 0; i < iterations; i++)
        {
            Quaternion aimTowards = Quaternion.FromToRotation(aimTransform.forward, direction);
            Quaternion blendedRotation = Quaternion.Slerp(
                Quaternion.identity,
                aimTowards,
                (weight / iterations) * 3.5f);
            bone.rotation = blendedRotation * bone.rotation;
        }
    }

    private Vector3 GetSmoothedVisualDirection(Vector3 direction, float smoothTime)
    {
        if (!_hasVisualAim)
        {
            _visualAimDirection = direction;
            _hasVisualAim = true;
        }

        float blend = 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
        _visualAimDirection = Vector3.Slerp(_visualAimDirection, direction, blend);
        return _visualAimDirection;
    }
}
