using UnityEngine;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using FishNet.Object.Synchronizing;

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

    public readonly SyncVar<bool> IsAiming = new();
    private bool _isAimingLocal;
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

    private struct StateData : IReconcileData
    {
        public PredictionRigidbody Body;
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

    public override void OnStartNetwork()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        Debug.Log(rb);
        _pred.Initialize(rb);
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
    }

    protected override void TimeManager_OnTick()
    {
        if (health != null && !health.IsAlive) return;
        var input = inputs != null ? inputs.ConsumeForTick() : default;
        PerformReplicate(input);
        CreateReconcile();
    }

    public override void CreateReconcile()
    {
        var sd = new StateData
        {
            Body = _pred,
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
        _isAimingLocal = rd.AimHeld;
        if (IsServerInitialized && IsAiming.Value != rd.AimHeld)
            IsAiming.Value = rd.AimHeld;
        if (IsServerInitialized)
        {
            IsCrouchingNet.Value = _isCrouching;
            IsProneNet.Value = _isProne;
            SpeedNet.Value = _pred.Rigidbody.velocity.magnitude;
        }


        movement.SimulateStance(rd, ref _isCrouching, ref _isProne);
        movement.SimulateGroundCheck(ref _grounded, _pred.Rigidbody, groundMask);
        movement.SimulateMove(rd, _pred, _grounded, _isCrouching, _isProne);
        movement.SimulateRotation(rd, _pred, (float)TimeManager.TickDelta);
        movement.SimulateJump(rd, _pred, _grounded, ref _nextAllowedJumpTick, TimeManager.LocalTick);
        shoot.ProcessFire(rd, ref _isReloading, ref _nextAllowedFireTick, TimeManager.LocalTick, this, _pred);
        movement.ApplyDrag(_pred, _grounded, (float)TimeManager.TickDelta);
        movement.ClampSpeed(_pred);
        _pred.Simulate();
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
        _pred.Reconcile(sd.Body);
    }

    public void SetReloading(bool value)
    {
        _isReloading = value;
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
