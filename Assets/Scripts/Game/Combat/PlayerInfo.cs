using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerInfo : NetworkBehaviour
{
    [Header("Player Stats")]
    public int maxAmmo = 6; // Max ammo in magazine
    public int inventoryAmmo = 30; // Total ammo available
    public int currentAmmo; // Current ammo in magazine
    public NetworkVariable<int> health = new NetworkVariable<int>(100);
    public bool isReloading = false; // Track if the player is currently reloading
    public float reloadTime = 3f; // Time required to reload

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
        if (IsServer)
        {
            health.Value -= damage;
            if (health.Value <= 0)
            {
                Debug.Log("Player died.");
                HandleDeathServerRpc();
            }
        }
    }

    // Method to restore health
    public void RestoreHealth(int amount)
    {
        health.Value += amount;
        if (health.Value > 100) health.Value = 100; // Cap health at 100
        Debug.Log("Health restored by " + amount);
    }

    // Method to restore ammo
    public void RestoreAmmo(int amount)
    {
        inventoryAmmo += amount;
        if (inventoryAmmo > 30) inventoryAmmo = 30; // Cap ammo at max capacity
        Debug.Log("Ammo restored by " + amount);
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

    [ServerRpc(RequireOwnership = false)]
    private void SetRespawnPositionServerRpc(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    // Coroutine to respawn the player
    private IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is not assigned!");
            yield break;
        }

        // Reset health and ammo
        health.Value = 100;
        currentAmmo = maxAmmo;

        // Move the player to the spawn point
        if (IsServer)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }
        else
        {
            SetRespawnPositionServerRpc(spawnPoint.position, spawnPoint.rotation);
        }

        // Disable ragdoll and re-enable player controls
        ToggleRagdoll(false);
    }

    // Reload method to refill ammo
    public void Reload()
    {
        // Check if the player is already reloading
        if (isReloading || currentAmmo == maxAmmo || inventoryAmmo == 0)
        {
            return;
        }

        // Start the reload coroutine
        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        // Wait for the reload time (3 seconds)
        yield return new WaitForSeconds(reloadTime);

        // Calculate the ammo needed and update ammo counts
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
        isReloading = false; // Reset the reloading flag
    }
}