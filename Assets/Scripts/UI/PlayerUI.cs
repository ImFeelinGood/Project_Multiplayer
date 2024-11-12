using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;

    private PlayerInfo playerInfo;

    private void Start()
    {
        // Find the PlayerInfo component (assuming the player has this script)
        playerInfo = FindObjectOfType<PlayerInfo>();
    }

    private void Update()
    {
        // Only update UI for the local player
        if (playerInfo == null || !playerInfo.IsLocalPlayer) return;

        // Update the UI with the current status
        ammoText.text = $"Ammo: {playerInfo.currentAmmo} / {playerInfo.inventoryAmmo}";
        healthText.text = $"Health: {playerInfo.health} / 100";
    }
}