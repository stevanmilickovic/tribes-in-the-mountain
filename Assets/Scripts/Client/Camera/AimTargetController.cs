using UnityEngine;

// A distant point on the view ray is used for muzzle convergence and firing.
// Nearby collision never changes the direction of the view or the aim rig.
[DefaultExecutionOrder(100)]
public class AimTargetController : MonoBehaviour
{
    public PlayerMotor playerMotor;
    public Transform target;
    [Min(0.01f)] public float maxDistance = 50f;

    private void LateUpdate()
    {
        if (playerMotor == null || target == null)
            return;

        bool aiming = playerMotor.IsAiming.Value;
        if (target.gameObject.activeSelf != aiming)
            target.gameObject.SetActive(aiming);

        if (aiming)
            target.position = transform.position + transform.forward * maxDistance;
    }
}
