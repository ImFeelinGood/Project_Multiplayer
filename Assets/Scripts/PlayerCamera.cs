using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    [Header("Recoil Settings")]
    public float recoilAmount = 1f; // How strong the recoil is
    public float recoilSpeed = 5f; // How fast the recoil is applied
    public float recoilRecoverySpeed = 10f; // How fast the recoil recovers

    private float xRotation = 0f;
    private float recoilRotation = 0f; // Stores the current recoil offset
    private float targetRecoil = 0f; // The target recoil value

    // Network variable for camera rotation
    private NetworkVariable<Quaternion> networkCameraRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Start()
    {
        if (!IsOwner)
        {
            // Disable the local camera for other players
            GetComponent<Camera>().enabled = false;
            return;
        }

        // Lock the cursor for the owning player
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!IsOwner)
        {
            // Apply the synced rotation for non-owners
            transform.localRotation = networkCameraRotation.Value;
            return;
        }

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

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

    // Method to apply recoil
    public void ApplyRecoil()
    {
        // Set the target recoil amount
        recoilRotation += recoilAmount;
    }
}
