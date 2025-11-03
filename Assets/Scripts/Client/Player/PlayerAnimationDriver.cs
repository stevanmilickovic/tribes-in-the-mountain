using UnityEngine;
using FishNet.Object;
using FishNet.Managing.Timing;

public class PlayerAnimationDriver : NetworkBehaviour
{
    public PlayerInputs input;
    public Rigidbody rb;
    public Animator anim;

    [Header("Animator Setup")]
    public int aimLayerIndex = 1;
    public float aimBlendSpeed = 3f;

    private float _aimWeight;
    private bool _wasAiming;
    private bool wasCrouching;
    private bool wasProne;

    public override void OnStartClient()
    {
        if (!IsOwner) { enabled = false; return; }
        if (anim) anim.applyRootMotion = false;

        TimeManager.OnTick += OnTick;
    }

    public override void OnStopClient()
    {
        if (IsOwner && TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        if (!anim || !input) return;

        float speed = rb ? new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude : input.move.magnitude * 5f;
        if (speed < 0.01f) speed = 0f;

        bool aiming = input.isAiming;
        anim.SetFloat("Speed", aiming ? 0f : speed);
        anim.SetBool("CombatMode", aiming);
        if (aiming && !_wasAiming)
            anim.SetTrigger("Combat");

        if (input.prone)
        {
            if (!wasProne)
            {
                anim.SetBool("Prone", true);
                anim.SetBool("Crouch", false);
                anim.SetBool("Stand", false);
                wasProne = true;
            }
            else
            {
                anim.SetBool("Prone", false);
                anim.SetBool("Stand", true);
                wasProne = false;
            }
        }

        if (input.crouch)
        {
            if (!wasCrouching)
            {
                anim.SetBool("Crouch", true);
                anim.SetBool("Stand", false);
                anim.SetBool("Prone", false);
                wasCrouching = true;
            }
            else
            {
                anim.SetBool("Crouch", false);
                anim.SetBool("Stand", true);
                wasCrouching = false;
            }
        }

        float target = aiming ? 1f : 0f;
        _aimWeight = Mathf.MoveTowards(_aimWeight, target, aimBlendSpeed * Time.deltaTime);
        _aimWeight = Mathf.Clamp01(_aimWeight);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            anim.SetLayerWeight(aimLayerIndex, _aimWeight);

        _wasAiming = aiming;
    }
}
