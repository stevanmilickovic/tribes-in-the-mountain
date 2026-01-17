using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System;
using System.Globalization;
using UnityEngine;
using static FishNet.Utility.Template.TickNetworkBehaviour;
using static UnityEngine.UI.GridLayoutGroup;

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
    public Transform muzzle;
    public GameObject smokePrefab;

    public readonly SyncVar<bool> IsAiming = new();
    public readonly SyncVar<Quaternion> RotationNet = new();
    private float _nextRotSend;

    public readonly SyncVar<bool> IsCrouchingNet = new();
    public readonly SyncVar<bool> IsProneNet = new();
    public readonly SyncVar<float> SpeedNet = new();
    public readonly SyncVar<bool> IsReloadingNet = new();
    public readonly SyncVar<bool> HasAmmoNet = new();

    private PredictionRigidbody _pred;
    private bool _isCrouching;
    private bool _isProne;
    private bool _grounded;
    private bool _isReloading;
    private uint _nextAllowedFireTick;
    private uint _nextAllowedJumpTick;
    private bool _hasAmmo = true;
    private int _skipBroadcastTicks = 0;

    private Vector3 _netTargetPos;

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
        public bool HasAmmo;

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

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (inputs == null) inputs = GetComponent<PlayerInputs>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (team == null) team = GetComponent<PlayerTeam>();
        if (aimGun == null) aimGun = GetComponent<AimGun>();
    }

    public override void OnStartNetwork()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (inputs == null) inputs = GetComponent<PlayerInputs>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (team == null) team = GetComponent<PlayerTeam>();
        if (aimGun == null) aimGun = GetComponent<AimGun>();

        if (aimGun != null && aimGun.playerMotor != this)
            aimGun.playerMotor = this;

        _pred.Initialize(rb);
        SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);

        if (!IsServerInitialized && !Owner.IsLocalClient)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    void Update()
    {
        if (!IsOwner || inputs == null) return;

        var rd = inputs.LatestInput;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

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
            rb.position = Vector3.Lerp(rb.position, _netTargetPos, 20f * Time.deltaTime);
    }

    protected override void TimeManager_OnTick()
    {
        if (health != null && !health.IsAlive)
            return;

        var input = inputs.ConsumeForTick();
        PerformReplicate(input);

        if (IsServerInitialized)
            CreateReconcile();
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
            NextAllowedJumpTick = _nextAllowedJumpTick,
            HasAmmo = _hasAmmo
        };

        PerformReconcile(sd);
    }

    [Replicate]
    private void PerformReplicate(InputData rd, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        bool effectiveAimHeld = rd.AimHeld && !_isReloading;

        InputData simRd = rd;
        simRd.AimHeld = effectiveAimHeld;

        if (IsServerInitialized)
        {
            IsCrouchingNet.Value = _isCrouching;
            IsProneNet.Value = _isProne;
            SpeedNet.Value = _pred.Rigidbody.velocity.magnitude;
            IsReloadingNet.Value = _isReloading;
            HasAmmoNet.Value = _hasAmmo;

            if (IsAiming.Value != effectiveAimHeld)
                IsAiming.Value = effectiveAimHeld;
        }

        movement.SimulateStance(simRd, ref _isCrouching, ref _isProne);
        if (simRd.CrouchPressedEdge || simRd.PronePressedEdge)
            GetComponent<PlayerAudio>()?.PlayRuffle();

        movement.SimulateGroundCheck(ref _grounded, _pred.Rigidbody, groundMask);
        movement.SimulateMove(simRd, _pred, _grounded, _isCrouching, _isProne, _isReloading);
        movement.SimulateJump(simRd, _pred, _grounded, ref _nextAllowedJumpTick, TimeManager.LocalTick);

        shoot.ProcessFire(simRd, ref _isReloading, ref _nextAllowedFireTick, TimeManager.LocalTick, this, _pred, HasAmmoNet.Value);

        movement.ApplyDrag(_pred, _grounded, (float)TimeManager.TickDelta);
        movement.ClampSpeed(_pred);

        _pred.Simulate();

        if (IsServerInitialized)
            BroadcastPoseToObservers(rb.position);
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
        _hasAmmo = sd.HasAmmo;

        if (_skipBroadcastTicks <= 0)
            rb.position = sd.Position;

        rb.velocity = sd.Velocity;
    }

    public void BindLoadout(AimGun newAimGun, Transform newMuzzle)
    {
        if (newAimGun != null)
            aimGun = newAimGun;
        else if (aimGun == null)
            aimGun = GetComponent<AimGun>();

        muzzle = newMuzzle;

        if (aimGun != null && aimGun.playerMotor != this)
            aimGun.playerMotor = this;
    }

    public void SetReloading(bool value)
    {
        _isReloading = value;
        if (IsServerInitialized)
            IsReloadingNet.Value = value;
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
        if (_skipBroadcastTicks > 0)
        {
            _skipBroadcastTicks--;
            return;
        }

        foreach (var c in Observers)
            if (c != Owner)
                TargetRecvPose(c, pos.x, pos.y, pos.z);
    }

    [TargetRpc]
    void TargetRecvPose(NetworkConnection _, float px, float py, float pz)
    {
        _netTargetPos = new Vector3(px, py, pz);
    }

    [ObserversRpc(BufferLast = false)]
    public void RpcOnFire(Vector3 dir)
    {
        var audio = GetComponent<PlayerAudio>();
        if (audio != null)
            audio.PlayShot();

        if (smokePrefab != null && muzzle != null)
        {
            var rot = Quaternion.LookRotation(dir, Vector3.up);

            var smokeObj = Instantiate(smokePrefab, muzzle.position, rot);
            var ps = smokeObj.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
                ps.Play();

            Destroy(smokeObj, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }

    [Server]
    public void Teleport(Vector3 pos, Quaternion rot)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.position = pos;
        rb.rotation = rot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        _isReloading = false;
        _hasAmmo = true;

        _skipBroadcastTicks = 2;

        ClearReplicateCache();
        RpcClearPredictionCache();

        RpcTeleport(pos, rot);
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcTeleport(Vector3 pos, Quaternion rot)
    {
        if (IsOwner) return;

        rb.position = pos;
        rb.rotation = rot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        _netTargetPos = pos;

        ClearReplicateCache();
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcClearPredictionCache()
    {
        ClearReplicateCache();
    }

    public void ConsumeAmmo()
    {
        _hasAmmo = false;
        if (IsServerInitialized)
            HasAmmoNet.Value = false;
    }

    public void RestoreAmmo()
    {
        _hasAmmo = true;
        if (IsServerInitialized)
            HasAmmoNet.Value = true;
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
