using UnityEngine;
using Unity.Netcode;

public class BasePickup : NetworkBehaviour
{
    public event System.Action OnPickupTaken;

    // Call this method when the pickup is taken
    protected void NotifyPickupTaken()
    {
        OnPickupTaken?.Invoke();
    }

    // Server-side despawn logic
    [ServerRpc(RequireOwnership = false)]
    public void TakePickupServerRpc()
    {
        Debug.Log($"{name} is being taken and will be despawned by the server.");
        if (!IsServer)
            return;

        NotifyPickupTaken();
        NetworkObject.Despawn();
        Destroy(gameObject);
    }
}
