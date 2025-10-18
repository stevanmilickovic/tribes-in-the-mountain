using UnityEngine;

public static class ClientNetworkPrefabsUtil
{
    public static Rigidbody setGameObjectRigidbody(GameObject gameObject)
    {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;

        return rb;
    }
}
