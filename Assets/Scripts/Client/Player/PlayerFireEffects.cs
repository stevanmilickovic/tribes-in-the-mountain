using System.Collections.Generic;
using UnityEngine;

// Fire RPCs can arrive before the model and aim rig finish this frame.
[DefaultExecutionOrder(300)]
public class PlayerFireEffects : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;

    private readonly List<Vector3> _pendingShotDirections = new();

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor>();
    }

    private void OnEnable()
    {
        if (motor != null)
            motor.FireVisualRequested += QueueShot;
    }

    private void OnDisable()
    {
        if (motor != null)
            motor.FireVisualRequested -= QueueShot;
        _pendingShotDirections.Clear();
    }

    private void QueueShot(Vector3 direction)
    {
        _pendingShotDirections.Add(direction);
    }

    private void LateUpdate()
    {
        if (_pendingShotDirections.Count == 0)
            return;

        if (motor != null && motor.smokePrefab != null && motor.muzzle != null)
        {
            for (int i = 0; i < _pendingShotDirections.Count; i++)
                SpawnSmoke(_pendingShotDirections[i]);
        }

        _pendingShotDirections.Clear();
    }

    private void SpawnSmoke(Vector3 direction)
    {
        GameObject smoke = Instantiate(
            motor.smokePrefab,
            motor.muzzle.position,
            Quaternion.LookRotation(direction, Vector3.up));

        ParticleSystem particles = smoke.GetComponentInChildren<ParticleSystem>();
        if (particles == null)
        {
            Destroy(smoke);
            return;
        }

        particles.Play();
        Destroy(smoke, particles.main.duration + particles.main.startLifetime.constantMax);
    }
}
