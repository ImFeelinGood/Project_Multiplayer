using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;
    public Transform[] spawnPoints;
    public GameObject mainMenuPanel; // Reference to the main menu panel

    private bool isMainMenuOpen = false;

    private void Awake()
    {
        // Ensure only one instance exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize the instance and lock cursor
        instance = this;
        LockCursor();

        // Hide the main menu panel at the start
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
        }
        else
        {
            (byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData, string ip, int port) = RelayManager.instance.GetClientConnectionInfo();
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ip, (ushort)port, allocationId, key, connectionData, hostConnectionData, true);
            NetworkManager.Singleton.StartClient();
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
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Pending = false;
    }

    public Transform GetSpawnPoint(int playerId)
    {
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
}
