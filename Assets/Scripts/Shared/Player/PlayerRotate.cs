using UnityEngine;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;

public class PlayerRotate : NetworkBehaviour
{
    [Header("References")]
    public Transform playerObj;
    public PlayerInputs input;
    public PlayerHealth health;

    [Header("Settings")]
    public float rotationSpeed = 12f;
    public float aimingRotationSpeed = 20f;

    public struct RotateData : IReplicateData
    {
        public float yaw;
        public bool isAiming;
        public bool hasInput;
        public Vector2 move;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Quaternion rotation;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        TimeManager.OnTick += OnTick;
        TimeManager.OnPostTick += OnPostTick;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (TimeManager != null)
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }
    }

    private void OnTick()
    {
        if (!IsOwner) return;
        if (input == null || playerObj == null || (health != null && !health.IsAlive)) return;

        RotateData rd = new RotateData
        {
            yaw = input.lookYawDeg,
            isAiming = input.isAiming,
            move = input.move,
            hasInput = input.move.sqrMagnitude > 0.0001f
        };

        ReplicateRotate(rd);
    }

    private void OnPostTick()
    {
        if (!IsServerInitialized) return;
        CreateReconcile();
    }

    [Replicate]
    private void ReplicateRotate(RotateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        if (!data.hasInput && !data.isAiming) return;

        if (data.isAiming)
        {
            Quaternion target = Quaternion.Euler(0f, data.yaw, 0f);
            float spd = Mathf.Max(rotationSpeed, aimingRotationSpeed);
            playerObj.rotation = Quaternion.Slerp(playerObj.rotation, target, Time.deltaTime * spd);
            return;
        }

        Quaternion basis = Quaternion.Euler(0f, data.yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;
        Vector3 inputDir = (fwd * data.move.y + right * data.move.x).normalized;
        if (inputDir.sqrMagnitude > 0.0001f)
        {
            Quaternion tgt = Quaternion.LookRotation(inputDir, Vector3.up);
            playerObj.rotation = Quaternion.Slerp(playerObj.rotation, tgt, Time.deltaTime * rotationSpeed);
        }
    }

    [Reconcile]
    private void ReconcileRotate(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        playerObj.rotation = data.rotation;
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            rotation = playerObj.rotation
        };
        ReconcileRotate(rd, Channel.Unreliable);
    }
}
