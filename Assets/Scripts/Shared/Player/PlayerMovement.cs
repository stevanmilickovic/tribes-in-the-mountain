using UnityEngine;
using FishNet.Object;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public PlayerInputs input;
    public PlayerHealth health;

    Vector3 moveDirection;
    Rigidbody rb;

    public PlayerAnimState animState;

    private bool isCrouching;
    private bool isProne;

    private bool _lastCrouchPulse;
    private bool _lastPronePulse;

    public override void OnStartServer()
    {
        rb = ServerNetworkPrefabsUtil.setGameObjectRigidbody(gameObject);
        enabled = true;
        readyToJump = true;
    }

    public override void OnStartClient()
    {
        if (!IsServerInitialized)
        {
            rb = ClientNetworkPrefabsUtil.setGameObjectRigidbody(gameObject);
            enabled = false;
        }
    }

    private void Update()
    {
        animState.IsAiming.Value = (input != null && input.isAiming);
    }

    private void FixedUpdate()
    {
        if (health != null && !health.IsAlive) return;

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MovePlayer();

        SpeedControl();

        rb.drag = grounded ? groundDrag : 0f;

        if (input != null && input.jump && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        if (input == null || rb == null) return;

        float yaw = input.lookYawDeg;
        Quaternion basis = Quaternion.Euler(0f, yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;

        Vector2 m = input.move;
        moveDirection = fwd * m.y + right * m.x;

        float speedMultiplier = 1f;
        if (isCrouching)
        {
            speedMultiplier = 0f;
        }
        
        if (isProne)
        {
            speedMultiplier = 0.3f;
        }

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * speedMultiplier, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier * speedMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump() => readyToJump = true;

    public void ServerToggleCrouch()
    {
        isCrouching = !isCrouching;
        if (isCrouching) isProne = false;
        if (animState != null)
        {
            animState.IsCrouching.Value = isCrouching;
            animState.IsProne.Value = isProne;
        }
    }
    public void ServerToggleProne()
    {
        isProne = !isProne;
        if (isProne) isCrouching = false;
        if (animState != null)
        {
            animState.IsCrouching.Value = isCrouching;
            animState.IsProne.Value = isProne;
        }
    }
}
