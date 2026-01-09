using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class CaptureZone : NetworkBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float radius = 5f;

    [Tooltip("How fast the zone capture progress increases per second (scaled by player advantage).")]
    [SerializeField] private float captureRatePerSecond = 0.3f;

    [Tooltip("How fast capture progress decays back toward 0 when defenders outnumber attackers (or when no clear advantage).")]
    [SerializeField] private float decayPerSecond = 0.10f;

    [Tooltip("Initial owner. Use None for neutral/uncaptured zones.")]
    [SerializeField] private Team initialOwner = Team.None;

    [Tooltip("If true, this zone cannot be captured/changed (e.g., each team's home zone).")]
    [SerializeField] private bool uncapturable = false;

    [Header("Spawn Points (optional)")]
    [SerializeField] private Transform[] spawnPoints;
    public Transform[] Spawns => spawnPoints;

    public Team InitialOwner => initialOwner;
    public bool Uncapturable => uncapturable;

    [Header("Sync Vars (read-only on clients)")]
    private readonly SyncVar<Team> teamOwner = new();
    private readonly SyncVar<float> progress = new();
    private readonly SyncVar<bool> isContested = new();

    private readonly SyncVar<Team> capturingTeam = new();

    public float Progress => progress.Value;
    public Team Owner => teamOwner.Value;
    public bool IsContested => isContested.Value;
    public Team CapturingTeam => capturingTeam.Value;

    private readonly Collider[] _buffer = new Collider[16];
    private float _accum;

    private MatchController match => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        progress.Value = 0f;
        teamOwner.Value = initialOwner;
        isContested.Value = false;
        capturingTeam.Value = Team.None;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (match == null || match.State != MatchState.Live) return;

        _accum += Time.deltaTime;
        if (_accum < 0.25f) return;
        float dt = _accum;
        _accum = 0f;

        HandleZoneCaptureProgress(dt);
    }

    private void HandleZoneCaptureProgress(float dt)
    {
        int teamA = 0;
        int teamB = 0;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            _buffer,
            LayerMask.GetMask("PlayerHitbox")
        );

        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || !health.IsAlive) continue;

            var teamComp = col.GetComponentInParent<PlayerTeam>();
            if (teamComp == null) continue;

            if (teamComp.team.Value == Team.TeamA) teamA++;
            else if (teamComp.team.Value == Team.TeamB) teamB++;
        }

        if (uncapturable)
        {
            bool enemyPresent =
                (teamOwner.Value == Team.TeamA && teamB > 0) ||
                (teamOwner.Value == Team.TeamB && teamA > 0) ||
                (teamOwner.Value == Team.None && (teamA > 0 || teamB > 0));

            isContested.Value = enemyPresent;
            capturingTeam.Value = Team.None;
            progress.Value = 0f;
            return;
        }

        Team dominant = Team.None;
        int advantage = 0;

        if (teamA > teamB)
        {
            dominant = Team.TeamA;
            advantage = teamA - teamB;
        }
        else if (teamB > teamA)
        {
            dominant = Team.TeamB;
            advantage = teamB - teamA;
        }

        Team owner = teamOwner.Value;

        bool activeCapture =
            (dominant != Team.None) &&
            (owner == Team.None || dominant != owner);

        isContested.Value = activeCapture;
        capturingTeam.Value = activeCapture ? dominant : Team.None;

        float delta = 0f;

        if (activeCapture)
        {
            delta = captureRatePerSecond * advantage;
        }
        else
        {
            float decayScale = (advantage > 0) ? advantage : 1f;
            delta = -decayPerSecond * decayScale;
        }

        progress.Value = Mathf.Clamp01(progress.Value + delta * dt);

        if (progress.Value >= 1f && activeCapture)
        {
            teamOwner.Value = dominant;
            progress.Value = 0f;
            capturingTeam.Value = Team.None;
            isContested.Value = false;

            match?.ServerOnZoneCaptured(this);
        }
        else if (progress.Value <= 0f)
        {
            progress.Value = 0f;
        }
    }

    [Server]
    public void ResetZone()
    {
        progress.Value = 0f;
        teamOwner.Value = initialOwner;
        isContested.Value = false;
        capturingTeam.Value = Team.None;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
