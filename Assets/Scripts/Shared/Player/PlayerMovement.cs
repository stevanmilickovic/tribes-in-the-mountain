using UnityEngine;
using FishNet.Object.Prediction;

public class PlayerMovement
{
    public float moveSpeed = 6f;
    public float airMultiplier = 0.6f;
    public float rotationSpeed = 12f;
    public float aimingRotationSpeed = 720f;
    public float jumpForce = 3f;
    public float groundDrag = 4f;
    public float playerHeight = 1.8f;
    public uint jumpCooldownTicks = 64;

    public void SimulateStance(InputData rd, ref bool isCrouching, ref bool isProne)
    {
        if (rd.CrouchPressedEdge)
        {
            isCrouching = !isCrouching;
            if (isCrouching) isProne = false;
        }
        if (rd.PronePressedEdge)
        {
            isProne = !isProne;
            if (isProne) isCrouching = false;
        }
    }

    public void SimulateGroundCheck(ref bool grounded, Rigidbody body, LayerMask groundMask)
    {
        var origin = body.position + Vector3.up * 0.05f;
        grounded = Physics.Raycast(origin, Vector3.down, playerHeight * 0.5f + 0.3f, groundMask, QueryTriggerInteraction.Ignore);
    }

    public void SimulateMove(InputData rd, PredictionRigidbody body, bool grounded, bool isCrouching, bool isProne, bool isReloading)
    {
        float yaw = rd.Yaw;
        Quaternion basis = Quaternion.Euler(0f, yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;
        Vector2 move = rd.Move;
        Vector3 moveDir = fwd * move.y + right * move.x;

        float speedMultiplier = 1f;
        if (isCrouching)
            speedMultiplier = 0f;
        else if (isProne)
            speedMultiplier = 0.3f;

        if (rd.AimHeld)
            speedMultiplier *= 0.6f;

        //if (isReloading)
        //    speedMultiplier *= 0.4f;

        Vector3 force = moveDir.normalized * moveSpeed * 10f * speedMultiplier;

        if (grounded)
            body.AddForce(force, ForceMode.Force);
        else
            body.AddForce(force * airMultiplier, ForceMode.Force);
    }

    public void SimulateRotation(InputData rd, PredictionRigidbody body, float tickDelta)
    {
        if (rd.AimHeld)
        {
            // Aim targets can be collision-adjusted and camera-offset. The body must
            // follow the replicated camera yaw instead, otherwise close geometry can
            // make weapon aiming feed back into root rotation.
            Quaternion targetRot = Quaternion.Euler(0f, rd.Yaw, 0f);

            float spd = Mathf.Max(rotationSpeed, aimingRotationSpeed);
            Quaternion newRot = Quaternion.RotateTowards(body.Rigidbody.rotation, targetRot, spd * tickDelta);
            body.Rigidbody.MoveRotation(newRot);
            return;
        }

        Quaternion basis = Quaternion.Euler(0f, rd.Yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;
        Vector3 inputDir = (fwd * rd.Move.y + right * rd.Move.x);
        if (inputDir.sqrMagnitude <= 0.0001f) return;

        Quaternion tgt = Quaternion.LookRotation(inputDir.normalized, Vector3.up);
        Quaternion rot = Quaternion.Slerp(body.Rigidbody.rotation, tgt, rotationSpeed * tickDelta);
        body.Rigidbody.MoveRotation(rot);
    }

    public void SimulateJump(InputData rd, PredictionRigidbody body, bool grounded, ref uint nextAllowedJumpTick, uint currentTick)
    {
        if (!rd.JumpHeld) return;
        if (!grounded) return;
        if (currentTick < nextAllowedJumpTick) return;

        Vector3 vel = body.Rigidbody.linearVelocity;
        vel.y = 0f;
        body.Rigidbody.linearVelocity = vel;
        body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        nextAllowedJumpTick = currentTick + jumpCooldownTicks;
    }

    public void ApplyDrag(PredictionRigidbody body, bool grounded, float tickDelta)
    {
        if (!grounded) return;
        Vector3 v = body.Rigidbody.linearVelocity;
        v.x *= 1f / (1f + groundDrag * tickDelta);
        v.z *= 1f / (1f + groundDrag * tickDelta);
        body.Rigidbody.linearVelocity = v;
    }

    public void ClampSpeed(PredictionRigidbody body)
    {
        Vector3 v = body.Rigidbody.linearVelocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        if (flat.magnitude > moveSpeed)
        {
            Vector3 limited = flat.normalized * moveSpeed;
            body.Rigidbody.linearVelocity = new Vector3(limited.x, v.y, limited.z);
        }
    }
}
