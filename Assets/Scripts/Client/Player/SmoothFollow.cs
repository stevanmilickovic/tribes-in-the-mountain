using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 20f;

    private void Awake()
    {
        transform.parent = null;
    }

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * followSpeed);
    }
}
