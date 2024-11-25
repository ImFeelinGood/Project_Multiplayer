using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 1f; // Default sensitivity
    [SerializeField] private Transform playerBody;

    [Header("Recoil Settings")]
    public float recoilAmount = 1f; // How strong the recoil is
    public float recoilSpeed = 5f; // How fast the recoil is applied
    public float recoilRecoverySpeed = 10f; // How fast the recoil recovers

    private float xRotation = 0f;
    private float recoilRotation = 0f; // Stores the current recoil offset

    // Network variable for camera rotation
    private NetworkVariable<Quaternion> networkCameraRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool isCameraActive = true; // Tracks whether the camera is active

    void Start()
    {
        if (!IsOwner)
        {
            // Disable the local camera for other players
            GetComponent<Camera>().enabled = false;
            return;
        }

        // Lock cursor and load saved sensitivity
        Cursor.lockState = CursorLockMode.Locked;

        // Normalize sensitivity by screen width (optional)
        float baseWidth = 1920f; // Base resolution width
        float resolutionScale = Screen.width / baseWidth;
        mouseSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 1f) * resolutionScale;
    }

    void Update()
    {
        if (!IsOwner)
        {
            // Apply the synced rotation for non-owners
            transform.localRotation = networkCameraRotation.Value;
            return;
        }

        if (!isCameraActive) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Adjust vertical rotation (looking up and down) based on mouse input
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply recoil rotation offset
        xRotation -= recoilRotation;

        // Set the camera's local rotation
        Quaternion newRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.localRotation = newRotation;

        // Rotate the player body horizontally (left and right)
        playerBody.Rotate(Vector3.up * mouseX);

        // Smoothly recover the recoil rotation back to zero
        recoilRotation = Mathf.Lerp(recoilRotation, 0f, recoilRecoverySpeed * Time.deltaTime);

        // Update the camera rotation in the network variable
        networkCameraRotation.Value = transform.localRotation;
    }

    public void SetCameraActive(bool active)
    {
        isCameraActive = active;
        if (!active)
        {
            networkCameraRotation.Value = transform.localRotation; // Ensure rotation remains consistent for others
        }
    }

    public void UpdateSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
        Debug.Log($"Mouse sensitivity updated to: {mouseSensitivity}");
    }

    // Method to apply recoil
    public void ApplyRecoil()
    {
        // Set the target recoil amount
        recoilRotation += recoilAmount;
    }
}