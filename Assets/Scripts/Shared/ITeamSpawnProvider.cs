
public interface ITeamSpawnProvider
{
    UnityEngine.Transform GetSpawn(Team team, FishNet.Object.NetworkObject player);
}
