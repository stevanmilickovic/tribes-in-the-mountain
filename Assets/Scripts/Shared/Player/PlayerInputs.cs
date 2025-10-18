using UnityEngine;
using FishNet.Object;

public class PlayerInputs : NetworkBehaviour
{
    [Header("References")]
    public ThirdPersonCam cameraRig;

    [Header("Output (read-only)")]
    public Vector2 move;
    public bool jump;
    public float lookYawDeg;
    public float lookPitchDeg;

    [Header("Combat")]
    public bool firePressed;
    public bool isAiming;

    public override void OnStartClient()
    {
        if (!IsOwner) enabled = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        CollectInputs();

        SendInputServerRpc(move, jump, lookYawDeg, lookPitchDeg, firePressed, isAiming);
    }

    private void CollectInputs()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 movementVector = new Vector2(horizontal, vertical);
        if (movementVector.sqrMagnitude > 1f) movementVector.Normalize();

        bool jumpKey = Input.GetKey(KeyCode.Space);

        bool fire = Input.GetMouseButton(0);
        bool aim = Input.GetMouseButton(1);

        float yaw = cameraRig ? cameraRig.lookYawDeg : 0f;
        float pit = cameraRig ? cameraRig.lookPitchDeg : 0f;

        move = movementVector; 
        jump = jumpKey; 
        lookYawDeg = yaw; 
        lookPitchDeg = pit;
        firePressed = fire; 
        isAiming = aim;
    }

    [ServerRpc(RequireOwnership = true, RunLocally = false)]
    private void SendInputServerRpc(Vector2 m, bool j, float yaw, float pit, bool fire, bool aim)
    {
        move = m;
        jump = j;
        lookYawDeg = yaw;
        lookPitchDeg = pit;
        firePressed = fire;
        isAiming = aim;
    }
}
