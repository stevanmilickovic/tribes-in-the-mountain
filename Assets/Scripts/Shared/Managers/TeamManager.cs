using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class TeamManager : NetworkBehaviour
{
    private readonly Dictionary<string, HashSet<PlayerTeam>> _teams = new();
    private readonly Dictionary<PlayerTeam, string> _playerToTeam = new();

    public void Join(PlayerTeam player, string teamId)
    {
        if (!IsServerInitialized || player == null) return;
        if (string.IsNullOrWhiteSpace(teamId) || teamId == TeamDatabase.NeutralId) return;

        Leave(player);

        if (!_teams.TryGetValue(teamId, out var set) || set == null)
        {
            set = new HashSet<PlayerTeam>();
            _teams[teamId] = set;
        }

        set.Add(player);
        _playerToTeam[player] = teamId;
    }

    public void Leave(PlayerTeam player)
    {
        if (!IsServerInitialized || player == null) return;

        if (_playerToTeam.TryGetValue(player, out var oldId))
        {
            if (!string.IsNullOrWhiteSpace(oldId) && _teams.TryGetValue(oldId, out var set) && set != null)
                set.Remove(player);

            _playerToTeam.Remove(player);
        }
        else
        {
            foreach (var kv in _teams)
                kv.Value?.Remove(player);
        }
    }

    public int GetCount(string teamId)
    {
        if (!IsServerInitialized) return 0;
        if (string.IsNullOrWhiteSpace(teamId) || teamId == TeamDatabase.NeutralId) return 0;

        return _teams.TryGetValue(teamId, out var set) && set != null ? set.Count : 0;
    }

    public (int, int) GetCountsAB()
    {
        return (GetCount(MatchController.TeamAId), GetCount(MatchController.TeamBId));
    }
}
