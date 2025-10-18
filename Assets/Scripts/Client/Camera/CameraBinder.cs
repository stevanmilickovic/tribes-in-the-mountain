using UnityEngine;
using FishNet.Object;
using Cinemachine;

public class CameraBinder : NetworkBehaviour
{
    [Header("References inside Player Prefab")]
    public PlayerInputs playerInputs;
    public Transform orientation;
    public Transform playerObj;

    public override void OnStartClient()
    {
        if (!IsOwner)
            return;

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("PlayerCameraBinder: No main camera found in scene.");
            return;
        }

        ThirdPersonCam cam = mainCam.GetComponentInChildren<ThirdPersonCam>();
        if (cam == null)
        {
            Debug.LogWarning("PlayerCameraBinder: Could not find ThirdPersonCam under the main camera.");
            return;
        }

        cam.player = transform;
        cam.orientation = orientation;

        CinemachineFreeLook freeLook = cam.GetComponent<CinemachineFreeLook>();
        if (freeLook != null)
        {
            freeLook.Follow = playerObj;
            freeLook.LookAt = playerObj;
        }

        if (playerInputs != null)
            playerInputs.cameraRig = cam;

        Debug.Log("PlayerCameraBinder: Successfully bound local camera rig to player prefab.");
    }
}
