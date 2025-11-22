using UnityEngine;
using FishNet.Object;

public class PlayerAnimationDriver : NetworkBehaviour
{
    public Animator anim;
    public PlayerMotor motor;
    public PlayerInputs inputs;
    public PlayerHealth health;

    public int aimLayerIndex = 1;
    public float aimBlendSpeed = 3f;

    private float _aimWeight;
    private bool _wasAiming;
    private Vector3 _lastPos;
    private bool _hasLast;
    private bool wasDead;

    public string lastDeathAnim;

    private void LateUpdate()
    {
        if (anim == null || motor == null) return;

        if (!health.IsAlive)
        {
            wasDead = true;

            if (motor.IsProneNet.Value) lastDeathAnim = "DeathProne";
            else if (motor.IsCrouchingNet.Value) lastDeathAnim = "DeathCrouched";
            else if (motor.PredictedVelocity.magnitude > 2f) lastDeathAnim = "DeathRun";
            else lastDeathAnim = "DeathStanding";

            anim.SetTrigger("Death");
            return;
        }

        if (health.IsAlive && wasDead)
        {
            wasDead = false;
            anim.SetBool("Death", false);
            anim.SetTrigger("Reset");
        }

        bool aiming = motor.IsAiming.Value;
        float speed = motor.IsOwner ? motor.PredictedVelocity.magnitude : motor.SpeedNet.Value;

        HandleAudio(speed);

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

    private void HandleAudio(float speed)
    {
        var audio = motor.GetComponent<PlayerAudio>();
        if (audio == null) return;

        bool grounded = motor.IsGrounded;
        bool moving = speed > 0.1f;
        bool prone = motor.IsProne;
        bool crouch = motor.IsCrouching;

        if (grounded && moving)
        {
            if (prone)
                audio.PlayCrawlLoop();
            else if (!crouch)
                audio.PlayFootstepLoop();
            else
                audio.StopMovementLoop();
        }
        else
        {
            if (audio.source.isPlaying && (audio.source.clip == audio.footstep || audio.source.clip == audio.crawl))
                audio.StopMovementLoop();
        }
    }

}
