using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float sprintSpeed = 4f;
    [SerializeField] private float sprintDuration = 2f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Camera")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float lookSpeed = 0.1f;
    [SerializeField] private float maxLookAngle = 85f;

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintHeld;

    private float verticalVelocity;
    private float cameraPitch;
    private float sprintTimeRemaining;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        sprintTimeRemaining = sprintDuration;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    // Input Events

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintHeld = context.ReadValueAsButton();
    }

    // Movement

private void HandleMovement()
{
    Vector3 direction =
        transform.right * moveInput.x +
        transform.forward * moveInput.y;

    direction = Vector3.ClampMagnitude(direction, 1f);

    bool movingForward =
        moveInput.y > 0f &&
        direction.sqrMagnitude > 0f;

    bool canSprint = sprintTimeRemaining > 0f;

    float currentSpeed = walkSpeed;

    // Sprint while Shift is held, we're moving forward,
    // and we still have sprint time remaining.
    if (sprintHeld && movingForward && canSprint)
    {
        currentSpeed = sprintSpeed;

        sprintTimeRemaining -= Time.deltaTime;
        sprintTimeRemaining =
            Mathf.Max(sprintTimeRemaining, 0f);
    }

    // Only regenerate after Shift has been released.
    if (!sprintHeld)
    {
        sprintTimeRemaining += Time.deltaTime;
        sprintTimeRemaining =
            Mathf.Min(sprintTimeRemaining, sprintDuration);
    }

    Vector3 velocity = direction * currentSpeed;

    if (controller.isGrounded && verticalVelocity < 0f)
    {
        verticalVelocity = -2f;
    }

    verticalVelocity += gravity * Time.deltaTime;
    velocity.y = verticalVelocity;

    controller.Move(velocity * Time.deltaTime);
}

    // Camera

    private void HandleMouseLook()
    {
        float mouseX = lookInput.x * lookSpeed;
        float mouseY = lookInput.y * lookSpeed;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookAngle,
            maxLookAngle
        );

        playerCamera.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}