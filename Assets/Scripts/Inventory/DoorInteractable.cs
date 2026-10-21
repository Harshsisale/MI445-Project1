using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float openSpeed = 120f;
    [SerializeField] private bool isLocked;
    [SerializeField] private ItemDefinition requiredKey;

    private Quaternion closedRotation;
    private bool isOpen;

    public bool IsLocked => isLocked;
    public string Prompt => isLocked
        ? requiredKey != null ? "Unlock door with " + requiredKey.displayName : "Door locked"
        : isOpen ? "Close door" : "Open door";

    private void Awake()
    {
        if (hinge == null)
            hinge = transform;

        closedRotation = hinge.localRotation;
    }

    private void Update()
    {
        Quaternion target = isOpen
            ? closedRotation * Quaternion.Euler(0f, openAngle, 0f)
            : closedRotation;

        hinge.localRotation = Quaternion.RotateTowards(
            hinge.localRotation, target, openSpeed * Time.deltaTime);
    }

    public void Interact(PlayerItemInteraction player)
    {
        if (isLocked)
        {
            if (requiredKey == null || player == null || player.Inventory == null ||
                !player.Inventory.HasItem(requiredKey))
                return;

            SetLocked(false);
        }

        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (open && isLocked)
            return;

        isOpen = open;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (isLocked)
            SetOpen(false);
    }
}
