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
        Debug.Log($"[Inputs] ticking {name}");

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 m = new Vector2(h, v);
        if (m.sqrMagnitude > 1f) m.Normalize();

        bool j = Input.GetKey(KeyCode.Space);

        bool fire = Input.GetMouseButton(0);
        bool aim = Input.GetMouseButton(1);

        float yaw = cameraRig ? cameraRig.lookYawDeg : 0f;
        float pit = cameraRig ? cameraRig.lookPitchDeg : 0f;

        move = m; jump = j; lookYawDeg = yaw; lookPitchDeg = pit;
        firePressed = fire; isAiming = aim;

        SendInputServerRpc(m, j, yaw, pit, fire, aim);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = false)]
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
