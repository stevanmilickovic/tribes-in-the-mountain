using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Speeds")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float rotationSpeedDegPerSec = 540f; // turn speed

    private Rigidbody _rb;

    private struct InputSnapshot
    {
        public float moveX;
        public float moveY;
        public bool aiming;
        public bool hasYaw;
        public float yaw;
    }

    private InputSnapshot _lastInput;
    private bool _hasInput;

    public override void OnStartServer()
    {
        base.OnStartServer();
        EnsureRigidbody();
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        EnsureRigidbody();

        if (!IsServer && _rb != null)
            _rb.isKinematic = true;
    }

    private void EnsureRigidbody()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (!_hasInput)
            return;

        Vector3 wishMove = Vector3.zero;

        if (Mathf.Abs(_lastInput.moveX) > 0.001f || Mathf.Abs(_lastInput.moveY) > 0.001f)
        {
            wishMove = new Vector3(_lastInput.moveX, 0f, _lastInput.moveY);
            wishMove = Vector3.ClampMagnitude(wishMove, 1f);
        }

        if (_lastInput.hasYaw && (_lastInput.aiming || wishMove.sqrMagnitude > 0f))
        {
            float targetYaw = _lastInput.yaw;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeedDegPerSec * Time.fixedDeltaTime
            );
        }

        if (wishMove.sqrMagnitude > 0f)
        {
            Vector3 moveWorld =
                transform.forward * wishMove.z +
                transform.right * wishMove.x;

            Vector3 newPos = _rb.position + moveWorld * (moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(newPos);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitInputServerRpc(float moveX, float moveY, bool aiming, bool hasYaw, float yaw)
    {
        _lastInput.moveX = moveX;
        _lastInput.moveY = moveY;
        _lastInput.aiming = aiming;
        _lastInput.hasYaw = hasYaw;
        _lastInput.yaw = yaw;
        _hasInput = true;
    }

    public void SendInputToServer(float moveX, float moveY, bool aiming, bool hasYaw, float yaw)
    {
        if (!IsOwner) return;
        SubmitInputServerRpc(moveX, moveY, aiming, hasYaw, yaw);
    }
}
