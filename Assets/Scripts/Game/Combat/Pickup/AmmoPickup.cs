using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class AmmoPickup : NetworkBehaviour
{
    public int ammoRestoreAmount = 15; // Amount of ammo to restore
    public float respawnTime = 20f; // Time before respawning

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a player
        if (other.CompareTag("Player"))
        {
            PlayerInfo player = other.GetComponent<PlayerInfo>();
            if (player != null && player.inventoryAmmo < 30)
            {
                // Restore 50% of the player's max ammo
                int restoreAmount = Mathf.Min(ammoRestoreAmount, 30 - player.inventoryAmmo);
                player.RestoreAmmo(restoreAmount);

                // Despawn the pickup on the server
                DespawnPickupServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnPickupServerRpc()
    {
        // Despawn the pickup and start the respawn coroutine
        NetworkObject.Despawn();
        StartCoroutine(RespawnPickup());
    }

    // Coroutine to respawn the pickup after a delay
    private IEnumerator RespawnPickup()
    {
        yield return new WaitForSeconds(respawnTime);

        // Respawn the pickup on the server
        NetworkObject.Spawn();
    }
}
