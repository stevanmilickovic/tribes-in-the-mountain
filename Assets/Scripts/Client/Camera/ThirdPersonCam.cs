using Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;

    [Header("Output (read-only)")]
    public float lookYawDeg;
    public float lookPitchDeg;

    [Header("Pitch Clamp")]
    public float minPitch = -89f;
    public float maxPitch = 89f;

    [Header("Cinemachine Vcams")]
    public CinemachineFreeLook freeCam;
    public CinemachineFreeLook aimCam;
    int activePriority = 11;
    int inactivePriority = 10;

    public GameObject crosshair;

    bool _wasAiming;

    private PlayerInputs playerInputs;

    public void SetPlayerInfo(Transform player, Transform orientation, Transform playerObj)
    {
        this.player = player;
        this.orientation = orientation;
        playerInputs = player.GetComponent<PlayerInputs>();

        freeCam.Follow = playerObj;
        freeCam.LookAt = playerObj;

        aimCam.Follow = playerObj;
        aimCam.LookAt = playerObj;
    }

    void LateUpdate()
    {
        if (player == null || orientation == null) return;

        SetPitchAndYaw();

        UpdateCam();
    }

    void SetPitchAndYaw()
    {
        Vector3 camFwd = transform.forward;

        if (camFwd.sqrMagnitude > 0.0001f)
            orientation.rotation = Quaternion.LookRotation(camFwd, Vector3.up);

        Vector3 flat = Vector3.ProjectOnPlane(camFwd, Vector3.up);
        if (flat.sqrMagnitude > 0.0001f)
            lookYawDeg = Mathf.Repeat(Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg, 360f);

        float pitch = Mathf.Asin(Mathf.Clamp(camFwd.y, -1f, 1f)) * Mathf.Rad2Deg;
        lookPitchDeg = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCam()
    {
        bool aiming = playerInputs != null && playerInputs.isAiming;
        if (aiming != _wasAiming)
        {
            if (aiming)
            {
                crosshair.SetActive(true);

                aimCam.m_XAxis.Value = freeCam.m_XAxis.Value;
                aimCam.m_YAxis.Value = freeCam.m_YAxis.Value;

                aimCam.PreviousStateIsValid = true;
                aimCam.ForceCameraPosition(transform.position, transform.rotation);

                aimCam.Priority = activePriority;
                freeCam.Priority = inactivePriority;
            }
            else
            {
                crosshair.SetActive(false);

                freeCam.m_XAxis.Value = aimCam.m_XAxis.Value;
                freeCam.m_YAxis.Value = aimCam.m_YAxis.Value;

                freeCam.PreviousStateIsValid = true;
                freeCam.ForceCameraPosition(transform.position, transform.rotation);

                freeCam.Priority = activePriority;
                aimCam.Priority = inactivePriority;
            }
        }

        _wasAiming = aiming;
    }
}
