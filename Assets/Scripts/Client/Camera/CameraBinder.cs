using UnityEngine;
using FishNet.Object;

public class CameraBinder : NetworkBehaviour
{
    [Header("References inside Player Prefab")]
    public PlayerInputs playerInputs;
    public PlayerMotor playerMotor;
    public Transform orientation;
    public Transform playerObj;
    public Transform targetObj;

    private ThirdPersonCam _cam;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        if (playerInputs == null) playerInputs = GetComponent<PlayerInputs>();
        if (playerMotor == null) playerMotor = GetComponent<PlayerMotor>();

        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        _cam = mainCam.GetComponent<ThirdPersonCam>();
        if (_cam == null)
            return;

        Bind();

        if (playerInputs != null)
            playerInputs.cameraRig = _cam;

        var atc = mainCam.GetComponent<AimTargetController>();
        if (atc != null)
        {
            atc.playerMotor = playerMotor;
            atc.target = targetObj;
        }
    }

    public void Bind()
    {
        if (!IsOwner) return;
        if (_cam == null) return;
        if (orientation == null) return;

        Transform follow = playerObj != null ? playerObj : transform;
        _cam.SetPlayerInfo(transform, orientation, follow);
    }
}
