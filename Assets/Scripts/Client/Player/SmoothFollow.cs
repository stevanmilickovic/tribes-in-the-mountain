using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 20f;
    public float snapDistance = 2f; // distance above which we hard-snap

    private void Awake()
    {
        transform.parent = null;
    }

    void LateUpdate()
    {
        if (!target) return;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > snapDistance)
        {
            // Teleport-style snap (e.g. after respawn)
            transform.position = target.position;
            transform.rotation = target.rotation;
        }
        else
        {
            // Normal smooth follow
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * followSpeed);
        }
    }
}
