using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class TeamManager : NetworkBehaviour
{
    private readonly HashSet<PlayerTeam> teamA = new HashSet<PlayerTeam>();
    private readonly HashSet<PlayerTeam> teamB = new HashSet<PlayerTeam>();

    public void Join(PlayerTeam player, Team team)
    {
        if (!IsServerInitialized || player == null) return;
        teamA.Remove(player);
        teamB.Remove(player);
        if (team == Team.TeamA) teamA.Add(player);
        else if (team == Team.TeamB) teamB.Add(player);
    }

    public void Leave(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;
        teamA.Remove(player);
        teamB.Remove(player);
    }

    public (int, int) GetCounts()
    {
        return (teamA.Count, teamB.Count);
    }
}
