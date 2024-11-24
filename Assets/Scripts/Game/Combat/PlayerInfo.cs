using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;

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
    public float respawnDelay = 3f; // Delay before respawn

    [Header("Animator")]
    public Animator animator; // Reference to the player's Animator

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Sound Effects")]
    public AudioClip reloadSFX;

    private bool isDead = false; // Track death state

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
        if (!IsServer) return;

        health.Value -= damage;
        if (health.Value <= 0 && !isDead)
        {
            isDead = true; // Set death state
            StartDeathSequence();
        }
    }

    private void StartDeathSequence()
    {
        Debug.Log($"{gameObject.name} has died!");

        // Disable player controls
        DisablePlayerControls();

        // Trigger respawn logic after a delay
        StartCoroutine(RespawnPlayer());
    }

    private IEnumerator RespawnPlayer()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Request the server to respawn the player
        RespawnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RespawnServerRpc()
    {
        if (GameManager.instance == null)
        {
            Debug.LogError("GameManager instance is null. Cannot respawn player.");
            return;
        }

        Transform spawnPoint = GameManager.instance.GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("No valid spawn point found for respawn.");
            return;
        }

        Debug.Log($"Respawning player at: {spawnPoint.position}");

        // Disable syncing on the client-side
        RespawnClientRpc(spawnPoint.position, spawnPoint.rotation);

        // Set position and rotation on the server as well
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        health.Value = 100;
        inventoryAmmo.Value = 30;
        currentAmmo.Value = maxAmmo;
        isDead = false;

        StartCoroutine(ResetRespawningFlag());
    }

    private void DisableClientNetworkTransform()
    {
        var clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        if (clientNetworkTransform != null)
        {
            clientNetworkTransform.enabled = false; // Temporarily disable syncing
        }
    }

    private void EnableClientNetworkTransform()
    {
        var clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        if (clientNetworkTransform != null)
        {
            clientNetworkTransform.enabled = true; // Re-enable syncing
        }
    }

    /*
    [ClientRpc]
    private void RespawnClientRpc(Vector3 newPosition, Quaternion newRotation)
    {
        StartCoroutine(ApplyRespawnPosition(newPosition, newRotation));
    }

    private IEnumerator ApplyRespawnPosition(Vector3 newPosition, Quaternion newRotation)
    {
        // Wait for server updates to propagate
        yield return new WaitForSeconds(0.1f);

        Debug.Log($"Client: Applying respawn position {newPosition}");
        transform.position = newPosition;
        transform.rotation = newRotation;

        EnablePlayerControls();
    }
    */
    [ClientRpc]
    private void RespawnClientRpc(Vector3 newPosition, Quaternion newRotation)
    {
        Debug.Log($"Client: Respawning at {newPosition}");

        // Disable the ClientNetworkTransform to stop syncing temporarily
        DisableClientNetworkTransform();

        // Update position and rotation
        transform.position = newPosition;
        transform.rotation = newRotation;

        // Re-enable syncing
        EnableClientNetworkTransform();

        StartCoroutine(ReEnableControlsAfterSync());
    }

    private IEnumerator ReEnableControlsAfterSync()
    {
        // Wait for synchronization
        yield return new WaitForSeconds(0.1f);

        // Re-enable controls
        EnablePlayerControls();

        Debug.Log("Player controls re-enabled after respawn.");
    }

    private IEnumerator ResetRespawningFlag()
    {
        yield return new WaitForSeconds(0.1f);
        isRespawning = false;
    }

    private bool isRespawning = false;

    private void Update()
    {
        if (isRespawning) return; // Skip updates during respawn

        // Normal movement or transform update logic
    }

    private void DisablePlayerControls()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Disable other components
        var playerInput = GetComponent<PlayerInput>();
        var playerMovement = GetComponent<PlayerMovement>();
        var playerShooting = GetComponent<PlayerShooting>();

        if (playerInput != null) playerInput.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;
    }

    private void EnablePlayerControls()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Enable other components
        var playerInput = GetComponent<PlayerInput>();
        var playerMovement = GetComponent<PlayerMovement>();
        var playerShooting = GetComponent<PlayerShooting>();

        if (playerInput != null) playerInput.enabled = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;
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

        PlaySound(reloadSFX);
        TriggerReloadAnimation();
        Debug.Log("Reloading...");
        ReloadServerRpc(); // Trigger reload on the server
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void TriggerReloadAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("ReloadTrigger"); // Set the trigger for the empty animation
        }
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