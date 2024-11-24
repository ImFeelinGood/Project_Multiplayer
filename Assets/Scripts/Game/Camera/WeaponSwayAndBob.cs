using UnityEngine;
using Unity.Netcode;

public class WeaponSwayAndBob : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponTransform; // The weapon or holder transform
    [SerializeField] private CharacterController playerController; // The player's CharacterController

    [Header("Sway Settings")]
    [SerializeField] private float swayStep = 0.01f; // Positional sway amount
    [SerializeField] private float swayMaxDistance = 0.06f; // Max positional sway distance
    [SerializeField] private float swayRotationStep = 4f; // Rotational sway amount
    [SerializeField] private float swayMaxRotation = 5f; // Max rotational sway angle
    [SerializeField] private float swaySmoothness = 10f; // Sway interpolation speed

    [Header("Bobbing Settings")]
    [SerializeField] private float bobSpeed = 6f; // Speed of bobbing
    [SerializeField] private float bobAmount = 0.02f; // Height of bobbing
    [SerializeField] private float bobRotationAmount = 2f; // Rotational bobbing amount
    [SerializeField] private Vector3 travelLimit = new Vector3(0.025f, 0.025f, 0.025f); // Max positional bobbing offset
    [SerializeField] private Vector3 rotationMultiplier = new Vector3(2f, 1f, 1f); // Rotational bobbing multiplier
    [SerializeField] private PlayerMovement mover; // Reference to PlayerMovement

    [Header("Jump and Fall Bobbing")]
    [SerializeField] private float jumpBobAmount = 0.03f;  // Bob amount during jump
    [SerializeField] private float fallBobAmount = 0.02f;  // Bob amount during fall
    [SerializeField] private float landingBobAmount = 0.04f;  // Bob amount on landing
    [SerializeField] private float landingBobSpeed = 12f; // Speed of landing bob recovery

    private Vector3 swayPosition; // Current sway position offset
    private Vector3 swayRotation; // Current sway rotation offset
    private Vector3 bobPosition; // Current bob position offset
    private Vector3 bobRotation; // Current bob rotation offset

    private float bobTimer; // Timer for bobbing
    private Vector3 initialPosition; // Initial local position of the weapon
    private Quaternion initialRotation; // Initial local rotation of the weapon

    void Start()
    {
        // Cache initial position and rotation
        initialPosition = weaponTransform.localPosition;
        initialRotation = weaponTransform.localRotation;
    }

    private void Update()
    {
        // Ensure this logic only runs on the owner
        if (!IsOwner) return;

        ApplyWeaponSway();
        ApplyBobbing();
        CompositePositionAndRotation();
    }

    private bool DetermineOwnership()
    {
        // Replace this with actual ownership logic
        // For example: return GetComponent<NetworkObject>().IsOwner;
        return true; // Simulated owner
    }

    private void ApplyWeaponSway()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Invert the input to achieve opposite direction sway
        Vector3 swayOffset = new Vector3(
            Mathf.Clamp(mouseX * -swayStep, -swayMaxDistance, swayMaxDistance), // Invert on X
            Mathf.Clamp(mouseY * -swayStep, -swayMaxDistance, swayMaxDistance), // Invert on Y
            0f
        );
        swayPosition = swayOffset; // Apply the inverted sway

        // Inverted sway rotation
        Vector3 rotationOffset = new Vector3(
            Mathf.Clamp(mouseY * swayRotationStep, -swayMaxRotation, swayMaxRotation), // Inverted pitch
            Mathf.Clamp(-mouseX * swayRotationStep, -swayMaxRotation, swayMaxRotation), // Inverted yaw
            Mathf.Clamp(-mouseX * swayRotationStep * 0.5f, -swayMaxRotation, swayMaxRotation) // Inverted roll
        );
        swayRotation = rotationOffset;
    }

    private void ApplyBobbing()
    {
        // Check if the player is moving
        bool isMoving = mover.isMoving;

        if (mover.isJumping)
        {
            // Apply jump bob effect
            bobPosition = new Vector3(0f, -jumpBobAmount, 0f); // Downward bob when jumping
            bobRotation = Vector3.zero; // No rotation during jump
        }
        else if (mover.isFalling)
        {
            // Apply fall bob effect
            bobPosition = new Vector3(0f, +fallBobAmount, 0f); // Upward bob when falling
            bobRotation = Vector3.zero; // No rotation during fall
        }
        else if (mover.hasLanded)
        {
            // Apply landing bob effect
            bobPosition = Vector3.Lerp(bobPosition, new Vector3(0f, -landingBobAmount, 0f), Time.deltaTime * landingBobSpeed);
            bobRotation = Vector3.zero; // No rotation during landing
        }
        else if (isMoving)
        {
            // Increment bobbing timer for standard movement
            bobTimer += Time.deltaTime * bobSpeed;

            // Calculate bobbing offsets
            float bobSin = Mathf.Sin(bobTimer);

            bobPosition = new Vector3(
                0f, // No horizontal bobbing
                bobSin * bobAmount, // Vertical bobbing
                0f  // No depth bobbing
            );

            bobRotation = Vector3.zero; // No rotational bobbing for simplicity
        }
        else
        {
            // Reset bobbing when stationary
            bobTimer = 0f;
            bobPosition = Vector3.Lerp(bobPosition, Vector3.zero, Time.deltaTime * swaySmoothness);
            bobRotation = Vector3.Lerp(bobRotation, Vector3.zero, Time.deltaTime * swaySmoothness);
        }
    }

    private void CompositePositionAndRotation()
    {
        // Combine sway and bob offsets
        weaponTransform.localPosition = Vector3.Lerp(
            weaponTransform.localPosition,
            initialPosition + swayPosition + bobPosition,
            Time.deltaTime * swaySmoothness
        );

        weaponTransform.localRotation = Quaternion.Lerp(
            weaponTransform.localRotation,
            initialRotation * Quaternion.Euler(swayRotation + bobRotation),
            Time.deltaTime * swaySmoothness
        );
    }
}