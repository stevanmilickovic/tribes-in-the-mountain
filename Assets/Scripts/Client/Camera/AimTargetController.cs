using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimTargetController : MonoBehaviour
{
    public PlayerMotor playerMotor;
    public Transform target;
    public float maxDistance = 50f;
    public float minDistance = 1f;
    public LayerMask hitMask;

    void LateUpdate()
    {
        if (playerMotor == null || target == null) return;

        bool aiming = playerMotor.IsAiming.Value;
        if (!aiming)
        {
            if (target.gameObject.activeSelf)
                target.gameObject.SetActive(false);
            return;
        }

        if (!target.gameObject.activeSelf)
            target.gameObject.SetActive(true);

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance > minDistance)
                target.position = hit.point;
            else
                target.position = cam.transform.position + cam.transform.forward * minDistance;
        }
        else
        {
            target.position = cam.transform.position + cam.transform.forward * maxDistance;
        }
    }
}
