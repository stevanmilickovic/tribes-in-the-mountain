using UnityEngine;

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

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (player == null || orientation == null)
            return;

        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        if (viewDir.sqrMagnitude > 0.0001f)
            orientation.forward = viewDir.normalized;

        Vector3 flatFwd = orientation.forward; flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude > 0.0001f)
            lookYawDeg = Mathf.Repeat(Mathf.Atan2(flatFwd.x, flatFwd.z) * Mathf.Rad2Deg, 360f);

        Vector3 camFwd = transform.forward;
        float pitch = Mathf.Asin(Mathf.Clamp(camFwd.y, -1f, 1f)) * Mathf.Rad2Deg;
        lookPitchDeg = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}
