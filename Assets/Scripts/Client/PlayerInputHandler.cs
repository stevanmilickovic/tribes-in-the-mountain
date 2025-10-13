using UnityEngine;
using FishNet.Object;

[DisallowMultipleComponent]
public class PlayerInputHandler : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerLook look;

    [Header("Bindings")]
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (look == null) look = GetComponent<PlayerLook>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        enabled = IsOwner;
    }

    private void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S
        bool aiming = Input.GetKey(aimKey);

        bool isMoving = Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f;

        bool hasYaw = false;
        float yawToSend = 0f;

        if ((isMoving || aiming) && look != null)
        {
            hasYaw = true;
            yawToSend = look.GetCameraYaw();
        }

        movement.SendInputToServer(h, v, aiming, hasYaw, yawToSend);
    }
}
