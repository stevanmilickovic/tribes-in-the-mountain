using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionScript : MonoBehaviour
{
    public DemoSystemScript demo;
    JoyStickScript joyStick;
    Animator animator;
    AnimatorStateInfo animatorStateInfo;
    AnimatorClipInfo animatorClipInfo;
    public bool moving, cover;
    public Quaternion controlledTurnValue, orderedTurnValue;

    void Start () 
    {
        animator = GetComponent<Animator>();
        joyStick = demo.joyStick;
    }

    void OnAnimatorMove () 
    {
        // Get the current info on the animator's State and the State's animation clip (base) [state]
        animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        animatorClipInfo = animator.GetCurrentAnimatorClipInfo(0)[0];

        // Are we moving? Establish first if the player is moving 
        if (joyStick.direction.magnitude > joyStick.deadzone)
        {
            // Then establish with which animations we wanna give the player root rotation control (moving is true)
            if (animatorStateInfo.IsTag  ("Running") || animatorStateInfo.IsTag  ("Crawling")) 
            moving = true; 
        }
        else moving = false;

        // Run this code and give Animator's Rootmotion full control when we are 'not moving'
        if (!moving && !cover) 
        {
            transform.position += animator.deltaPosition;
            transform.rotation = animator.deltaRotation * transform.rotation;
        } // Run this code and give player rotational control over the rootmotion when we 'are moving' 
        else if (moving)
        {
            transform.position += animator.deltaPosition;

            // Here we receive the DemoSystemScript's value in the turnValue variable
            // turnvalue is the Quaternion that holds an (euler) rotation.y value 
            // We use a Slerp to have a smoother transition towards the next rotational value
            transform.rotation = Quaternion.Slerp (transform.rotation, controlledTurnValue, 2 * Time.deltaTime);
        }

        // If we click Cover, the "orderedTurnValue" variable will lead the character to the cover position (on the pillar)
        if (cover) 
        {
            transform.position += animator.deltaPosition;
            transform.rotation = Quaternion.Slerp (transform.rotation, orderedTurnValue, 100 * Time.deltaTime);
        }
    }
}
