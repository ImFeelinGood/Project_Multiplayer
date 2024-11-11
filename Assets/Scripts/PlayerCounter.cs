using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCounter : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI playersCountText; // Reference to the TMP text field

    // NetworkVariable to hold the player count, with read permissions for everyone
    private NetworkVariable<int> playersNum = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    private void Start()
    {
        if (IsServer)
        {
            // Subscribe to client connected and disconnected events
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            // Unsubscribe from events to avoid memory leaks
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        // Update the TMP text with the current player count
        playersCountText.text = "Players: " + playersNum.Value.ToString();
    }

    // Called when a new client connects
    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            playersNum.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }
    }

    // Called when a client disconnects
    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            playersNum.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }
    }
}
