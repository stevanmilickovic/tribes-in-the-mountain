using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class CaptureZone : NetworkBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float captureRatePerSecond = 0.3f;
    [SerializeField] private float decayPerSecond = 0.10f;

    [Tooltip("Initial owner teamId. Use 'none' for neutral/uncaptured zones.")]
    [SerializeField] private string initialOwnerId = TeamDatabase.NeutralId;

    [SerializeField] private bool uncapturable = false;

    [Header("Spawn Points (optional)")]
    [SerializeField] private Transform[] spawnPoints;
    public Transform[] Spawns => spawnPoints;

    public string InitialOwnerId => initialOwnerId;
    public bool Uncapturable => uncapturable;

    private readonly SyncVar<string> ownerId = new(TeamDatabase.NeutralId);
    private readonly SyncVar<float> progress = new();
    private readonly SyncVar<bool> isContested = new();
    private readonly SyncVar<string> capturingTeamId = new(TeamDatabase.NeutralId);

    public float Progress => progress.Value;
    public string TeamOwnerId => ownerId.Value;
    public bool IsContested => isContested.Value;
    public string CapturingTeamId => capturingTeamId.Value;

    private readonly Collider[] _buffer = new Collider[16];
    private float _accum;

    private MatchController match => MatchController.Instance;

    public override void OnStartServer()
    {
        base.OnStartServer();

        progress.Value = 0f;
        ownerId.Value = NormalizeTeamId(initialOwnerId);
        isContested.Value = false;
        capturingTeamId.Value = TeamDatabase.NeutralId;
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
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            _buffer,
            LayerMask.GetMask("PlayerHitbox")
        );

        var teamCounts = new Dictionary<string, int>(4);

        for (int i = 0; i < count; i++)
        {
            var col = _buffer[i];
            if (col == null) continue;

            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || !health.IsAlive) continue;

            var teamComp = col.GetComponentInParent<PlayerTeam>();
            if (teamComp == null) continue;

            string tid = NormalizeTeamId(teamComp.TeamId);
            if (tid == TeamDatabase.NeutralId) continue;

            teamCounts.TryGetValue(tid, out int c);
            teamCounts[tid] = c + 1;
        }

        string owner = NormalizeTeamId(ownerId.Value);

        if (uncapturable)
        {
            bool enemyPresent = false;

            if (owner == TeamDatabase.NeutralId)
            {
                enemyPresent = teamCounts.Count > 0;
            }
            else
            {
                foreach (var kv in teamCounts)
                {
                    if (kv.Key != owner && kv.Value > 0)
                    {
                        enemyPresent = true;
                        break;
                    }
                }
            }

            isContested.Value = enemyPresent;
            capturingTeamId.Value = TeamDatabase.NeutralId;
            progress.Value = 0f;
            return;
        }

        string dominant = TeamDatabase.NeutralId;
        int top = 0;
        int second = 0;

        foreach (var kv in teamCounts)
        {
            int v = kv.Value;
            if (v > top)
            {
                second = top;
                top = v;
                dominant = kv.Key;
            }
            else if (v > second)
            {
                second = v;
            }
        }

        int advantage = Mathf.Max(0, top - second);

        bool activeCapture =
            dominant != TeamDatabase.NeutralId &&
            advantage > 0 &&
            (owner == TeamDatabase.NeutralId || dominant != owner);

        isContested.Value = activeCapture;
        capturingTeamId.Value = activeCapture ? dominant : TeamDatabase.NeutralId;

        float delta;

        if (activeCapture)
        {
            delta = captureRatePerSecond * advantage;
        }
        else
        {
            float decayScale = advantage > 0 ? advantage : 1f;
            delta = -decayPerSecond * decayScale;
        }

        progress.Value = Mathf.Clamp01(progress.Value + delta * dt);

        if (progress.Value >= 1f && activeCapture)
        {
            ownerId.Value = dominant;
            progress.Value = 0f;
            capturingTeamId.Value = TeamDatabase.NeutralId;
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
        ownerId.Value = NormalizeTeamId(initialOwnerId);
        isContested.Value = false;
        capturingTeamId.Value = TeamDatabase.NeutralId;
    }

    private static string NormalizeTeamId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TeamDatabase.NeutralId;

        var db = TeamDatabase.Instance;
        if (db == null) return id;

        return db.IsValidTeamId(id) ? id : TeamDatabase.NeutralId;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
