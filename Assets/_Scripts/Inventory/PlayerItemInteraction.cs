
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
    [Header("Forgiving Pickups")]
    [SerializeField, Min(0.1f)] private float pickupReach = 5f;
    [Tooltip("How far from the crosshair an item's collider can be and still be picked up.")]
    [SerializeField, Min(0.01f)] private float pickupAimRadius = 0.65f;

    public PlayerInventory Inventory => inventory;
    public ItemDefinition HeldItem => itemHolder != null ? itemHolder.EquippedItem : null;
    public IInteractable CurrentInteractable { get; private set; }
    public RaycastHit CurrentHit { get; private set; }

    private InputAction interactAction;
    private InputAction pickupAction;
    private readonly RaycastHit[] rayHits = new RaycastHit[128];
    private readonly RaycastHit[] visibilityHits = new RaycastHit[128];
    private readonly Collider[] pickupCandidates = new Collider[128];

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
        InputAction action = CurrentInteractable is WorldItemPickup || CurrentInteractable is WorldRecipeInteractable
            ? pickupAction : interactAction;
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
        if (CurrentInteractable is WorldRecipeInteractable)
            return "Press E to read recipe";

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
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit directHit = default;
        IInteractable directTarget = null;
        int count = Physics.RaycastNonAlloc(ray, rayHits, Mathf.Max(interactionRange, pickupReach),
            interactionMask, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (IsPlayerCollider(rayHits[i].collider) || rayHits[i].distance >= nearest) continue;
            nearest = rayHits[i].distance;
            directHit = rayHits[i];
        }

        if (directHit.collider != null)
        {
            directTarget = directHit.collider.GetComponentInParent<IInteractable>();
            if (directTarget is MonoBehaviour component && !component.isActiveAndEnabled)
                directTarget = null;

            // Keep a directly aimed item or door/switch ahead of nearby aim-assist candidates.
            if ((directTarget is WorldItemPickup && directHit.distance <= pickupReach) ||
                (directTarget != null && !(directTarget is WorldItemPickup) &&
                 !(directTarget is DiggableMound) && directHit.distance <= interactionRange))
            {
                CurrentHit = directHit;
                return directTarget;
            }
        }

        WorldItemPickup pickup = FindForgivingPickup(ray, out RaycastHit pickupHit);
        if (pickup != null)
        {
            CurrentHit = pickupHit;
            return pickup;
        }

        if (directTarget != null && !(directTarget is WorldItemPickup) && directHit.distance <= interactionRange)
        {
            CurrentHit = directHit;
            return directTarget;
        }
        return null;
    }

    private WorldItemPickup FindForgivingPickup(Ray ray, out RaycastHit pickupHit)
    {
        pickupHit = default;
        WorldItemPickup best = null;
        float bestScore = float.PositiveInfinity;
        // An overlap capsule also finds tiny/trigger colliders and items so close
        // that the camera starts inside them, which a single ray can miss entirely.
        int count = Physics.OverlapCapsuleNonAlloc(ray.origin, ray.GetPoint(pickupReach), pickupAimRadius,
            pickupCandidates, interactionMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider candidate = pickupCandidates[i];
            if (IsPlayerCollider(candidate)) continue;
            WorldItemPickup pickup = candidate.GetComponentInParent<WorldItemPickup>();
            if (pickup == null || !pickup.isActiveAndEnabled) continue;

            Bounds bounds = candidate.bounds;
            float alongRay = Mathf.Clamp(Vector3.Dot(bounds.center - ray.origin, ray.direction), 0f, pickupReach);
            Vector3 point = bounds.ClosestPoint(ray.GetPoint(alongRay));
            Vector3 toPoint = point - ray.origin;
            if (toPoint.sqrMagnitude < 0.0001f) toPoint = bounds.center - ray.origin;
            float forward = Vector3.Dot(toPoint, ray.direction);
            if (forward < 0f || toPoint.sqrMagnitude > pickupReach * pickupReach) continue;
            float offAim = Vector3.ProjectOnPlane(toPoint, ray.direction).sqrMagnitude;
            if (offAim > pickupAimRadius * pickupAimRadius) continue;

            float score = offAim / Mathf.Max(forward * forward, 0.04f) + toPoint.sqrMagnitude * 0.0001f;
            if (score >= bestScore || !HasPickupSightline(ray.origin, point, pickup)) continue;

            best = pickup;
            bestScore = score;
            Vector3 direction = point - ray.origin;
            pickupHit = new RaycastHit { point = point, distance = direction.magnitude };
            if (direction.sqrMagnitude > 0.0001f && candidate.Raycast(new Ray(ray.origin, direction.normalized),
                    out RaycastHit actualHit, direction.magnitude + 0.05f))
                pickupHit = actualHit;
        }
        return best;
    }

    private bool HasPickupSightline(Vector3 origin, Vector3 point, WorldItemPickup pickup)
    {
        Vector3 direction = point - origin;
        float distance = direction.magnitude;
        if (distance < 0.001f) return true;
        int count = Physics.RaycastNonAlloc(origin, direction / distance, visibilityHits, distance,
            interactionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = visibilityHits[i];
            if (IsPlayerCollider(hit.collider) || hit.collider.GetComponentInParent<WorldItemPickup>() == pickup)
                continue;

            DiggableMound mound = hit.collider.GetComponentInParent<DiggableMound>();
            if (mound != null)
            {
                float reach = mound.GetBuriedPickupReach(hit);
                if (reach <= 0f || distance - hit.distance > reach + 0.01f) return false;
            }
            else if (hit.distance < distance - 0.04f)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsPlayerCollider(Collider collider)
    {
        Transform root = inventory != null ? inventory.transform : transform;
        return collider.transform.IsChildOf(root);
    }
}

