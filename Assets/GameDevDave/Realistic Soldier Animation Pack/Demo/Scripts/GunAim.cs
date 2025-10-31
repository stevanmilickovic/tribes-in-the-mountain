using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAim : MonoBehaviour
{
    public DemoSystemScript demo;
    public Transform targetTransform, aimTransform, bone;
    public int iterations = 10;
    [Range (0,1)]
    public float weight = 1;
    public bool DoGunAim;

    private void OnDrawGizmos() 
    {
        // Draw a line for visual feedback in the Editor
        Debug.DrawLine(demo.raycast.transform.position, demo.raycast.transform.position + demo.raycast.transform.forward * 50);
    }

    // We use LateUpdate to override Update and the animations
    void LateUpdate()
    {
        if (DoGunAim) 
        {
            // Enable target for testing
            targetTransform.gameObject.SetActive (true);
            // Local variable to store the target location in
            Vector3 targetPosition = targetTransform.position;

            // Looping the AimAtTarget method results in a more accurate & consistent aim
            // The more iterations we do, the more accurate the value is 
            for (int i = 0; i < iterations; i++)
            {
                AimAtTarget(bone, targetPosition, weight / iterations * 3.5f); 
            }  
        }      
        else 
            targetTransform.gameObject.SetActive (false);    
    }

    private void AimAtTarget (Transform bone, Vector3 targetPosition, float weight) 
    {
        // Local var to store
        Vector3 aimDirection = aimTransform.forward;
        // Local var to store, Create direction vector (direction = destination - origin)
        Vector3 targetDirection = targetPosition - aimTransform.position;
        // Translate the direction of two vector3's to an angle
        Quaternion aimTowards = Quaternion.FromToRotation(aimDirection, targetDirection);
        // Transition/Blend slider to control the weight of the GunAim
        Quaternion blendedRotation = Quaternion.Slerp (Quaternion.identity, aimTowards, weight);
        // Aim the bone (in this case the spine) towards the target through the calculated angle
        bone.rotation = blendedRotation * bone.rotation;
    }

    // Method for Toggle Button
    public void EnableGunAim () 
    {
        DoGunAim = !DoGunAim;
    }
}
