using UnityEngine;
using FishNet.Object;

public class PlayerAnimationDriver : NetworkBehaviour
{
    public PlayerInputs input;
    public Rigidbody rb;
    public Animator anim;

    [Header("Animator Setup")]
    [Tooltip("Index of the 'Aim' layer in the Animator (from the demo controller it’s 1).")]
    public int aimLayerIndex = 1;
    [Tooltip("How fast to blend the aim layer.")]
    public float aimBlendSpeed = 3f;

    private float _aimWeight;
    private bool _wasAiming; 
    private Vector3 _lastPos;
    private bool _hasLast;

    public PlayerAnimState state;

    public override void OnStartClient()
    {
        if (anim) anim.applyRootMotion = false;
    }

    void LateUpdate()
    {

        float speed = CalculateSpeedFromTransform();

        bool aiming = state.IsAiming.Value;

        anim.SetBool("CombatMode", aiming);

        if (aiming && !_wasAiming)
            anim.SetTrigger("Combat");

        SetAnimatorSpeed(speed);

        anim.SetBool("Prone", state.IsProne.Value);
        anim.SetBool("Crouch", state.IsCrouching.Value);
        anim.SetBool("Stand", !state.IsProne.Value && !state.IsCrouching.Value);

        SetAnimatorLayerWeigth();

        _wasAiming = aiming;
    }

    private float CalculateSpeedFromTransform()
    {
        if (!_hasLast) { _lastPos = transform.position; _hasLast = true; return 0f; }
        Vector3 cur = transform.position;
        Vector3 d = cur - _lastPos; d.y = 0f;
        _lastPos = cur;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float s = d.magnitude / dt;
        return (s < 0.02f) ? 0f : s;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (!state.IsAiming.Value)
        {
            anim.SetFloat("Speed", speed);
        }
        else
        {
            anim.SetFloat("Speed", 0f);
        }
    }

    private void SetAnimatorLayerWeigth()
    {
        float target = state.IsAiming.Value ? 1f : 0f;
        _aimWeight = Mathf.MoveTowards(_aimWeight, target, aimBlendSpeed * Time.deltaTime);
        _aimWeight = Mathf.Clamp01(_aimWeight);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            anim.SetLayerWeight(aimLayerIndex, _aimWeight);
    }
}
