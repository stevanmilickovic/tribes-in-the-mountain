using UnityEngine;

// The view owns yaw and pitch. Player movement and the aim rig consume those
// angles; neither physics hits nor animated bones can turn the camera.
[DefaultExecutionOrder(-200)]
public class ThirdPersonCam : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.04f;
    [SerializeField, Min(0.01f)] private float aimTransitionTime = 0.12f;
    [SerializeField, Min(0f)] private float teleportSnapDistance = 3f;
    [SerializeField, Min(0f)] private float mouseSensitivityX = 1f;
    [SerializeField, Min(0f)] private float mouseSensitivityY = 1f;
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Stance")]
    [SerializeField, Min(0f)] private float crouchDrop = 0.45f;
    [SerializeField, Min(0f)] private float proneDrop = 0.85f;
    [SerializeField, Min(0.01f)] private float stanceTransitionTime = 0.15f;

    [Header("Camera Positions")]
    [SerializeField, Min(0f)] private float freeDistance = 3.5f;
    [SerializeField, Min(0f)] private float aimDistance = 3f;
    [SerializeField] private float freeShoulderOffset;
    [SerializeField] private float aimShoulderOffset = 0.65f;
    [SerializeField] private GameObject crosshair;

    public float lookYawDeg;
    public float lookPitchDeg;

    private Transform _player;
    private PlayerMotor _playerMotor;
    private Vector3 _smoothedPivot;
    private Vector3 _pivotVelocity;
    private float _currentStanceDrop;
    private float _stanceDropVelocity;
    private float _currentDistance;
    private float _distanceVelocity;
    private float _currentShoulderOffset;
    private float _shoulderVelocity;
    private bool _isAiming;

    public void SetPlayerInfo(Transform player)
    {
        _player = player;
        _playerMotor = player != null ? player.GetComponent<PlayerMotor>() : null;
        if (player == null)
            return;

        Vector3 forward = transform.forward;
        lookYawDeg = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        lookPitchDeg = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        lookPitchDeg = Mathf.Clamp(lookPitchDeg, minPitch, maxPitch);
        _currentStanceDrop = GetStanceDrop();
        _stanceDropVelocity = 0f;
        _smoothedPivot = player.position + pivotOffset + Vector3.down * _currentStanceDrop;
        _pivotVelocity = Vector3.zero;
        _currentDistance = freeDistance;
        _currentShoulderOffset = freeShoulderOffset;
        _distanceVelocity = 0f;
        _shoulderVelocity = 0f;
    }

    private void Update()
    {
        if (_player == null)
            return;

        if (Cursor.lockState == CursorLockMode.Locked && !PlayerInputs.Paused)
        {
            lookYawDeg = Mathf.Repeat(
                lookYawDeg + Input.GetAxis("Mouse X") * mouseSensitivityX, 360f);
            lookPitchDeg = Mathf.Clamp(
                lookPitchDeg + Input.GetAxis("Mouse Y") * mouseSensitivityY,
                minPitch,
                maxPitch);
        }

        _isAiming = Cursor.lockState == CursorLockMode.Locked &&
            !PlayerInputs.Paused && Input.GetMouseButton(1);
        if (crosshair != null && crosshair.activeSelf != _isAiming)
            crosshair.SetActive(_isAiming);
    }

    private void LateUpdate()
    {
        if (_player == null)
            return;

        _currentStanceDrop = Mathf.SmoothDamp(
            _currentStanceDrop,
            GetStanceDrop(),
            ref _stanceDropVelocity,
            stanceTransitionTime);
        Vector3 targetPivot = _player.position + pivotOffset +
            Vector3.down * _currentStanceDrop;
        if (Vector3.Distance(_smoothedPivot, targetPivot) > teleportSnapDistance)
        {
            _smoothedPivot = targetPivot;
            _pivotVelocity = Vector3.zero;
        }
        else
        {
            _smoothedPivot = Vector3.SmoothDamp(
                _smoothedPivot, targetPivot, ref _pivotVelocity, followSmoothTime);
        }

        Quaternion viewRotation = Quaternion.Euler(-lookPitchDeg, lookYawDeg, 0f);
        _currentDistance = Mathf.SmoothDamp(
            _currentDistance,
            _isAiming ? aimDistance : freeDistance,
            ref _distanceVelocity,
            aimTransitionTime);
        _currentShoulderOffset = Mathf.SmoothDamp(
            _currentShoulderOffset,
            _isAiming ? aimShoulderOffset : freeShoulderOffset,
            ref _shoulderVelocity,
            aimTransitionTime);

        transform.SetPositionAndRotation(
            _smoothedPivot + viewRotation * new Vector3(
                _currentShoulderOffset, 0f, -_currentDistance),
            viewRotation);
    }

    private float GetStanceDrop()
    {
        if (_playerMotor == null)
            return 0f;
        if (_playerMotor.IsProne)
            return proneDrop;
        if (_playerMotor.IsCrouching)
            return crouchDrop;
        return 0f;
    }
}
