using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class GameMode : NetworkBehaviour
{
    private readonly SyncVar<int> remainingSeconds = new();
    private readonly SyncVar<int> teamACount = new();
    private readonly SyncVar<int> teamBCount = new();

    private float accumulated;
    private TeamManager teams;

    public int RemainingSeconds => remainingSeconds.Value;
    public int TeamACount => teamACount.Value;
    public int TeamBCount => teamBCount.Value;

    public override void OnStartServer()
    {
        base.OnStartServer();
        teams = GetComponent<TeamManager>();
        if (teams == null) teams = gameObject.AddComponent<TeamManager>();
        remainingSeconds.Value = 600;
        teamACount.Value = 0;
        teamBCount.Value = 0;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        accumulated += Time.deltaTime;
        if (accumulated >= 1f)
        {
            accumulated -= 1f;
            remainingSeconds.Value -= 1;
            if (remainingSeconds.Value <= 0) remainingSeconds.Value = 600;
        }
        var counts = teams != null ? teams.GetCounts() : (0, 0);
        if (teamACount.Value != counts.Item1) teamACount.Value = counts.Item1;
        if (teamBCount.Value != counts.Item2) teamBCount.Value = counts.Item2;
    }
}
