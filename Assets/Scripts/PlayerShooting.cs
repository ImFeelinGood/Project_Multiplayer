using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerShooting : NetworkBehaviour
{
    public GameObject projectilePrefab; // Prefab for the projectile
    public Transform shootingPoint; // The point from where the projectile will be instantiated
    public float shootingForce = 20f; // Force applied to the projectile
    public float gunCooldown = 1f; // Cooldown for shooting
    private PlayerInfo playerHealth; // Reference to PlayerInfo

    private bool isShooting; // Flag to check if currently shooting

    private void Start()
    {
        playerHealth = GetComponent<PlayerInfo>(); // Get the PlayerInfo component
    }

    void Update()
    {
        if (!IsOwner) return;

        // Shooting Input
        if (Input.GetButtonDown("Fire1")) // Assume left mouse button for shooting
        {
            if (!isShooting) // Check if not currently shooting
            {
                Shoot();
            }
        }
    }

    public void Shoot()
    {
        if (playerHealth.currentAmmo > 0) // Check ammo before shooting
        {
            isShooting = true;

            // Instantiate the projectile
            GameObject projectile = Instantiate(projectilePrefab, shootingPoint.position, shootingPoint.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Add force to the projectile
                rb.AddForce(shootingPoint.forward * shootingForce, ForceMode.Impulse);

                // Only subtract ammo after the projectile has been instantiated
                playerHealth.currentAmmo--; // Subtract ammo here after confirming firing
            }

            // Start the cooldown
            StartCoroutine(GunCooldown());
        }
    }

    private IEnumerator GunCooldown()
    {
        yield return new WaitForSeconds(gunCooldown);
        isShooting = false; // Reset shooting flag
    }
}
