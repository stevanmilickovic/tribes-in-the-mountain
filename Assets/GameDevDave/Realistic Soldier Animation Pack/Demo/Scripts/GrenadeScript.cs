using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeScript : MonoBehaviour
{
    public DemoSystemScript demo;
    public GameObject grenade;
    public Transform grenadeHandLocation;
    Animator animator;
    GameObject nadeInst; 

    void Start () 
    {
        animator = GetComponent<Animator>();
    }

    // These two methods are controlled by Animation Event Triggers
    // You can find these Animation Events in all of the "Anticipation" and "Throw" grenade animation clips

    public void EquipGrenade () 
    {
        // Assign and instantiate the grenade object
        nadeInst = Instantiate(grenade, demo.hand_Right.transform.position, demo.hand_Right.transform.rotation);
        // Parent the nade object to the right hand 
        nadeInst.transform.SetParent(demo.hand_Right.transform);
        // Give it the right positional and rotational values now that it has local coordinates
        // The nade itself is a child of the hand just like the grenadeHandLocation transform 
        // They share the same parent, so they can share the same world/local coordinates
        nadeInst.transform.SetPositionAndRotation(grenadeHandLocation.position, grenadeHandLocation.rotation);
    }
    
    public void ThrowGrenade () 
    {
        // Let's start the grenade's timer mechanic (GrenadeMechanic Script)
        nadeInst.GetComponent<GrenadeMechanic>().startTimer = true;

        // Calculate the direction we want to throw the grenade in 
        // Luckily, at the right time when the Animation Trigger Event executes this method 
        // The red axis of the grenade has the correct direction
        // So let's just use the transform.right of the grenade object with a little bit of world up...

        // Let's first unparent the grenade so it can fly through world coordinates undisturbed
        nadeInst.transform.SetParent(null);
        // Fetch the rigidbody of the grenade object to apply physics
        Rigidbody rigidbody = nadeInst.GetComponent<Rigidbody>(); 
        // Enable Physics
        rigidbody.isKinematic = false;
        // Apply physics to launch the nade out of the hand (local.x direction and a little world up)
        rigidbody.AddForce(nadeInst.transform.right * 8 + Vector3.up * 4, ForceMode.Impulse);
        // Let's add a random rotational force for believability
        rigidbody.AddTorque(new Vector3 (Random.Range(0, 100), Random.Range(0, 100), Random.Range(0, 100)), ForceMode.Impulse);
    }    

    // Animation Event Trigger for putting the gun down in prone mode during grenade animation 
    public void LayGunDown () 
    {
        transform.GetComponent<GunScript>().gun.transform.SetParent(null);
    }
    // Animation Event Trigger for grabbing the gun back in prone mode during grenade animation 
    public void PickGunUp () 
    {
        transform.GetComponent<GunScript>().gun.transform.SetParent(demo.hand_Right.transform);

        if (animator.GetBool("CombatMode"))
        animator.GetComponent<GunScript>().GunTransitionManipulator(false, 50, true);
        else
        animator.GetComponent<GunScript>().GunTransitionManipulator(true, 10, false);
    }
}
