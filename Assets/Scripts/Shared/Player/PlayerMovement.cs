using UnityEngine;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump = true;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;

    public PlayerInputs input;
    public PlayerHealth health;

    private Rigidbody rb;
    private bool grounded;
    private bool isCrouching;
    private bool isProne;
    private Vector3 moveDirection;

    public struct MoveData : IReplicateData
    {
        public Vector2 move;
        public bool jump;
        public bool crouch;
        public bool prone;
        public float yaw;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Quaternion rotation;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        rb = GetComponent<Rigidbody>();
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
        if (health != null && !health.IsAlive) return;

        MoveData md = new MoveData
        {
            move = input.move,
            jump = input.jump,
            crouch = input.crouch,
            prone = input.prone,
            yaw = input.lookYawDeg
        };

        Replicate(md);
    }

    private void OnPostTick()
    {
        if (!IsServerInitialized) return;
        CreateReconcile();
    }

    [Replicate]
    private void Replicate(MoveData md, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        ApplyInput(md);
        GroundCheck();
        ApplyForces(md);
        SpeedControl();

        if (rb.angularVelocity.sqrMagnitude > 0.0001f)
            rb.angularVelocity *= 0.9f;
        else
            rb.angularVelocity = Vector3.zero;

        rb.drag = grounded ? groundDrag : 0f;
    }

    [Reconcile]
    private void Reconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
    {
        transform.SetPositionAndRotation(rd.position, rd.rotation);
        rb.velocity = rd.velocity;
    }

    private void ApplyInput(MoveData md)
    {
        Quaternion basis = Quaternion.Euler(0f, md.yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;
        moveDirection = fwd * md.move.y + right * md.move.x;

        if (md.crouch)
        {
            isCrouching = !isCrouching;
            isProne = false;
        }
        if (md.prone)
        {
            isProne = !isProne;
            isCrouching = false;
        }

        if (md.jump && grounded && readyToJump)
        {
            readyToJump = false;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            TimeManager.Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void GroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
    }

    private void ApplyForces(MoveData md)
    {
        float speedMult = 1f;
        if (isCrouching) speedMult = 0.5f;
        if (isProne) speedMult = 0.3f;

        Vector3 dir = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.zero;

        float baseForce = moveSpeed * 10f * speedMult;
        if (grounded)
            rb.AddForce(dir * baseForce, ForceMode.Force);
        else
            rb.AddForce(dir * baseForce * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        float speedMult = 1f;
        if (isCrouching) speedMult = 0.5f;
        if (isProne) speedMult = 0.3f;

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float max = moveSpeed * speedMult;
        if (flatVel.magnitude > max)
        {
            Vector3 limited = flatVel.normalized * max;
            rb.velocity = new Vector3(limited.x, rb.velocity.y, limited.z);
        }
    }

    private void ResetJump() => readyToJump = true;

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            position = transform.position,
            velocity = rb.velocity,
            rotation = transform.rotation
        };
        Reconcile(rd, Channel.Unreliable);
    }
}
