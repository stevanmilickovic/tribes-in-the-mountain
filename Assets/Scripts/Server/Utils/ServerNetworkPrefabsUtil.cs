using UnityEngine;

public static class ServerNetworkPrefabsUtil
{
    public static Rigidbody setGameObjectRigidbody(GameObject gameObject)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.freezeRotation = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        return rb;
    }
}
