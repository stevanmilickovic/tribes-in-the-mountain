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
    private bool _wasDead;

    public string lastDeathAnim;

    private PlayerAudio _audio;

    private void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (inputs == null) inputs = GetComponent<PlayerInputs>();
        if (health == null) health = GetComponent<PlayerHealth>();
        _audio = GetComponent<PlayerAudio>();
    }

    public void BindAnimator(Animator newAnimator)
    {
        anim = newAnimator;

        _aimWeight = 0f;
        _wasAiming = false;

        if (anim != null)
        {
            if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
                anim.SetLayerWeight(aimLayerIndex, 0f);
        }
    }

    private void LateUpdate()
    {
        if (motor == null || health == null) return;
        if (anim == null) return;

        if (!health.IsAlive)
        {
            if (!_wasDead)
            {
                if (motor.IsProneNet.Value) lastDeathAnim = "DeathProne";
                else if (motor.IsCrouchingNet.Value) lastDeathAnim = "DeathCrouched";
                else if (motor.PredictedVelocity.magnitude > 2f) lastDeathAnim = "DeathRun";
                else lastDeathAnim = "DeathStanding";

                anim.SetTrigger("Death");
                _wasDead = true;
            }

            return;
        }

        if (_wasDead)
        {
            _wasDead = false;
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
        if (_audio == null) return;

        bool grounded = motor.IsGrounded;
        bool moving = speed > 0.1f;
        bool prone = motor.IsProne;
        bool crouch = motor.IsCrouching;

        if (grounded && moving)
        {
            if (prone)
                _audio.PlayCrawlLoop();
            else if (!crouch)
                _audio.PlayFootstepLoop();
            else
                _audio.StopMovementLoop();
        }
        else
        {
            if (_audio.source != null && _audio.source.isPlaying &&
                (_audio.source.clip == _audio.footstep || _audio.source.clip == _audio.crawl))
                _audio.StopMovementLoop();
        }
    }
}
