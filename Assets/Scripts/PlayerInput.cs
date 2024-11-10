using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerInput : NetworkBehaviour
{
    [SerializeField] private PlayerInfo playerInfo; // Changed variable name for clarity
    [SerializeField] private PlayerShooting playerShooting;

    void Update()
    {
        if (!IsOwner) return;

        // Shooting
        if (Input.GetButtonDown("Fire1")) // Left mouse button
        {
            playerShooting.Shoot();
        }

        // Reload
        if (Input.GetKeyDown(KeyCode.R)) // Reload with R key
        {
            playerInfo.Reload();
        }
    }
}
