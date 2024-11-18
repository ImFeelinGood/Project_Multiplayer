using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    [Header("References")]
    public GameObject cosmeticProjectilePrefab;
    public GameObject bulletHolePrefab; // Bullet hole prefab reference
    public Transform shootingPoint;
    public Camera playerCamera;
    public float shootingForce = 300f;
    public float gunCooldown = 1f;

    [Header("Shooting Settings")]
    public int damage = 30;
    public float movingAccuracy = 0.6f; // 60% accuracy
    public float standingAccuracy = 0.9f; // 90% accuracy

    private PlayerInfo playerInfo;
    private PlayerMovement playerMovement;
    private bool isShooting;

    private void Start()
    {
        playerInfo = GetComponent<PlayerInfo>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    // ServerRpc to handle ammo deduction and shooting logic
    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc()
    {
        if (playerInfo.currentAmmo.Value > 0)
        {
            playerInfo.currentAmmo.Value--;
        }
    }

    public void Shoot()
    {
        if (playerInfo.isReloading.Value)
        {
            FindObjectOfType<PlayerUI>()?.TriggerReloadWarning();
            return;
        }

        if (playerInfo.currentAmmo.Value > 0)
        {
            // Request the server to handle the shooting logic
            ShootServerRpc();

            isShooting = true;
            StartCoroutine(GunCooldown());

            // Determine accuracy based on movement state
            float accuracy = playerMovement.isMoving ? movingAccuracy : standingAccuracy;

            // Perform a raycast with accuracy spread
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 spread = GetRandomSpread(accuracy);
            Vector3 direction = ray.direction + spread;

            if (Physics.Raycast(ray.origin, direction, out RaycastHit hit))
            {
                // Apply damage if hit a player
                PlayerInfo targetPlayer = hit.collider.GetComponent<PlayerInfo>();
                if (targetPlayer != null && targetPlayer != playerInfo)
                {
                    HitTargetServerRpc(targetPlayer.NetworkObjectId, damage);
                    // Skip spawning bullet hole if the target is a player
                    return;
                }

                // Spawn bullet hole at the point of impact if the target is not a player
                SpawnBulletHole(hit);
            }

            // Instantiate cosmetic projectile
            SpawnCosmeticProjectile(direction);
        }
        else
        {
            Debug.Log("No ammo left. Reload!");
        }
    }

    // ServerRpc to apply damage
    [ServerRpc(RequireOwnership = false)]
    private void HitTargetServerRpc(ulong targetNetworkObjectId, int damage)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
        {
            PlayerInfo targetPlayer = targetObject.GetComponent<PlayerInfo>();
            targetPlayer?.TakeDamage(damage);
        }
    }

    // Method to spawn a bullet hole
    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab != null)
        {
            GameObject bulletHole = Instantiate(bulletHolePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            bulletHole.transform.SetParent(hit.collider.transform); // Parent to the hit object
            Destroy(bulletHole, 60f); // Destroy after 1 minute
        }
    }

    // Method to spawn cosmetic projectile
    private void SpawnCosmeticProjectile(Vector3 direction)
    {
        GameObject cosmeticProjectile = Instantiate(cosmeticProjectilePrefab, shootingPoint.position, shootingPoint.rotation);
        Rigidbody rb = cosmeticProjectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * shootingForce, ForceMode.Impulse);
        }
        Destroy(cosmeticProjectile, 2f);
    }

    // Method to calculate random spread based on accuracy
    private Vector3 GetRandomSpread(float accuracy)
    {
        float spreadFactor = 1f - accuracy;
        float spreadX = Random.Range(-spreadFactor, spreadFactor);
        float spreadY = Random.Range(-spreadFactor, spreadFactor);
        return new Vector3(spreadX, spreadY, 0f);
    }

    private IEnumerator GunCooldown()
    {
        yield return new WaitForSeconds(gunCooldown);
        isShooting = false;
    }
}
