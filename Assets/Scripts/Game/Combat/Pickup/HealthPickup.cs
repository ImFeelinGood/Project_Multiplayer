using UnityEngine;
using Unity.Netcode;

public class HealthPickup : BasePickup
{
    public int healthRestoreAmount = 50;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInfo player = other.GetComponent<PlayerInfo>();
            if (player != null && player.health.Value < 100)
            {
                int restoreAmount = Mathf.Min(healthRestoreAmount, 100 - player.health.Value);
                player.RestoreHealthServerRpc(restoreAmount);

                // Notify the server to despawn this pickup
                TakePickupServerRpc();
            }
        }
    }
}
