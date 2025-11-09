using UnityEngine;

[DefaultExecutionOrder(10000)]
public class AimBoneDriver : MonoBehaviour
{
    public Transform bone;          // spine or chest bone
    public Transform aimTransform;  // child of the rifle, aligned with muzzle
    public Transform target;        // world target point
    [Range(0f, 1f)] public float weight = 1f;
    public int iterations = 10;

    void LateUpdate()
    {
        if (bone == null || aimTransform == null || target == null)
            return;

        Vector3 aimDir = aimTransform.forward;
        Vector3 targetDir = target.position - aimTransform.position;
        Quaternion aimTowards = Quaternion.FromToRotation(aimDir, targetDir);
        Quaternion blended = Quaternion.Slerp(Quaternion.identity, aimTowards, weight / iterations * 3.5f);

        for (int i = 0; i < iterations; i++)
            bone.rotation = blended * bone.rotation;
    }
}
