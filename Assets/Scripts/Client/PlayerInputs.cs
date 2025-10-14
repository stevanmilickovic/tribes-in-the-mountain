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

    public override void OnStartClient()
    {
        if (!IsOwner) enabled = false;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 m = new Vector2(h, v);
        if (m.sqrMagnitude > 1f) m.Normalize();

        bool j = Input.GetKey(KeyCode.Space);

        float yaw = cameraRig ? cameraRig.lookYawDeg : 0f;
        float pit = cameraRig ? cameraRig.lookPitchDeg : 0f;

        move = m; jump = j; lookYawDeg = yaw; lookPitchDeg = pit;

        SendInputServerRpc(m, j, yaw, pit);
    }

    [ServerRpc(RequireOwnership = true, RunLocally = false)]
    private void SendInputServerRpc(Vector2 m, bool j, float yaw, float pit)
    {
        move = m;
        jump = j;
        lookYawDeg = yaw;
        lookPitchDeg = pit;
    }
}
