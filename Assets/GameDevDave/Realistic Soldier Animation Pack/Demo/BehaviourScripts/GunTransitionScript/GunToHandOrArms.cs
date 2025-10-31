using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunToHandOrArms : StateMachineBehaviour
{
    /*
    List of clips this behaviour script is used in:

    IdleProne

    */

    // This is a way to be 100% sure that the gun is going to be 
    // properly positioned in the arms during this animation clip's playback

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.GetBool("CombatMode"))
        animator.GetComponent<GunScript>().GunTransitionManipulator(false, 50, true);
        else
        animator.GetComponent<GunScript>().GunTransitionManipulator(true, 10, false);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    // override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    // 
    // }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {
    //
    // }

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
