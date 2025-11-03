using UnityEngine;
using FishNet.Object;
using FishNet.Managing;
using FishNet;

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

    [Header("Posture")]
    public bool crouch;
    public bool prone;

    private bool _prevJumpHeld;
    private bool _prevFireHeld;
    private bool _prevAimHeld;
    private bool _prevCrouchHeld;
    private bool _prevProneHeld;

    public override void OnStartClient()
    {
        if (!IsOwner) { enabled = false; return; }
        var tm = InstanceFinder.TimeManager;
        if (tm != null)
        {
            tm.OnTick += OnTick;
            tm.OnPostTick += OnPostTick;
        }
    }

    public override void OnStopClient()
    {
        var tm = InstanceFinder.TimeManager;
        if (tm != null)
        {
            tm.OnTick -= OnTick;
            tm.OnPostTick -= OnPostTick;
        }
    }

    private void OnTick()
    {
        CollectInputsTick();
    }

    private void OnPostTick()
    {
        jump = false;
        crouch = false;
        prone = false;
        firePressed = false;
    }

    private void CollectInputsTick()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector2 movementVector = new Vector2(horizontal, vertical);
        if (movementVector.sqrMagnitude > 1f) movementVector.Normalize();

        bool jumpHeld = Input.GetKey(KeyCode.Space);
        bool fireHeld = Input.GetMouseButton(0);
        bool aimHeld = Input.GetMouseButton(1);
        bool crouchHeld = Input.GetKey(KeyCode.LeftControl);
        bool proneHeld = Input.GetKey(KeyCode.Z);

        bool jumpDown = jumpHeld && !_prevJumpHeld;
        bool fireDown = fireHeld && !_prevFireHeld;
        bool crouchDown = crouchHeld && !_prevCrouchHeld;
        bool proneDown = proneHeld && !_prevProneHeld;

        if (crouchDown) proneDown = false;
        if (proneDown) crouchDown = false;

        float yaw = cameraRig ? cameraRig.lookYawDeg : 0f;
        float pit = cameraRig ? cameraRig.lookPitchDeg : 0f;

        move = movementVector;
        jump = jumpDown;
        lookYawDeg = yaw;
        lookPitchDeg = pit;
        firePressed = fireDown;
        isAiming = aimHeld;
        crouch = crouchDown;
        prone = proneDown;

        _prevJumpHeld = jumpHeld;
        _prevFireHeld = fireHeld;
        _prevAimHeld = aimHeld;
        _prevCrouchHeld = crouchHeld;
        _prevProneHeld = proneHeld;
    }
}
