using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI reloadNoticeText; // Text for reloading notice

    private PlayerInfo playerInfo;
    private Coroutine reloadNoticeCoroutine;

    private void Start()
    {
        // Find the PlayerInfo component (assuming the player has this script)
        playerInfo = FindObjectOfType<PlayerInfo>();
        reloadNoticeText.gameObject.SetActive(false); // Initially hide the reload notice
    }

    private void Update()
    {
        // Only update UI for the local player
        if (playerInfo == null || !playerInfo.IsLocalPlayer) return;

        // Update the UI with the current status
        ammoText.text = $"Ammo: {playerInfo.currentAmmo.Value} / {playerInfo.inventoryAmmo.Value}";
        healthText.text = $"Health: {playerInfo.health.Value} / 100";

        // Handle reloading UI state
        if (playerInfo.isReloading.Value)
        {
            ShowReloadNotice("Reloading...");
        }
        else if (reloadNoticeText.text == "Reloading...")
        {
            ShowReloadNotice("Reloaded", 1f); // Show "Reloaded" message for 1 second
        }
    }

    // Method to show reloading notice
    public void ShowReloadNotice(string message, float duration = 0f)
    {
        reloadNoticeText.text = message;
        reloadNoticeText.gameObject.SetActive(true);

        if (reloadNoticeCoroutine != null)
        {
            StopCoroutine(reloadNoticeCoroutine);
        }

        // Hide after a delay if a duration is specified
        if (duration > 0f)
        {
            reloadNoticeCoroutine = StartCoroutine(HideReloadNoticeAfterDelay(duration));
        }
    }

    private IEnumerator HideReloadNoticeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        reloadNoticeText.gameObject.SetActive(false);
    }

    // Method to handle shake and red text effect
    public void TriggerReloadWarning()
    {
        if (reloadNoticeCoroutine != null)
        {
            StopCoroutine(reloadNoticeCoroutine);
        }

        StartCoroutine(ShakeText(reloadNoticeText));
    }

    private IEnumerator ShakeText(TextMeshProUGUI text)
    {
        text.text = "Cannot shoot while reloading!";
        text.color = Color.red;
        text.gameObject.SetActive(true);

        Vector3 originalPosition = text.rectTransform.localPosition;
        float duration = 0.5f; // Shake duration
        float magnitude = 5f;  // Shake magnitude
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-magnitude, magnitude);
            float offsetY = Random.Range(-magnitude, magnitude);
            text.rectTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        text.rectTransform.localPosition = originalPosition;
        text.color = Color.white;
        text.gameObject.SetActive(false);
    }
}
