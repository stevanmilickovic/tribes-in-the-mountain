using UnityEngine;
using FishNet.Object;
using Cinemachine;

public class CameraBinder : NetworkBehaviour
{
    [Header("References inside Player Prefab")]
    public PlayerInputs playerInputs;
    public PlayerMotor playerMotor;
    public Transform orientation;
    public Transform playerObj;
    public Transform targetObj;

    public override void OnStartClient()
    {
        if (!IsOwner)
            return;

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return;
        }

        ThirdPersonCam cam = mainCam.GetComponent<ThirdPersonCam>();
        if (cam == null)
        {
            return;
        }

        cam.SetPlayerInfo(transform, orientation, playerObj);

        if (playerInputs != null)
            playerInputs.cameraRig = cam;

        AimTargetController atc = mainCam.GetComponent<AimTargetController>();

        if (atc != null)
        {
            atc.playerMotor = playerMotor;
            atc.target = targetObj;
        }
    }
}
