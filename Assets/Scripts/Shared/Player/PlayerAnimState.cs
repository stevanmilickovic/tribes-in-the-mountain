using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerAnimState : NetworkBehaviour
{
    public readonly SyncVar<bool> IsCrouching = new();
    public readonly SyncVar<bool> IsProne = new();
    public readonly SyncVar<bool> IsAiming = new();
}
