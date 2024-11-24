using UnityEngine;
using Unity.Netcode;

public class BasePickup : NetworkBehaviour
{
    // Event to notify when the pickup has been taken
    public event System.Action OnPickupTaken;

    // Call this method when the pickup is taken
    protected void NotifyPickupTaken()
    {
        OnPickupTaken?.Invoke();
    }
}
