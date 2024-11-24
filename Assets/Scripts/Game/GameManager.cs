using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    public Transform[] spawnPoints;
    public GameObject mainMenuPanel; // Reference to the main menu panel

    private bool isMainMenuOpen = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"Number of spawn points: {spawnPoints?.Length ?? 0}");

        instance = this;

        LockCursor();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        if (RelayManager.instance.isHost)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            (byte[] allocationId, byte[] key, byte[] connectionData, string ip, int port) = RelayManager.instance.GetHostConnectionInfo();
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ip, (ushort)port, allocationId, key, connectionData, true);
            NetworkManager.Singleton.StartHost();

            // Delay host spawn
            StartCoroutine(DelayHostSpawn());
        }
        else
        {
            (byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) = RelayManager.instance.GetClientConnectionInfo();
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ip, (ushort)port, allocationId, key, connectionData, hostConnectionData, true);
            NetworkManager.Singleton.StartClient();

            // Delay client spawn
            StartCoroutine(DelayClientSpawn());
        }
    }

    IEnumerator WaitForPlayerObject(ulong clientId)
    {
        NetworkObject playerObject = null;
        while ((playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId)) == null)
        {
            yield return null;
        }

        Transform spawnPoint = GetSpawnPoint((int)clientId);
        if (spawnPoint != null)
        {
            playerObject.transform.position = spawnPoint.position;
            playerObject.transform.rotation = spawnPoint.rotation;
            Debug.Log($"Spawned client {clientId} at {spawnPoint.position}");
        }
        else
        {
            Debug.LogError($"No spawn point found for client {clientId}!");
        }
    }

    private IEnumerator DelayClientSpawn()
    {
        yield return new WaitForSeconds(0.5f); // Wait until the spawn process has completed

        var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (playerObject != null)
        {
            GameObject playerGameObject = playerObject.gameObject;  // Accessing the GameObject
            Transform spawnPoint = GetRandomSpawnPoint();

            if (spawnPoint != null)
            {
                playerGameObject.transform.position = spawnPoint.position;
                playerGameObject.transform.rotation = spawnPoint.rotation;
                Debug.Log("Client spawned at: " + spawnPoint.position);
            }
        }
        else
        {
            Debug.LogError("Failed to spawn client.");
        }
    }

    private IEnumerator DelayHostSpawn()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure NetworkObjects are initialized

        // Get the host's player object
        GameObject hostPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;

        if (hostPlayer != null)
        {
            Transform spawnPoint = GetRandomSpawnPoint();

            if (spawnPoint != null)
            {
                hostPlayer.transform.position = spawnPoint.position;
                hostPlayer.transform.rotation = spawnPoint.rotation;
                Debug.Log("Host spawned at: " + spawnPoint.position);
            }
            else
            {
                Debug.LogError("No spawn point found for the host.");
            }
        }
    }

    private void Update()
    {
        // Toggle main menu with Esc key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMainMenuOpen)
            {
                CloseMainMenu();
            }
            else
            {
                OpenMainMenu();
            }
        }
    }

    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log($"Connection approval request from client: {request.ClientNetworkId}");
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Pending = false;

        // Assign a spawn point to the player
        int playerId = (int)request.ClientNetworkId; // Use the client's network ID
        Transform spawnPoint = GetSpawnPoint(playerId);

        AssignSpawnPoint((ulong)playerId);

        if (spawnPoint != null)
        {
            // Set the position of the player object after creation
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    //GameObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId)?.gameObject;
                    //if (playerObject != null)
                    //{
                    //    playerObject.transform.position = spawnPoint.position;
                    //    playerObject.transform.rotation = spawnPoint.rotation;
                    //}
                    StartCoroutine(DelayClientSpawn());
                }
            };
        }
    }

    private void AssignSpawnPoint(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points are not set in the GameManager!");
            return;
        }

        // Convert ulong clientId to int for compatibility
        int playerId = (int)clientId;
        Transform spawnPoint = GetSpawnPoint(playerId);

        if (spawnPoint == null)
        {
            Debug.LogError($"No spawn point available for clientId {clientId}");
            return;
        }

        // Ensure the player object exists before setting position
        NetworkObject playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerNetworkObject != null)
        {
            playerNetworkObject.transform.position = spawnPoint.position;
            playerNetworkObject.transform.rotation = spawnPoint.rotation;
            Debug.Log($"Spawned client {clientId} at {spawnPoint.position}");
        }
        else
        {
            Debug.LogError($"Player object for client {clientId} not found!");
        }

        StartCoroutine(WaitForPlayerObject(clientId));
    }

    public Transform GetSpawnPoint(int playerId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned in the GameManager!");
            return null;
        }

        return spawnPoints[playerId % spawnPoints.Length];
    }

    public void BackToMainMenu()
    {
        // Shutdown the network and load the main menu
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu2");
    }

    public void OpenMainMenu()
    {
        isMainMenuOpen = true;
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        // Unlock cursor
        UnlockCursor();

        // Disable player controls
        TogglePlayerControls(false);
    }

    public void CloseMainMenu()
    {
        isMainMenuOpen = false;
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        // Lock cursor
        LockCursor();

        // Enable player controls
        TogglePlayerControls(true);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void TogglePlayerControls(bool enable)
    {
        // Find the player's input components and toggle their state
        GameObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().gameObject;
        if (localPlayer != null)
        {
            var playerInput = localPlayer.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = enable;
            }

            var playerMovement = localPlayer.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = enable;
            }

            var playerShooting = localPlayer.GetComponent<PlayerShooting>();
            if (playerShooting != null)
            {
                playerShooting.enabled = enable;
            }

            var playerCamera = localPlayer.GetComponent<PlayerCamera>();
            if (playerCamera != null)
            {
                playerCamera.enabled = enable;
            }
        }
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points are initialized in GameManager!");
            return null;
        }

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform chosenSpawn = spawnPoints[randomIndex];

        Debug.Log($"Selected spawn point: {chosenSpawn.name} at {chosenSpawn.position}");
        return chosenSpawn;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Transform spawnPoint = GameManager.instance.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
        }
    }
}
