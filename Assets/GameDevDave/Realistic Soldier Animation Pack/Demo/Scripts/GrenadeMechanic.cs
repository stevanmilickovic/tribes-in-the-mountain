using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeMechanic : MonoBehaviour
{
    [HideInInspector]
    public bool startTimer;
    float timer = 5;
    void FixedUpdate () 
    { // Gets started by the GrenadeScript through an animation event trigger in the grenadethrow animation
        if (startTimer) // Decrement float with Time.deltaTime will we hit zero
        timer -= Time.deltaTime;
    }
    void OnCollisionStay (Collision coll) 
    {   // When we hit zero 
        if (timer < 0)
        // Do particle explosion
        // Destroy this GameObject
        Destroy(gameObject);
    }
}