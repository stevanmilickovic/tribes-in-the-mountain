using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;

public class CaptureZone : NetworkBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private Team attackingTeam = Team.TeamB;
    [SerializeField] private float captureRatePerSecond = 1f;
    [SerializeField] private float decayPerSecond = 0.10f;

    [Header("Sync Vars (read-only on clients)")]
    private readonly SyncVar<Team> teamOwner = new();
    private readonly SyncVar<float> progress = new();

    private readonly Collider[] _buffer = new Collider[16];
    private float _accum;

    private MatchController match => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        teamOwner.Value = Team.None;
        progress.Value = 0f;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        _accum += Time.deltaTime;
        if (_accum < 0.25f) return;
        _accum = 0f;

        int attackers = 0;
        int defenders = 0;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _buffer, LayerMask.GetMask("PlayerHitbox"));
        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || !health.IsAlive) continue;

            var teamComp = col.GetComponentInParent<PlayerTeam>();
            if (teamComp == null) continue;

            if (teamComp.team.Value == attackingTeam)
                attackers++;
            else
                defenders++;
        }

        float delta = 0f;
        if (attackers > defenders)
            delta = captureRatePerSecond * (attackers - defenders);
        else if (defenders > attackers)
            delta = -decayPerSecond * (defenders - attackers);

        progress.Value = Mathf.Clamp01(progress.Value + delta * 0.25f);

        if (progress.Value >= 1f)
        {
            teamOwner.Value = attackingTeam;
            match?.ServerOnZoneCaptured(attackingTeam);
        }
    }

    public void ResetZone()
    {
        if (!IsServerInitialized) return;
        progress.Value = 0f;
        teamOwner.Value = Team.None;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
