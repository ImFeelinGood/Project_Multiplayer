using UnityEngine;
using Unity.Netcode;

public class PlayerSetup : NetworkBehaviour
{
    private void Start()
    {
        // Ensure only the local player has an enabled AudioListener
        Camera playerCamera = GetComponentInChildren<Camera>();
        AudioListener audioListener = playerCamera?.GetComponent<AudioListener>();

        if (IsOwner)
        {
            // Enable AudioListener only for the local player
            if (audioListener != null) audioListener.enabled = true;
        }
        else
        {
            // Disable AudioListener for non-local players
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}
