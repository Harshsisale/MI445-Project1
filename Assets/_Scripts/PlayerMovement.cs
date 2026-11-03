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
    private bool allowMouseLook = true;
    private bool allowMovement = true;

    public bool MouseLookEnabled => allowMouseLook;
    public bool MovementEnabled => allowMovement;

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
        if (allowMouseLook)
            HandleMouseLook();
    }

    public void SetMouseLookEnabled(bool enabled)
    {
        allowMouseLook = enabled;
        lookInput = Vector2.zero;
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (allowMovement == enabled)
            return;

        allowMovement = enabled;
        if (!enabled)
        {
            moveInput = Vector2.zero;
            sprintHeld = false;
        }
    }

    // Input Events

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = allowMouseLook ? context.ReadValue<Vector2>() : Vector2.zero;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintHeld = context.ReadValueAsButton();
    }

    // Movement

private void HandleMovement()
{
    Vector2 effectiveInput = allowMovement ? moveInput : Vector2.zero;
    Vector3 direction =
        transform.right * effectiveInput.x +
        transform.forward * effectiveInput.y;

    direction = Vector3.ClampMagnitude(direction, 1f);

    bool movingForward =
        effectiveInput.y > 0f &&
        direction.sqrMagnitude > 0f;

    bool canSprint = sprintTimeRemaining > 0f;

    float currentSpeed = walkSpeed;

    // Sprint while Shift is held, we're moving forward,
    // and we still have sprint time remaining.
    if (allowMovement && sprintHeld && movingForward && canSprint)
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
