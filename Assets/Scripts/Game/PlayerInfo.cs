using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerInfo : NetworkBehaviour
{
    [Header("Player Stats")]
    public int maxAmmo = 6; // Max ammo in magazine
    public int inventoryAmmo = 30; // Total ammo available
    public int currentAmmo; // Current ammo in magazine
    public int health = 100; // Player health

    [Header("Ragdoll & Respawn")]
    public Transform spawnPoint; // The point where the player will respawn
    public float respawnDelay = 3f; // Delay before respawn
    private Rigidbody[] ragdollBodies; // Array to store ragdoll parts
    private Animator animator;

    private void Start()
    {
        currentAmmo = maxAmmo; // Start with a full magazine
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();

        // Disable ragdoll at the start
        ToggleRagdoll(false);
    }

    // Method to decrease health
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Handle player death
            Debug.Log("Player died.");
            HandleDeathServerRpc();
        }
    }

    // ServerRpc to handle player death and ragdoll
    [ServerRpc(RequireOwnership = false)]
    private void HandleDeathServerRpc()
    {
        ToggleRagdoll(true); // Enable ragdoll
        StartCoroutine(RespawnPlayer()); // Start respawn coroutine
    }

    // Method to toggle ragdoll effect
    private void ToggleRagdoll(bool enable)
    {
        if (animator != null)
        {
            animator.enabled = !enable;
        }

        foreach (Rigidbody rb in ragdollBodies)
        {
            rb.isKinematic = !enable;
            rb.useGravity = enable;
        }
    }

    // Coroutine to respawn the player
    private IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Reset health and ammo
        health = 100;
        currentAmmo = maxAmmo;

        // Move the player to the spawn point
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // Disable ragdoll and re-enable player controls
        ToggleRagdoll(false);
    }

    // Reload method to refill ammo
    public void Reload()
    {
        int ammoNeeded = maxAmmo - currentAmmo;
        if (inventoryAmmo >= ammoNeeded)
        {
            currentAmmo += ammoNeeded;
            inventoryAmmo -= ammoNeeded;
        }
        else
        {
            currentAmmo += inventoryAmmo;
            inventoryAmmo = 0;
        }
        Debug.Log("Reloaded ammo.");
    }

    // Method to handle damage from specific projectile
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile != null)
            {
                TakeDamage(projectile.damage);
                Destroy(other.gameObject);
            }
        }
    }
}
