using UnityEngine;
using FishNet.Object;

public class PlayerLook : NetworkBehaviour
{
    [Header("Anchor (position only)")]
    [SerializeField] private Transform cameraPivot; // player child at head height

    [Header("Camera")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float heightOffset = 0.0f;

    [Header("Look")]
    [SerializeField] private float sensitivity = 1.2f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 80f;


    private Transform _yawRig;
    private Transform _pitchRig;
    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        if (cameraPivot == null)
            cameraPivot = transform.Find("CameraPivot");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner) { enabled = false; return; }

        if (sceneCamera == null) sceneCamera = Camera.main;
        if (cameraPivot == null)
        {
            var auto = new GameObject("CameraPivot_Auto");
            cameraPivot = auto.transform;
            cameraPivot.SetParent(transform, false);
            cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);
        }

        _yawRig = new GameObject("CamYawRig_WS").transform;
        _pitchRig = new GameObject("CamPitchRig_WS").transform;
        _pitchRig.SetParent(_yawRig, false);

        if (sceneCamera != null)
        {
            sceneCamera.transform.SetParent(_pitchRig, false);
        }

        _yaw = transform.eulerAngles.y;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        RepositionRigAndCamera(immediate: true);
    }

    private void Update()
    {
        if (!IsOwner) return;

        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        _yaw += mouseX * sensitivity;
        _pitch -= mouseY * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        RepositionRigAndCamera(immediate: false);
    }

    private void RepositionRigAndCamera(bool immediate)
    {
        if (_yawRig == null || _pitchRig == null || cameraPivot == null || sceneCamera == null) return;

        Vector3 pivotPos = cameraPivot.position + new Vector3(0f, heightOffset, 0f);
        _yawRig.position = pivotPos;
        _yawRig.rotation = Quaternion.Euler(0f, _yaw, 0f);

        _pitchRig.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        Vector3 camLocal = new Vector3(0f, 0f, -distance);
        sceneCamera.transform.localPosition = camLocal;

        sceneCamera.transform.rotation = Quaternion.LookRotation(pivotPos - sceneCamera.transform.position, Vector3.up);
    }

    public float GetCameraYaw()
    {
        if (sceneCamera == null) return transform.eulerAngles.y;
        Vector3 fwd = sceneCamera.transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        fwd.Normalize();
        float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        if (yaw < 0f) yaw += 360f;
        return yaw;
    }

    public void SetYawToPlayerForward()
    {
        _yaw = transform.eulerAngles.y;
    }
}
