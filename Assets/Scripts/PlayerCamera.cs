using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        if (!IsOwner)
        {
            // Disable the camera for other players
            GetComponent<Camera>().enabled = false;
            return;
        }

        // Lock the cursor for the owning player
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Only allow camera control if this is the owning player
        if (!IsOwner) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust vertical rotation (looking up and down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply the rotation to the camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate the player body horizontally (left and right)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}