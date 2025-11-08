using UnityEngine;
using FishNet.Object;

public class PlayerAnimationDriver : NetworkBehaviour
{
    public Animator anim;
    public PlayerMotor motor;
    public PlayerInputs inputs;

    public int aimLayerIndex = 1;
    public float aimBlendSpeed = 3f;

    private float _aimWeight;
    private bool _wasAiming;
    private Vector3 _lastPos;
    private bool _hasLast;

    private void LateUpdate()
    {
        if (anim == null || motor == null) return;

        bool aiming = motor.IsAiming.Value;
        float speed = motor.IsOwner ? motor.PredictedVelocity.magnitude : motor.SpeedNet.Value;

        anim.SetBool("CombatMode", aiming);
        if (aiming && !_wasAiming)
            anim.SetTrigger("Combat");

        anim.SetBool("Prone", motor.IsProneNet.Value);
        anim.SetBool("Crouch", motor.IsCrouchingNet.Value);
        anim.SetBool("Stand", !motor.IsCrouchingNet.Value && !motor.IsProneNet.Value);

        if (aiming || motor.IsCrouching || speed < 0.01f)
            anim.SetFloat("Speed", 0f);
        else
            anim.SetFloat("Speed", speed);

        float target = aiming ? 1f : 0f;
        _aimWeight = Mathf.MoveTowards(_aimWeight, target, aimBlendSpeed * Time.deltaTime);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            anim.SetLayerWeight(aimLayerIndex, _aimWeight);

        _wasAiming = aiming;
    }

    private float CalculateSpeedFromTransform()
    {
        if (!_hasLast) { _lastPos = transform.position; _hasLast = true; return 0f; }
        Vector3 cur = transform.position;
        Vector3 delta = cur - _lastPos; delta.y = 0f;
        _lastPos = cur;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float s = delta.magnitude / dt;
        return (s < 0.02f) ? 0f : s;
    }
}
