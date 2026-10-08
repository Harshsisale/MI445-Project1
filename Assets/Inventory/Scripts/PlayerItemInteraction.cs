
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask pickupMask = ~0;

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = playerInput.actions.FindAction("Interact");
    }

    private void Update()
    {
        if (!interactAction.WasPressedThisFrame())
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupRange,
            pickupMask,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        WorldItemPickup pickup =
            hit.collider.GetComponentInParent<WorldItemPickup>();

        if (pickup != null)
            pickup.TryPickup(inventory);
    }
}
