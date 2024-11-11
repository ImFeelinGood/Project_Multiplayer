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

    private PlayerInfo playerInfo;
    private bool isShooting;

    private void Start()
    {
        playerInfo = GetComponent<PlayerInfo>();
    }

    // Shoot method using raycast
    public void Shoot()
    {
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
                    HitTargetServerRpc(targetPlayer.NetworkObjectId);
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
    [ServerRpc]
    private void HitTargetServerRpc(ulong targetNetworkObjectId)
    {
        NetworkObject targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetworkObjectId];
        if (targetObject != null)
        {
            PlayerInfo targetPlayer = targetObject.GetComponent<PlayerInfo>();
            if (targetPlayer != null)
            {
                targetPlayer.TakeDamage(30); // Example damage value
            }
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
