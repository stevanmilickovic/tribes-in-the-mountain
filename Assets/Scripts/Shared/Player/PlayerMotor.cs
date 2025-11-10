using UnityEngine;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using FishNet.Object.Synchronizing;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Component.Transforming;

public class PlayerMotor : TickNetworkBehaviour
{
    public PlayerInputs inputs;
    public PlayerMovement movement;
    public PlayerShoot shoot;
    public Rigidbody rb;
    public LayerMask groundMask;
    public PlayerHealth health;
    public PlayerTeam team;
    public Transform Orientation;
    public Transform target;
    public AimGun aimGun;

    public readonly SyncVar<bool> IsAiming = new();
    public readonly SyncVar<Quaternion> RotationNet = new();
    private float _nextRotSend;

    public readonly SyncVar<bool> IsCrouchingNet = new();
    public readonly SyncVar<bool> IsProneNet = new();
    public readonly SyncVar<float> SpeedNet = new();

    private PredictionRigidbody _pred;
    private bool _isCrouching;
    private bool _isProne;
    private bool _grounded;
    private bool _isReloading;
    private uint _nextAllowedFireTick;
    private uint _nextAllowedJumpTick;

    Vector3 _netTargetPos;

    private struct StateData : IReconcileData
    {
        public Vector3 Position;
        public Vector3 Velocity;

        public bool Grounded;
        public bool IsCrouching;
        public bool IsProne;
        public bool IsReloading;
        public uint NextAllowedFireTick;
        public uint NextAllowedJumpTick;

        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }


    private void Awake()
    {
        _pred = new PredictionRigidbody();
        movement = new PlayerMovement();
        shoot = new PlayerShoot();
    }

    void Update()
    {
        if (!IsOwner || inputs == null) return;

        var rd = inputs.LatestInput;

        movement.SimulateRotation(rd, _pred, Time.deltaTime, target);

        if (Time.time >= _nextRotSend)
        {
            _nextRotSend = Time.time + 0.05f;
            RpcSendRotation(rb.rotation);
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner && !IsServerInitialized)
        {
            rb.position = Vector3.Lerp(rb.position, _netTargetPos, 20f * Time.deltaTime);
        }
    }

    public override void OnStartNetwork()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        _pred.Initialize(rb);
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);

        if (!IsServerInitialized && !Owner.IsLocalClient)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    protected override void TimeManager_OnTick()
    {
        if (health != null && !health.IsAlive)
        {
            return;
        }

        var input = inputs.ConsumeForTick();
        PerformReplicate(input);

        if (IsServerInitialized)
        {
            CreateReconcile();
        }
            
    }

    public override void CreateReconcile()
    {
        var sd = new StateData
        {
            Position = rb.position,
            Velocity = rb.velocity,
            Grounded = _grounded,
            IsCrouching = _isCrouching,
            IsProne = _isProne,
            IsReloading = _isReloading,
            NextAllowedFireTick = _nextAllowedFireTick,
            NextAllowedJumpTick = _nextAllowedJumpTick
        };

        PerformReconcile(sd);
    }

    [Replicate]
    private void PerformReplicate(InputData rd, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        if (IsServerInitialized)
        {
            IsCrouchingNet.Value = _isCrouching;
            IsProneNet.Value = _isProne;
            SpeedNet.Value = _pred.Rigidbody.velocity.magnitude;
            if (IsAiming.Value != rd.AimHeld)
            {
                IsAiming.Value = rd.AimHeld;
            }
        }

        movement.SimulateStance(rd, ref _isCrouching, ref _isProne);
        movement.SimulateGroundCheck(ref _grounded, _pred.Rigidbody, groundMask);
        movement.SimulateMove(rd, _pred, _grounded, _isCrouching, _isProne);
        movement.SimulateJump(rd, _pred, _grounded, ref _nextAllowedJumpTick, TimeManager.LocalTick);
        shoot.ProcessFire(rd, ref _isReloading, ref _nextAllowedFireTick, TimeManager.LocalTick, this, _pred);
        movement.ApplyDrag(_pred, _grounded, (float)TimeManager.TickDelta);
        movement.ClampSpeed(_pred);

        _pred.Simulate();

        if (IsServerInitialized)
        {
            BroadcastPoseToObservers(rb.position);
        }
    }

    [Reconcile]
    private void PerformReconcile(StateData sd, Channel channel = Channel.Unreliable)
    {

        _grounded = sd.Grounded;
        _isCrouching = sd.IsCrouching;
        _isProne = sd.IsProne;
        _isReloading = sd.IsReloading;
        _nextAllowedFireTick = sd.NextAllowedFireTick;
        _nextAllowedJumpTick = sd.NextAllowedJumpTick;

        rb.position = sd.Position;
        rb.velocity = sd.Velocity;
    }


    public void SetReloading(bool value)
    {
        _isReloading = value;
    }

    [ServerRpc(RequireOwnership = true)]
    private void RpcSendRotation(Quaternion rot)
    {
        rb.MoveRotation(rot);
        RotationNet.Value = rot;
        RpcBroadcastRotation(rot);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcBroadcastRotation(Quaternion rot)
    {
        if (IsOwner) return;
        rb.MoveRotation(rot);
    }

    [Server]
    void BroadcastPoseToObservers(Vector3 pos)
    {
        foreach (var c in Observers)
            if (c != Owner) TargetRecvPose(c, pos.x, pos.y, pos.z);
    }

    [TargetRpc]
    void TargetRecvPose(NetworkConnection _, float px, float py, float pz)
    {
        _netTargetPos = new Vector3(px, py, pz);
    }


    public bool IsGrounded => _grounded;
    public bool IsCrouching => _isCrouching;
    public bool IsProne => _isProne;
    public bool IsReloading => _isReloading;
    public Vector3 PredictedVelocity =>
        _pred != null && _pred.Rigidbody != null
            ? _pred.Rigidbody.velocity
            : Vector3.zero;
    public PredictionRigidbody Body => _pred;
    public PlayerTeam Team => team;
    public PlayerHealth Health => health;
}
