using UnityEngine;
using FishNet.Object;

public class PlayerRotate : NetworkBehaviour
{
    [Header("References")]
    public Transform playerObj;
    public PlayerInputs input;

    [Header("Settings")]
    public float rotationSpeed = 12f;

    public float aimingRotationSpeed = 20f;

    public PlayerHealth health;

    private Vector3 inputDir;

    public override void OnStartClient()
    {
        if (!IsServerInitialized) { 
            enabled = false; 
        }
    }

    void FixedUpdate()
    {
        if (input == null || playerObj == null || (health != null && !health.IsAlive)) return;

        if (input.isAiming) {
            Aim();
            return;
        }

        if (!isMoving()) return;

        Vector3 inputDir = GetInputDirection();

        if (inputDir.sqrMagnitude <= 0.0001f) return;

        Quaternion tgt = Quaternion.LookRotation(inputDir, Vector3.up);
        playerObj.rotation = Quaternion.Slerp(playerObj.rotation, tgt, Time.deltaTime * rotationSpeed);
    }

    private void Aim()
    {
        float yaw = input.lookYawDeg;
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);
        float spd = Mathf.Max(rotationSpeed, aimingRotationSpeed);
        playerObj.rotation = Quaternion.Slerp(playerObj.rotation, target, Time.deltaTime * spd);
    }

    private bool isMoving()
    {
        if (input == null) return false;
        Vector2 m = input.move;
        return m.sqrMagnitude > 0.0001f;
    }

    private Vector3 GetInputDirection()
    {
        if (input == null) return Vector3.zero;
        Vector2 m = input.move;
        float basisYaw = input.lookYawDeg;
        Quaternion basis = Quaternion.Euler(0f, basisYaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;
        return (fwd * m.y + right * m.x).normalized;
    }
}
