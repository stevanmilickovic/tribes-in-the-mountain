using System.Collections;
using System.Collections.Generic;
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

    public override void OnStartServer()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = false;
        enabled = true;
        readyToJump = true;
    }

    public override void OnStartClient()
    {
        Debug.Log($"[Move] ENABLE on {name}");
        if (!IsServerInitialized)
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            enabled = false;
        }
    }

    private void Update()
    {
        if (health != null && !health.IsAlive) return;

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        SpeedControl();

        if (rb != null)
            rb.drag = grounded ? groundDrag : 0f;

        if (input != null && input.jump && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void FixedUpdate()
    {
        if (health != null && !health.IsAlive) return;
        MovePlayer();
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

        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
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
}
