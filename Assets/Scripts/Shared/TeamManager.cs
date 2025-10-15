using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class TeamManager : NetworkBehaviour
{
    private readonly HashSet<PlayerTeam> _teamA = new HashSet<PlayerTeam>();
    private readonly HashSet<PlayerTeam> _teamB = new HashSet<PlayerTeam>();

    public void Join(PlayerTeam player, Team team)
    {
        if (!IsServerInitialized || player == null) return;
        _teamA.Remove(player);
        _teamB.Remove(player);
        if (team == Team.TeamA) _teamA.Add(player);
        else if (team == Team.TeamB) _teamB.Add(player);
    }

    public void Leave(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;
        _teamA.Remove(player);
        _teamB.Remove(player);
    }

    public (int, int) GetCounts()
    {
        return (_teamA.Count, _teamB.Count);
    }
}
