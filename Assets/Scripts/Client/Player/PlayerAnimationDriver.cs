using UnityEngine;
using FishNet.Object;

public class PlayerAnimationDriver : NetworkBehaviour
{
    private enum Stance
    {
        Standing,
        Crouching,
        Prone
    }

    private readonly struct StanceTransitionPair
    {
        public readonly int EnterHash;
        public readonly int ReturnHash;
        public readonly string EnterName;
        public readonly string ReturnName;
        public readonly Stance Start;
        public readonly Stance End;

        public StanceTransitionPair(string enterName, string returnName, Stance start, Stance end)
        {
            EnterHash = Animator.StringToHash(enterName);
            ReturnHash = Animator.StringToHash(returnName);
            EnterName = enterName;
            ReturnName = returnName;
            Start = start;
            End = end;
        }
    }

    private static readonly StanceTransitionPair[] BaseStanceTransitions =
    {
        new StanceTransitionPair("StandingToProne", "ProneToStanding", Stance.Standing, Stance.Prone),
        new StanceTransitionPair("DiveToProne", "ProneToStanding", Stance.Standing, Stance.Prone),
        new StanceTransitionPair("StandingToCrouch", "CrouchToStanding", Stance.Standing, Stance.Crouching),
        new StanceTransitionPair("CrouchToProne", "ProneToCrouch", Stance.Crouching, Stance.Prone)
    };

    private static readonly StanceTransitionPair[] AimStanceTransitions =
    {
        new StanceTransitionPair("AimStandingToProne", "AimProneToStanding", Stance.Standing, Stance.Prone),
        new StanceTransitionPair("AimStandingToCrouch", "AimCrouchToStanding", Stance.Standing, Stance.Crouching),
        new StanceTransitionPair("AimCrouchToProne", "AimProneToCrouch", Stance.Crouching, Stance.Prone)
    };

    private const float StanceReversalBlendDuration = 0.05f;

    public Animator anim;
    public PlayerMotor motor;
    public PlayerInputs inputs;
    public PlayerHealth health;

    public int aimLayerIndex = 1;
    public float aimBlendSpeed = 3f;

    private float _aimWeight;
    private bool _wasAiming;
    private bool _wasDead;
    private bool _wasReloading;
    private Stance _previousStance;
    private bool _hasPreviousStance;

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
        _hasPreviousStance = false;

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
            _hasPreviousStance = false;
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

        bool reloading = motor.IsReloadingNet.Value;

        if (reloading && !_wasReloading)
        {
            anim.SetTrigger("Reload");
            Debug.Log("Set reload trigger");
        }

        _wasReloading = reloading;

        bool aiming = motor.IsAiming.Value;
        float speed = motor.IsOwner ? motor.PredictedVelocity.magnitude : motor.SpeedNet.Value;

        HandleAudio(speed);

        anim.SetBool("CombatMode", aiming);
        if (aiming && !_wasAiming)
            anim.SetTrigger("Combat");

        Stance stance = GetStance();
        anim.SetBool("Prone", stance == Stance.Prone);
        anim.SetBool("Crouch", stance == Stance.Crouching);
        anim.SetBool("Stand", stance == Stance.Standing);

        if (speed < 0.01f)
            anim.SetFloat("Speed", 0f);
        else
            anim.SetFloat("Speed", speed);

        if (_hasPreviousStance && stance != _previousStance)
            ReverseInterruptedStanceTransitions(stance);
        _previousStance = stance;
        _hasPreviousStance = true;

        float target = aiming ? 1f : 0f;
        _aimWeight = Mathf.MoveTowards(_aimWeight, target, aimBlendSpeed * Time.deltaTime);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            anim.SetLayerWeight(aimLayerIndex, _aimWeight);

        _wasAiming = aiming;
    }

    private Stance GetStance()
    {
        bool prone = motor.IsOwner ? motor.IsProne : motor.IsProneNet.Value;
        bool crouching = motor.IsOwner ? motor.IsCrouching : motor.IsCrouchingNet.Value;

        if (prone) return Stance.Prone;
        if (crouching) return Stance.Crouching;
        return Stance.Standing;
    }

    private void ReverseInterruptedStanceTransitions(Stance destination)
    {
        ReverseInterruptedStanceTransition(0, destination, BaseStanceTransitions);
        if (aimLayerIndex >= 0 && aimLayerIndex < anim.layerCount)
            ReverseInterruptedStanceTransition(aimLayerIndex, destination, AimStanceTransitions);
    }

    private void ReverseInterruptedStanceTransition(
        int layerIndex,
        Stance destination,
        StanceTransitionPair[] pairs)
    {
        // During a blend, the incoming state is the visible stance transition we want to undo.
        if (anim.IsInTransition(layerIndex) &&
            TryReverseStanceTransition(anim.GetNextAnimatorStateInfo(layerIndex), layerIndex, destination, pairs))
            return;

        TryReverseStanceTransition(anim.GetCurrentAnimatorStateInfo(layerIndex), layerIndex, destination, pairs);
    }

    private bool TryReverseStanceTransition(
        AnimatorStateInfo state,
        int layerIndex,
        Stance destination,
        StanceTransitionPair[] pairs)
    {
        foreach (StanceTransitionPair pair in pairs)
        {
            string reverseName;
            if (state.shortNameHash == pair.EnterHash && destination == pair.Start)
                reverseName = pair.ReturnName;
            else if (state.shortNameHash == pair.ReturnHash && destination == pair.End)
                reverseName = pair.EnterName;
            else
                continue;

            int reverseHash = Animator.StringToHash(anim.GetLayerName(layerIndex) + "." + reverseName);
            if (!anim.HasState(layerIndex, reverseHash))
                continue;

            // The paired clips depict opposite motions. Complementary progress starts
            // the return close to the pose reached when the input was reversed.
            float reverseTime = 1f - Mathf.Clamp01(state.normalizedTime);
            anim.CrossFade(reverseHash, StanceReversalBlendDuration, layerIndex, reverseTime);
            return true;
        }

        return false;
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
