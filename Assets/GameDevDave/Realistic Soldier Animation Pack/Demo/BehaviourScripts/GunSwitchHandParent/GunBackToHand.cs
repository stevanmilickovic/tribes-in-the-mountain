using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunBackToHand : StateMachineBehaviour
{
    /*
    List of clips this behaviour script is used in:

    GrenadeHandThrow
    GrenadeCrouchThrow
    CoverGrenadeThrowLeft
    CoverGrenadeThrowRight

    */

    public float speed;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state    
    // override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    // 
    // }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    // override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    // 
    // }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Look up the DemoScript
        var demo = GameObject.FindWithTag("GameController").GetComponent<DemoSystemScript>();

        // Return Gun to normal parent
        demo.gun.transform.SetParent(demo.hand_Right.transform);

        // Return back to normal
        animator.GetComponent<GunScript>().GunTransitionManipulator(false, speed, true);
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
