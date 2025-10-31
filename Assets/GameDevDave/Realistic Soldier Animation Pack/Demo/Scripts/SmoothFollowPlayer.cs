using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowPlayer : MonoBehaviour
{
    public Transform target;
    Vector3 currentVelocity = Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, target.position + Vector3.up, ref currentVelocity, 0.05f);
    }
}
