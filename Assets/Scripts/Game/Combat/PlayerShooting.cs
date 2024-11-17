using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    [Header("References")]
    public GameObject cosmeticProjectilePrefab; // Cosmetic projectile prefab
    public Transform shootingPoint; // Where the visual projectile will appear
    public Camera playerCamera; // The player's camera
    public float shootingForce = 300f; // Force for the cosmetic projectile
    public float gunCooldown = 1f; // Cooldown for shooting

    [Header("Shooting Settings")]
    public int damage = 30; // Damage value, editable from Inspector

    private PlayerInfo playerInfo;
    private bool isShooting;

    private void Start()
    {
        playerInfo = GetComponent<PlayerInfo>();
    }

    public void Shoot()
    {
        // Prevent shooting if the player is reloading or already shooting (cooldown)
        if (playerInfo.isReloading)
        {
            Debug.Log("Cannot shoot while reloading.");
            return;
        }

        // Check if there is ammo before shooting
        if (playerInfo.currentAmmo > 0)
        {
            // Decrease the ammo count
            playerInfo.currentAmmo--;

            // Start cooldown
            isShooting = true;
            StartCoroutine(GunCooldown());

            // Perform a raycast from the player's camera
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the raycast hit another player
                PlayerInfo targetPlayer = hit.collider.GetComponent<PlayerInfo>();
                if (targetPlayer != null && targetPlayer != playerInfo)
                {
                    // Call ServerRpc to apply damage
                    HitTargetServerRpc(targetPlayer.NetworkObjectId, damage);
                }
            }

            // Instantiate the cosmetic projectile locally
            SpawnCosmeticProjectile();
            Debug.Log("Player Shooting!");
        }
        else
        {
            Debug.Log("No ammo left. Reload!");
        }
    }

    // ServerRpc to apply damage to the target player
    [ServerRpc(RequireOwnership = false)]
    private void HitTargetServerRpc(ulong targetNetworkObjectId, int damage)
    {
        Debug.Log($"[ServerRpc] Attempting to damage object with NetworkObjectId: {targetNetworkObjectId}");

        // Try to get the NetworkObject using the NetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
        {
            PlayerInfo targetPlayer = targetObject.GetComponent<PlayerInfo>();

            if (targetPlayer != null)
            {
                // Check if the server is correctly identifying the client target
                Debug.Log($"[ServerRpc] Target identified: {targetPlayer.gameObject.name}, applying {damage} damage.");

                // Apply damage on the server
                targetPlayer.TakeDamage(damage);

                // Debug to confirm damage application
                Debug.Log($"[ServerRpc] Applied {damage} damage to {targetPlayer.gameObject.name}. Current Health: {targetPlayer.health}");
            }
            else
            {
                Debug.LogWarning("[ServerRpc] Target PlayerInfo component not found.");
            }
        }
        else
        {
            Debug.LogWarning("[ServerRpc] NetworkObject not found in SpawnedObjects.");
        }
    }

    // Method to spawn a cosmetic projectile locally
    private void SpawnCosmeticProjectile()
    {
        GameObject cosmeticProjectile = Instantiate(cosmeticProjectilePrefab, shootingPoint.position, shootingPoint.rotation);
        Rigidbody rb = cosmeticProjectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootingPoint.forward * shootingForce, ForceMode.Impulse);
        }

        // Destroy the cosmetic projectile after 2 seconds
        Destroy(cosmeticProjectile, 2f);
    }

    private IEnumerator GunCooldown()
    {
        yield return new WaitForSeconds(gunCooldown);
        isShooting = false;
    }
}
