using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerInfo : NetworkBehaviour
{
    public int maxAmmo = 6; // Max ammo in magazine
    public int inventoryAmmo = 30; // Total ammo available
    public int currentAmmo; // Current ammo in magazine
    public int health = 100; // Player health

    private void Start()
    {
        currentAmmo = maxAmmo; // Start with a full magazine
    }

    // Method to decrease health
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Handle player death (you can call a death method here)
            Debug.Log("Player died.");
            // Optionally, you could also trigger a respawn or disable the player
        }
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
        // Check if the collider is a projectile
        if (other.CompareTag("Projectile")) // Ensure your projectile prefab has this tag
        {
            // Assuming the projectile has a script that holds its damage value
            Projectile projectile = other.GetComponent<Projectile>();
            if (projectile != null)
            {
                TakeDamage(projectile.damage); // Apply damage from projectile
                Destroy(other.gameObject); // Destroy the projectile on hit
            }
        }
    }
}
