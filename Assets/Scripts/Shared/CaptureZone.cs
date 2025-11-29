using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;

public class CaptureZone : NetworkBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float captureRatePerSecond = 0.3f;
    [SerializeField] private float decayPerSecond = 0.10f;
    [SerializeField] private Team initialOwner = Team.TeamA;
    [SerializeField] private Transform[] spawnPoints;
    public Transform[] Spawns => spawnPoints;

    public Team InitialOwner => initialOwner;


    [Header("Sync Vars (read-only on clients)")]
    private readonly SyncVar<Team> teamOwner = new();
    private readonly SyncVar<float> progress = new();
    private readonly SyncVar<bool> isContested = new();

    public float Progress => progress.Value;
    public Team Owner => teamOwner.Value;
    public bool IsContested => isContested.Value;

    private readonly Collider[] _buffer = new Collider[16];
    private float _accum;

    private MatchController match => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        progress.Value = 0f;
        teamOwner.Value = initialOwner;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        if (match == null || match.State != MatchState.Live)
            return;

        _accum += Time.deltaTime;
        if (_accum < 0.25f) return;
        _accum = 0f;

        HandleZoneCaptureProgress();
    }

    private void HandleZoneCaptureProgress()
    {
        int teamA = 0;
        int teamB = 0;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _buffer, LayerMask.GetMask("PlayerHitbox"));
        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || !health.IsAlive) continue;

            var teamComp = col.GetComponentInParent<PlayerTeam>();
            if (teamComp == null) continue;

            if (teamComp.team.Value == Team.TeamA) teamA++;
            else if (teamComp.team.Value == Team.TeamB)
            {
                teamB++;
            }
        }

        bool attackersPresent = false;
        if (teamOwner.Value == Team.TeamA) attackersPresent = teamB > 0;
        else if (teamOwner.Value == Team.TeamB) attackersPresent = teamA > 0;

        isContested.Value = attackersPresent;

        float delta = 0f;

        if (teamOwner.Value == Team.TeamA)
        {
            if (teamB > teamA) delta = captureRatePerSecond * (teamB - teamA);
            else if (teamA > teamB) delta = -decayPerSecond * (teamA - teamB);
        }
        else if (teamOwner.Value == Team.TeamB)
        {
            if (teamA > teamB) delta = captureRatePerSecond * (teamA - teamB);
            else if (teamB > teamA) delta = -decayPerSecond * (teamB - teamA);
        }

        progress.Value = Mathf.Clamp01(progress.Value + delta * 0.25f);

        if (progress.Value >= 1f)
        {
            teamOwner.Value = (teamOwner.Value == Team.TeamA) ? Team.TeamB : Team.TeamA;
            progress.Value = 0f;
            match?.ServerOnZoneCaptured(this);
        }
        else if (progress.Value <= 0f)
        {
            progress.Value = 0f;
        }
    }


    public void ResetZone()
    {
        progress.Value = 0f;
        teamOwner.Value = initialOwner;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
