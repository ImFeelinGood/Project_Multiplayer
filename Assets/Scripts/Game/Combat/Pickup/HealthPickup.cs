using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HealthPickup : NetworkBehaviour
{
    public int healthRestoreAmount = 50; // Amount of health to restore
    public float respawnTime = 20f; // Time before respawning

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to a player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player take Health");
            PlayerInfo player = other.GetComponent<PlayerInfo>();
            if (player != null && player.health < 100)
            {
                // Restore 50% of the player's max health
                int restoreAmount = Mathf.Min(healthRestoreAmount, 100 - player.health);
                player.RestoreHealth(restoreAmount);

                // Despawn the pickup on the server
                DespawnPickupServerRpc();
            }
        }
        else
            Debug.Log("Player didn't take Health");
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
