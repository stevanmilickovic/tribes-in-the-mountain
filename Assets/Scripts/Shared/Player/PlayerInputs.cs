using UnityEngine;
using FishNet.Object;

public class PlayerInputs : NetworkBehaviour
{
    public ThirdPersonCam cameraRig;

    public InputData LatestInput;
    public InputData _bufferedInput;

    private bool _prevFireHeldThisFrame;

    public override void OnStartClient()
    {
        if (!IsOwner)
            enabled = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        CollectUnityInput();
    }

    public InputData ConsumeForTick()
    {
        var data = _bufferedInput;
        data.Yaw = LatestInput.Yaw;
        _bufferedInput = GetBufferedInputData(data);
        return data;
    }

    private void CollectUnityInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        bool jumpHeld = Input.GetKey(KeyCode.Space);
        bool aimHeld = Input.GetMouseButton(1);
        bool crouchEdge = Input.GetKeyDown(KeyCode.LeftControl);
        bool proneEdge = Input.GetKeyDown(KeyCode.Z);

        if (crouchEdge) proneEdge = false;
        else if (proneEdge) crouchEdge = false;

        float yaw = cameraRig ? cameraRig.lookYawDeg : 0f;
        float pit = cameraRig ? cameraRig.lookPitchDeg : 0f;

        bool fireHeld = Input.GetMouseButton(0);
        bool fireEdge = fireHeld && !_prevFireHeldThisFrame;

        LatestInput = new InputData
        {
            Move = move,
            JumpHeld = jumpHeld,
            AimHeld = aimHeld,
            FirePressedEdge = fireEdge,
            CrouchPressedEdge = crouchEdge,
            PronePressedEdge = proneEdge,
            Yaw = yaw,
            Pitch = pit
        };

        _bufferedInput.Move = move;
        _bufferedInput.JumpHeld = jumpHeld;
        _bufferedInput.AimHeld = aimHeld;
        _bufferedInput.FirePressedEdge |= fireEdge;
        _bufferedInput.CrouchPressedEdge |= crouchEdge;
        _bufferedInput.PronePressedEdge |= proneEdge;
        _bufferedInput.Yaw = yaw;
        _bufferedInput.Pitch = pit;

        _prevFireHeldThisFrame = fireHeld;

    }

    private InputData GetBufferedInputData(InputData inputData)
    {
        return new InputData
        {
            Move = inputData.Move,
            AimHeld = inputData.AimHeld,
            Yaw = inputData.Yaw,
            Pitch = inputData.Pitch
        };
    }
}
