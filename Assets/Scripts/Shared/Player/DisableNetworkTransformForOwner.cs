using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEngine;

public class DisableNetworkTransformForOwner : NetworkBehaviour
{
    public override void OnStartClient()
    {
        if (IsOwner)
        {
            var netTransform = GetComponent<NetworkTransform>();
            if (netTransform != null)
                netTransform.enabled = false;
        }
    }
}
