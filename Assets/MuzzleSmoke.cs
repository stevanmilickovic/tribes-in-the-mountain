using UnityEngine;

public class MuzzleSmoke : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;

    public void PlayAt(Transform muzzle)
    {
        if (ps == null) ps = GetComponentInChildren<ParticleSystem>(true);
        transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
    }
}
