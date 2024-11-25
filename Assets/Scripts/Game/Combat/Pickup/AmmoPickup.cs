using UnityEngine;
using Unity.Netcode;

public class AmmoPickup : BasePickup
{
    public int ammoRestoreAmount = 15;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInfo player = other.GetComponent<PlayerInfo>();
            if (player != null && player.inventoryAmmo.Value < 30)
            {
                int restoreAmount = Mathf.Min(ammoRestoreAmount, 30 - player.inventoryAmmo.Value);
                player.RestoreAmmoServerRpc(restoreAmount);

                // Notify the server to despawn this pickup
                TakePickupServerRpc();
            }
        }
    }
}
