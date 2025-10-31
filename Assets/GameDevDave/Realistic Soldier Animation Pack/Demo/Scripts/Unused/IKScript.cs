using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKScript : MonoBehaviour
{
    [HideInInspector]
    public Transform target;
    [HideInInspector]
    public AvatarIKGoal IKGoal;
    Animator animator;
    float weight;
    [HideInInspector]
    public float speed;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void IKLerp (AvatarIKGoal Limb, Transform Target, float Speed) 
    {
        target = Target;
        IKGoal = Limb;
        speed = Speed;
    }

    public void IKLerp (AvatarIKGoal Limb, Transform Target, float Speed, bool pin) 
    {
        target = Target;
        IKGoal = Limb;
        speed = Speed;

        // We can pin the object we constrain the IK to, to the IK's transform if the starting location/rotation is set in the animation already
        if (pin)
        target.SetPositionAndRotation(animator.GetIKPosition(IKGoal), animator.GetIKRotation(IKGoal));
        // else {target.localPosition = Vector3.zero; target.localRotation = Quaternion.identity;}
    }

    // Just keep it running as long as the weight is turned down it won't do a thing...
    void OnAnimatorIK()
    {
        if (target != null) 
        {
            weight = Mathf.Clamp(weight += Time.fixedDeltaTime * speed, 0, 1);     
            animator.SetIKPositionWeight(IKGoal, weight);
            animator.SetIKRotationWeight(IKGoal, weight);
            animator.SetIKPosition(IKGoal, target.position);
            animator.SetIKRotation(IKGoal, target.rotation); 
        }        
    }
}
