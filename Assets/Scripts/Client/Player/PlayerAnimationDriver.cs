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

    public override void OnStartClient()
    {
        if (!IsOwner) { enabled = false; return; }
        if (anim) anim.applyRootMotion = false;
    }

    void Update()
    {
        if (!anim || !input) return;

        float speed = 0f;
        if (rb) speed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        else speed = input.move.magnitude * 5f;

        if (speed < 0.01f) speed = 0f;

        bool aiming = input.isAiming;

        if (!aiming)
        {
            anim.SetFloat("Speed", speed);
        }
        else
        {
            anim.SetFloat("Speed", 0f);
        }

        anim.SetBool("CombatMode", aiming);

        if (aiming && !_wasAiming)
            anim.SetTrigger("Combat");

        float target = aiming ? 1f : 0f;
        _aimWeight = Mathf.MoveTowards(_aimWeight, target, aimBlendSpeed * Time.deltaTime);
        _aimWeight = Mathf.Clamp01(_aimWeight);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            anim.SetLayerWeight(aimLayerIndex, _aimWeight);

        _wasAiming = aiming;
    }
}
