using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunScript : MonoBehaviour
{
    public DemoSystemScript demo;
    [Header("Gun Transition for Prone & Crawl Animations")]
    public GameObject gun;
    Vector3 gunHandPosition, gunPos;
    Quaternion gunHandRotation, gunRot;
    public Transform gunArmsLocation;
    public float speed;
    public float easeIn;
    public bool doTransition;

    // line 97
    [Header("Bolt Animation Event")]
    public Animator gunAnim; 

    void Awake () 
    {
        // Save the original local transform location/rotation of the Gun within its parent's transform
        // So we can always come back to it
        gunHandPosition = gun.transform.localPosition;
        gunHandRotation = gun.transform.localRotation;

        // gunPos and gunRot will be the variables we swap new locations & rotations in
        // We start off with the original starting position & rotation of our gun
        gunPos =  gunHandPosition;
        gunRot = gunHandRotation;
    }

    // We use FixedUpdate because a consistent motion requires a consistent framerate
    void FixedUpdate()
    {
        // We will use easeIn as an increment to add an extra smooth transition
        // And give us the control over the speed to match/sync our animations
        easeIn = Mathf.Clamp (easeIn += speed * Time.fixedDeltaTime, 1, 10);

        // Make sure this code only gets run when it's needed (isolation)
        // Transition Enabled
        if (doTransition) 
        {
            // Smooth Lerp Interpolation
            // Will continuously transition the gun to the location we have set in the second Slerp parameter
            // It gets frequently updated with an increase of our fixed timestep until destination is reached 
            gun.transform.localPosition = Vector3.Slerp (gun.transform.localPosition, gunPos, easeIn * Time.fixedDeltaTime);
            gun.transform.localRotation = Quaternion.Slerp (gun.transform.localRotation, gunRot, easeIn * Time.fixedDeltaTime);

            // Disable this block, disable transition when both transforms are similar in position (approximation)
            if (FloatApproximation(gun.transform.localPosition.x, gunPos.x, 0.00001f)) 
            doTransition = false;
        }        
    }

    // A function to check for approximation (like the == operator but with the addition of putting in your own threshold)
    private bool FloatApproximation(float a, float b, float tolerance)
    {
        return (Mathf.Abs(a - b) < tolerance);
    }

    // The method that allows us control over the transition
    // Thanks to this logic
    // We are now able to simply put behaviourscripts in the animation clips that need this transition (controlling it with animation)
    // This is a very powerful tool in Unity's animator system 
    public void GunTransitionManipulator (bool GunToArms, float speed_, bool EaseIn) 
    {
        if (GunToArms) 
        {   // Gun To Arms
            gunPos = gunArmsLocation.localPosition;
            gunRot = gunArmsLocation.localRotation;
        }
        else 
        {   // Gun to Hand
            gunPos = gunHandPosition;
            gunRot = gunHandRotation;
        }

        // Choose transition style, Ease in or Ease out?
        if (EaseIn) 
        {
            // An EaseIn    // Configure the speed
            easeIn = 1;     speed = speed_;
        }
        else 
        {
            // An EaseOut   // Configure the speed
            easeIn = 10;    speed = speed_ * -1;
        }

        // Enable & Start the Transition
        doTransition = true;
    }

    // Play with these settings to see how it works
    void Update () 
    {   // Testing
        if (Input.GetKeyDown(KeyCode.Z)) {GunTransitionManipulator(false, 50, true);} 
        if (Input.GetKeyDown(KeyCode.X)) {GunTransitionManipulator(true, 50, true);}         
    }

    // Animation Event Trigger for the bolt in-out Animation 
    public void DoBolt () 
    {
        gun.GetComponent<Animator>().SetTrigger("Bolt");
    }
}
