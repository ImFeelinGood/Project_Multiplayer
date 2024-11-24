using UnityEngine;
using Unity.Netcode;
using System.Collections;
using game; // Adjust the namespace if needed to access GameLobbyManager

public class PickupSpawner : MonoBehaviour
{
    public GameObject ammoPickupPrefab; // Reference to the AmmoPickup prefab
    public GameObject healthPickupPrefab; // Reference to the HealthPickup prefab
    public Transform[] spawnPoints; // Points where pickups can spawn
    public float spawnInterval = 5f; // Time between respawn attempts

    private bool isHost = false;
    private GameObject[] activePickups; // Tracks active pickups at each spawn point

    private int healthPickupCount = 0; // Counter for active HealthPickups
    private int ammoPickupCount = 0; // Counter for active AmmoPickups

    private void Start()
    {
        // Determine if this client is the host based on LobbyManager
        isHost = GameLobbyManager.instance.isHost;

        if (isHost)
        {
            Debug.Log("This client is the host. PickupSpawner will run.");
            activePickups = new GameObject[spawnPoints.Length];
            StartCoroutine(SpawnPickups());
        }
        else
        {
            Debug.Log("This client is not the host. PickupSpawner will not run.");
        }
    }

    private IEnumerator SpawnPickups()
    {
        while (true)
        {
            Debug.Log("Attempting to spawn pickups...");
            yield return new WaitForSeconds(spawnInterval);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (activePickups[i] == null) // Only spawn if the point is free
                {
                    Transform spawnPoint = spawnPoints[i];
                    Debug.Log($"Spawning at {spawnPoint.position}");

                    // Determine which pickup to spawn based on the current count
                    GameObject prefabToSpawn = GetPickupToSpawn();

                    if (prefabToSpawn != null)
                    {
                        // Instantiate the pickup
                        GameObject pickup = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

                        // Ensure it has a NetworkObject component
                        if (pickup.TryGetComponent<NetworkObject>(out var networkObject))
                        {
                            networkObject.Spawn(true); // Host spawns objects
                            Debug.Log($"Spawned {pickup.name} at {spawnPoint.position}");

                            // Track the spawned pickup
                            activePickups[i] = pickup;

                            // Update the respective counter
                            if (pickup.GetComponent<HealthPickup>() != null)
                            {
                                healthPickupCount++;
                            }
                            else if (pickup.GetComponent<AmmoPickup>() != null)
                            {
                                ammoPickupCount++;
                            }

                            // Add a callback to clear the spawn point when the pickup is taken
                            pickup.GetComponent<BasePickup>().OnPickupTaken += () => OnPickupTaken(i, pickup);
                        }
                        else
                        {
                            Debug.LogError($"Prefab {prefabToSpawn.name} is missing a NetworkObject component!");
                            Destroy(pickup); // Cleanup to avoid lingering invalid GameObjects
                        }
                    }
                }
            }
        }
    }

    // Determine which pickup to spawn based on the current count
    private GameObject GetPickupToSpawn()
    {
        // Calculate the number of pickups to spawn for each type
        if (healthPickupCount > ammoPickupCount)
        {
            // If there are more health pickups, spawn an AmmoPickup
            return ammoPickupPrefab;
        }
        else if (ammoPickupCount > healthPickupCount)
        {
            // If there are more ammo pickups, spawn a HealthPickup
            return healthPickupPrefab;
        }
        else
        {
            // If both are equal, randomly choose between Ammo or Health
            return Random.value > 0.5f ? ammoPickupPrefab : healthPickupPrefab;
        }
    }

    // Called when a pickup is taken by a player
    private void OnPickupTaken(int index, GameObject takenPickup)
    {
        activePickups[index] = null; // Mark the spawn point as free
        Debug.Log($"Pickup at spawn point {index} was taken. Point is now free.");

        // Update the respective pickup counter
        if (takenPickup.GetComponent<HealthPickup>() != null)
        {
            healthPickupCount--;
        }
        else if (takenPickup.GetComponent<AmmoPickup>() != null)
        {
            ammoPickupCount--;
        }

        // Start respawn after a delay
        StartCoroutine(RespawnPickup(index));
    }

    // Coroutine to respawn the pickup after a delay
    private IEnumerator RespawnPickup(int index)
    {
        yield return new WaitForSeconds(10f); // Adjust the respawn delay as needed

        // Respawn the pickup at the given spawn point
        Transform spawnPoint = spawnPoints[index];
        GameObject prefabToSpawn = GetPickupToSpawn();

        if (prefabToSpawn != null)
        {
            // Instantiate the pickup
            GameObject pickup = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            activePickups[index] = pickup;

            // Update the respective counter
            if (pickup.GetComponent<HealthPickup>() != null)
            {
                healthPickupCount++;
            }
            else if (pickup.GetComponent<AmmoPickup>() != null)
            {
                ammoPickupCount++;
            }

            // Ensure it has a NetworkObject component and spawn it
            if (pickup.TryGetComponent<NetworkObject>(out var networkObject))
            {
                networkObject.Spawn(true); // Host spawns objects
                Debug.Log($"Respawned {pickup.name} at {spawnPoint.position}");
            }
            else
            {
                Debug.LogError($"Prefab {prefabToSpawn.name} is missing a NetworkObject component!");
                Destroy(pickup);
            }
        }
    }
}
