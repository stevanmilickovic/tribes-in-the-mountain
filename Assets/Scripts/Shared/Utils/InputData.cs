using FishNet.Object.Prediction;
using UnityEngine;

public struct InputData : IReplicateData
{
    public Vector2 Move;
    public bool JumpHeld;
    public bool FirePressedEdge;
    public bool CrouchPressedEdge;
    public bool PronePressedEdge;
    public bool AimHeld;
    public float Yaw;
    public float Pitch;
    private uint _tick;
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}
