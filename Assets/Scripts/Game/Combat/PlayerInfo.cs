using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerInfo : NetworkBehaviour
{
    [Header("Player Stats")]
    public int maxAmmo = 6; // Max ammo in magazine
    public NetworkVariable<int> inventoryAmmo = new NetworkVariable<int>(30); // Total ammo available
    public NetworkVariable<int> currentAmmo = new NetworkVariable<int>(); // Current ammo in magazine
    public NetworkVariable<int> health = new NetworkVariable<int>(100);
    public NetworkVariable<bool> isReloading = new NetworkVariable<bool>(false); // Track if the player is reloading
    public float reloadTime = 3f; // Time required to reload

    [Header("Ragdoll & Respawn")]
    public Transform spawnPoint; // The point where the player will respawn
    public float respawnDelay = 3f; // Delay before respawn
    private Rigidbody[] ragdollBodies; // Array to store ragdoll parts
    private Animator animator;

    private void Start()
    {
        if (IsServer)
        {
            currentAmmo.Value = maxAmmo; // Initialize currentAmmo only on the server
        }
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
    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(int amount)
    {
        // Only modify health on the server
        health.Value += amount;
        if (health.Value > 100) health.Value = 100; // Cap health at 100
        Debug.Log("Health restored by " + amount);
    }

    // Method to restore ammo (ServerRpc)
    [ServerRpc(RequireOwnership = false)]
    public void RestoreAmmoServerRpc(int amount)
    {
        inventoryAmmo.Value += amount;
        if (inventoryAmmo.Value > 30) inventoryAmmo.Value = 30; // Cap ammo at max capacity
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
        currentAmmo.Value = maxAmmo;

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

    // ServerRpc to handle reload logic on the server
    [ServerRpc(RequireOwnership = false)]
    private void ReloadServerRpc()
    {
        if (isReloading.Value) return; // Prevent duplicate reloads
        isReloading.Value = true; // Set reloading state
        StartCoroutine(ReloadCoroutine());
    }

    // Reload method to initiate reload process
    public void Reload()
    {
        // Check if already reloading or if conditions for reload are invalid
        if (isReloading.Value || currentAmmo.Value == maxAmmo || inventoryAmmo.Value == 0)
        {
            return;
        }

        Debug.Log("Reloading...");
        ReloadServerRpc(); // Trigger reload on the server
    }

    // Coroutine to handle the reload process
    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(reloadTime);

        // Calculate the ammo needed and update ammo counts
        int ammoNeeded = maxAmmo - currentAmmo.Value;
        if (inventoryAmmo.Value >= ammoNeeded)
        {
            currentAmmo.Value += ammoNeeded;
            inventoryAmmo.Value -= ammoNeeded;
        }
        else
        {
            currentAmmo.Value += inventoryAmmo.Value;
            inventoryAmmo.Value = 0;
        }

        Debug.Log("Reloaded ammo.");
        isReloading.Value = false; // Reset reloading state
    }
}