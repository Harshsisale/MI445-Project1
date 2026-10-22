
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerItemInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerItemHolder itemHolder;
    [SerializeField] private TMP_Text promptText;
    [FormerlySerializedAs("pickupRange")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    [FormerlySerializedAs("pickupMask")]
    [SerializeField] private LayerMask interactionMask = ~0;

    public PlayerInventory Inventory => inventory;
    public ItemDefinition HeldItem => itemHolder != null ? itemHolder.EquippedItem : null;
    public IInteractable CurrentInteractable { get; private set; }
    public RaycastHit CurrentHit { get; private set; }

    private InputAction interactAction;
    private InputAction pickupAction;

    private void Awake()
    {
        interactAction = playerInput.actions.FindAction("Interact", true);
        pickupAction = playerInput.actions.FindAction("Pickup", true);
    }

    private void Update()
    {
        CurrentInteractable = null;
        CurrentHit = default;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        CurrentInteractable = FindInteractable();
        HandleInteractionInput();
    }

    private void HandleInteractionInput()
    {
        InputAction action = CurrentInteractable is WorldItemPickup ? pickupAction : interactAction;
        if (action.WasPressedThisFrame())
            CurrentInteractable?.Interact(this);
    }

    private void LateUpdate()
    {
        if (promptText == null)
            return;

        bool showPrompt = Cursor.lockState == CursorLockMode.Locked &&
            CurrentInteractable is MonoBehaviour target && target != null && target.isActiveAndEnabled;

        promptText.text = showPrompt
            ? GetInteractionPrompt()
            : "";
    }

    private string GetInteractionPrompt()
    {
        if (CurrentInteractable is SwitchInteractable switchInteractable)
            return switchInteractable.GetInteractionPrompt(this);

        return (CurrentInteractable is WorldItemPickup ? "E - " : "Left click - ") + CurrentInteractable.Prompt;
    }

    private void OnDisable()
    {
        CurrentInteractable = null;
        CurrentHit = default;

        if (promptText != null)
            promptText.text = "";
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

        if (interactable is DiggableMound mound &&
            TryFindBuriedPickup(ray, hit, mound, out RaycastHit pickupHit, out WorldItemPickup pickup))
        {
            hit = pickupHit;
            interactable = pickup;
        }

        CurrentHit = hit;
        return interactable;
    }

    private bool TryFindBuriedPickup(Ray ray, RaycastHit moundHit, DiggableMound mound,
        out RaycastHit pickupHit, out WorldItemPickup pickup)
    {
        pickupHit = default;
        pickup = null;

        const float surfaceOffset = 0.001f;
        float reach = Mathf.Min(mound.GetBuriedPickupReach(moundHit), interactionRange - moundHit.distance);
        if (reach <= surfaceOffset)
            return false;

        // Start just inside the excavated surface. The next collider must be
        // a pickup; walls, ground and any other obstruction still block it.
        Vector3 origin = moundHit.point + ray.direction * surfaceOffset;
        if (!Physics.Raycast(origin, ray.direction, out pickupHit, reach - surfaceOffset,
                interactionMask, QueryTriggerInteraction.Ignore))
            return false;

        pickup = pickupHit.collider.GetComponentInParent<WorldItemPickup>();
        if (pickup == null || !pickup.isActiveAndEnabled)
            return false;

        pickupHit.distance += moundHit.distance + surfaceOffset;
        return true;
    }
}

