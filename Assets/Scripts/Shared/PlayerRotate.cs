using UnityEngine;
using FishNet.Object;

public class PlayerRotate : NetworkBehaviour
{
    [Header("References")]
    public Transform playerObj;
    public PlayerInputs input;

    [Header("Settings")]
    public float rotationSpeed = 12f;

    public override void OnStartClient()
    {
        if (!IsServerInitialized) { enabled = false; }
    }

    void Update()
    {
        if (input == null || playerObj == null) return;

        Vector2 m = input.move;
        if (m.sqrMagnitude <= 0.0001f) return;

        float yaw = input.lookYawDeg;
        Quaternion basis = Quaternion.Euler(0f, yaw, 0f);
        Vector3 fwd = basis * Vector3.forward;
        Vector3 right = basis * Vector3.right;

        Vector3 inputDir = (fwd * m.y + right * m.x).normalized;
        if (inputDir.sqrMagnitude <= 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
        playerObj.rotation = Quaternion.Slerp(playerObj.rotation, target, Time.deltaTime * rotationSpeed);
    }
}
