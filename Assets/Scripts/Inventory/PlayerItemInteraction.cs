
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerItemInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerItemHolder itemHolder;
    [FormerlySerializedAs("pickupRange")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [FormerlySerializedAs("pickupMask")]
    [SerializeField] private LayerMask interactionMask = ~0;

    public PlayerInventory Inventory => inventory;
    public ItemDefinition HeldItem => itemHolder != null ? itemHolder.EquippedItem : null;
    public IInteractable CurrentInteractable { get; private set; }

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = playerInput.actions.FindAction("Interact");
    }

    private void Update()
    {
        CurrentInteractable = null;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        CurrentInteractable = FindInteractable();

        if (interactAction.WasPressedThisFrame())
            CurrentInteractable?.Interact(this);
    }

    private IInteractable FindInteractable()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionMask,
            QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        IInteractable interactable =
            hit.collider.GetComponentInParent<IInteractable>();

        if (interactable is MonoBehaviour component && !component.isActiveAndEnabled)
            return null;

        return interactable;
    }
}

