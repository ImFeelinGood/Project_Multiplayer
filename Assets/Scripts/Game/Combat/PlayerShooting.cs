using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Animator")]
    public Animator animator; // Reference to the player's Animator

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Sound Effects")]
    public AudioClip gunshotSFX;
    public AudioClip emptyClickSFX;

    [Header("References")]
    public GameObject cosmeticProjectilePrefab;
    public GameObject bulletHolePrefab; // Bullet hole prefab reference
    public Transform shootingPoint;
    public Camera playerCamera;
    public float shootingForce = 300f;
    public float gunCooldown = 1f;

    [Header("Shooting Settings")]
    public int damage = 30;
    public float movingAccuracy = 0.95f; // 95% accuracy
    public float standingAccuracy = 0.995f; // 99.5% accuracy
    public float jumpingAccuracy = 0.8f; // 80% accuracy when jumping

    private PlayerInfo playerInfo;
    private PlayerMovement playerMovement;
    private bool isShooting;

    private void Start()
    {
        playerInfo = GetComponent<PlayerInfo>();
        playerMovement = GetComponent<PlayerMovement>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>(); // Assign Animator if not manually set
        }

    }

    // ServerRpc to handle ammo deduction and shooting logic
    [ServerRpc(RequireOwnership = false)]
    private void ShootServerRpc()
    {
        if (playerInfo.currentAmmo.Value > 0)
        {
            playerInfo.currentAmmo.Value--;
            PlayGunshotSoundClientRpc(); // Broadcast gunshot sound
        }
    }

    public void Shoot()
    {
        if (isShooting)
        {
            Debug.Log("Gun is cooling down. Please wait.");
            return;
        }

        if (playerInfo.isReloading.Value)
        {
            FindObjectOfType<PlayerUI>()?.TriggerReloadWarning();
            TriggerEmptyAnimation(); // Trigger the empty animation
            return;
        }

        if (playerInfo.currentAmmo.Value > 0)
        {
            isShooting = true;

            // Request the server to handle the shooting logic
            ShootServerRpc();

            // Trigger the shooting animation
            TriggerShootingAnimation();

            // Determine accuracy based on movement state
            float accuracy;
            if (playerMovement.isJumping || playerMovement.isFalling)
            {
                accuracy = jumpingAccuracy; // Use jumping accuracy if the player is in the air
            }
            else if (playerMovement.isMoving)
            {
                accuracy = movingAccuracy; // Use moving accuracy if the player is walking/running
            }
            else
            {
                accuracy = standingAccuracy; // Use standing accuracy otherwise
            }

            // Perform a raycast with accuracy spread
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 spread = GetRandomSpread(accuracy);
            Vector3 direction = ray.direction + spread;

            if (Physics.Raycast(ray.origin, direction, out RaycastHit hit))
            {
                PlayerInfo targetPlayer = hit.collider.GetComponent<PlayerInfo>();
                if (targetPlayer != null && targetPlayer != playerInfo)
                {
                    HitTargetServerRpc(targetPlayer.NetworkObjectId, damage);
                    StartCoroutine(GunCooldown());
                    return;
                }

                SpawnBulletHole(hit);
            }

            SpawnCosmeticProjectile(direction);

            StartCoroutine(GunCooldown());
        }
        else
        {
            Debug.Log("No ammo left. Reload!");
            PlaySound(emptyClickSFX); // Play empty sound
            TriggerEmptyAnimation(); // Trigger the empty animation
        }
    }

    // Method to trigger the shooting animation
    private void TriggerShootingAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("ShootTrigger"); // Set the trigger for the animation
        }
    }

    // Method to trigger the empty animation
    private void TriggerEmptyAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("EmptyTrigger"); // Set the trigger for the empty animation
        }
    }

    // Play shooting sound when shoot
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }


    // ClientRpc to play the gunshot sound
    [ClientRpc]
    private void PlayGunshotSoundClientRpc()
    {
        PlaySound(gunshotSFX);
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

    // Method to request spawning a bullet hole
    private void SpawnBulletHole(RaycastHit hit)
    {
        // Check if the hit object has a NetworkObject
        NetworkObject hitNetworkObject = hit.collider.gameObject.GetComponent<NetworkObject>();

        // Request the server to spawn the bullet hole
        if (hitNetworkObject != null)
        {
            SpawnBulletHoleServerRpc(hit.point, Quaternion.LookRotation(hit.normal), hitNetworkObject.NetworkObjectId);
        }
        else
        {
            // Fallback: Spawn the bullet hole without parenting if no NetworkObject
            SpawnBulletHoleServerRpc(hit.point, Quaternion.LookRotation(hit.normal), 0);
        }
    }

    // ServerRpc to spawn the bullet hole on the server
    [ServerRpc]
    private void SpawnBulletHoleServerRpc(Vector3 position, Quaternion rotation, ulong parentNetworkObjectId)
    {
        // Instantiate the bullet hole on the server
        GameObject bulletHole = Instantiate(bulletHolePrefab, position, rotation);

        // Parent the bullet hole to the hit object (if it has a valid NetworkObjectId)
        if (parentNetworkObjectId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentNetworkObjectId, out NetworkObject parentObject))
        {
            bulletHole.transform.SetParent(parentObject.transform);
        }

        // Spawn the bullet hole as a NetworkObject
        NetworkObject networkObject = bulletHole.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(); // Spawn it on the network
        }

        // Destroy the bullet hole after a specific time
        Destroy(bulletHole, 60f);
    }

    // Method to request spawning a cosmetic projectile
    private void SpawnCosmeticProjectile(Vector3 direction)
    {
        // Send a ServerRpc to spawn the projectile
        SpawnProjectileServerRpc(direction, shootingPoint.position, shootingPoint.rotation);
    }

    // ServerRpc to spawn the projectile on the server
    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, Quaternion rotation)
    {
        Vector3 offsetPosition = position + direction.normalized * 0.5f;

        // Instantiate the projectile on the server
        GameObject projectile = Instantiate(cosmeticProjectilePrefab, offsetPosition, rotation);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }

        // Apply force only on the server
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * shootingForce, ForceMode.Impulse);
        }

        // Notify clients to apply force
        ApplyForceClientRpc(networkObject.NetworkObjectId, direction);

        Destroy(projectile, 2f);
    }

    // ClientRpc to apply force locally
    [ClientRpc]
    private void ApplyForceClientRpc(ulong networkObjectId, Vector3 direction)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject projectileObject))
        {
            Rigidbody rb = projectileObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction * shootingForce, ForceMode.Impulse);
            }
        }
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
