using UnityEngine;
using FishNet.Object;

public class PlayerAnimationDriver : NetworkBehaviour
{
    public PlayerInputs input;
    public Rigidbody rb;
    public Transform playerObj;
    public Animator anim;

    public override void OnStartClient()
    {
        if (!IsOwner) { enabled = false; return; }
    }

    void Update()
    {
        if (!anim || !input) return;
        float speed = rb ? new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude
                         : input.move.magnitude * 5f;
        anim.SetFloat("Speed", speed);
        anim.SetBool("CombatMode", input.isAiming);
        anim.SetBool("Combat", input.isAiming);
    }
}
