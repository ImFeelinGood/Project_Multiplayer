using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 7f;
    [SerializeField] private float rotationSpeed = 500f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public bool isMoving; // Tracks if the player is moving
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned in PlayerMovement script!");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            int playerId = (int)OwnerClientId;
            Transform spawnPoint = GameManager.instance.GetSpawnPoint(playerId);

            UpdatePositionServerRPC(spawnPoint.position, spawnPoint.rotation);
        }
    }

    void Update()
    {
        if (!IsOwner || cameraTransform == null) return;

        // Check if the player is grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Cache input values
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Determine if the player is moving
        isMoving = (horizontalInput != 0 || verticalInput != 0);

        // Movement logic
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 movementDirection = forward * verticalInput + right * horizontalInput;
        movementDirection.Normalize();

        // Move the player
        controller.Move(movementDirection * movementSpeed * Time.deltaTime);

        // Jump if grounded
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Rotate the player
        if (verticalInput != 0)
        {
            Quaternion toRotation = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePositionServerRPC(Vector3 position, Quaternion rotation)
    {
        targetPosition = position;
        targetRotation = rotation;
    }
}



//Dont mind me this is just a RigidBody movement that failed
/*public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 7f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 currentVelocity; // Store current velocity for momentum
    private float groundDrag = 0.1f; // How fast to slow down on the ground
    private float airControlFactor = 0.5f; // Factor for air control adjustments

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned in PlayerMovement script!");
        }
    }

    void Update()
    {
        if (!IsOwner || cameraTransform == null) return;

        // Check if the player is grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Cache input values
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate movement direction relative to the camera's orientation
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction based on input
        Vector3 movementDirection = forward * verticalInput + right * horizontalInput;

        if (isGrounded)
        {
            // Ground movement logic
            if (horizontalInput == 0 && verticalInput == 0)
            {
                // Apply ground drag to slow down momentum
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0, groundDrag);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0, groundDrag);
            }
            else
            {
                // Update current velocity with movement input
                currentVelocity = movementDirection * movementSpeed;
            }
        }
        else
        {
            // While in the air, retain horizontal momentum but allow slight adjustments
            currentVelocity.x = rb.velocity.x; // Keep horizontal momentum
            currentVelocity.z = rb.velocity.z; // Keep horizontal momentum

            // Adjust horizontal speed based on input while in the air (limited control)
            if (horizontalInput != 0)
            {
                currentVelocity.x += horizontalInput * movementSpeed * airControlFactor * Time.deltaTime; // Slight strafe left/right
            }

            // Allow backward movement to slow down forward momentum
            if (verticalInput < 0) // Move backward
            {
                currentVelocity.z += verticalInput * movementSpeed * airControlFactor * Time.deltaTime; // Adjust backward movement
            }
            else if (verticalInput > 0) // Move forward
            {
                currentVelocity.z += verticalInput * movementSpeed * Time.deltaTime; // Continue forward momentum
            }

            // Clamp the horizontal velocity to limit maximum speed in the air
            currentVelocity.x = Mathf.Clamp(currentVelocity.x, -movementSpeed, movementSpeed);
            currentVelocity.z = Mathf.Clamp(currentVelocity.z, -movementSpeed, movementSpeed);
        }

        // Apply horizontal movement to the Rigidbody
        rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);

        // Jump if grounded and jump button is pressed
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Rotate player to face forward/backward movement direction only
        if (verticalInput != 0)
        {
            Quaternion toRotation = Quaternion.LookRotation(forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, Time.deltaTime * 500f);
        }
    }
}
*/