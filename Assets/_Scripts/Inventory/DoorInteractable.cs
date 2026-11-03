using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float openSpeed = 120f;
    [SerializeField] private bool isLocked;
    [SerializeField] private ItemDefinition requiredKey;
    [SerializeField] private bool isOpen;
    [Tooltip("Optional controlling door. Both doors use its open and locked state, and interactions from either side control it.")]
    [SerializeField] private DoorInteractable stateSource;

    [Tooltip("Plays when an interaction successfully opens or closes this unlocked door.")]
    public AudioSource doorOpen;

    private Quaternion closedRotation;

    public bool IsOpen => stateSource != null ? stateSource.IsOpen : isOpen;
    public bool IsLocked => stateSource != null ? stateSource.IsLocked : isLocked;
    public string Prompt => stateSource != null ? stateSource.Prompt : isLocked
        ? requiredKey != null ? "Unlock door with " + requiredKey.displayName : "Door locked"
        : isOpen ? "Close door" : "Open door";

    private void Awake()
    {
        if (hinge == null)
            hinge = transform;

        closedRotation = hinge.localRotation;
        if (isLocked)
            isOpen = false;
    }

    private void Start()
    {
        if (stateSource == null && isOpen)
            hinge.localRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        if (stateSource != null)
            return;

        Quaternion target = isOpen
            ? closedRotation * Quaternion.Euler(0f, openAngle, 0f)
            : closedRotation;

        hinge.localRotation = Quaternion.RotateTowards(
            hinge.localRotation, target, openSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (stateSource == null)
            return;

        // Copy the current swing after Update so both sides match during animation,
        // while retaining each doorway's own closed orientation and position.
        DoorInteractable source = stateSource;
        while (source.stateSource != null)
            source = source.stateSource;

        hinge.localRotation = closedRotation *
            (Quaternion.Inverse(source.closedRotation) * source.hinge.localRotation);
    }

    public void Interact(PlayerItemInteraction player)
    {
        DoorInteractable controllingDoor = this;
        AudioSource interactionSound = doorOpen;
        while (controllingDoor.stateSource != null)
        {
            controllingDoor = controllingDoor.stateSource;
            if (interactionSound == null)
                interactionSound = controllingDoor.doorOpen;
        }

        // Play once at the interacted doorway, falling back to its controlling door.
        if (controllingDoor.ToggleDoor(player) && interactionSound != null)
            interactionSound.Play();
    }

    private bool ToggleDoor(PlayerItemInteraction player)
    {
        if (isLocked)
        {
            if (requiredKey == null || player == null || player.Inventory == null ||
                !player.Inventory.HasItem(requiredKey))
                return false;

            SetLocked(false);
        }

        SetOpen(!isOpen);
        return true;
    }

    public void SetOpen(bool open)
    {
        if (stateSource != null)
        {
            stateSource.SetOpen(open);
            return;
        }

        if (open && isLocked)
            return;

        isOpen = open;
    }

    public void SetLocked(bool locked)
    {
        if (stateSource != null)
        {
            stateSource.SetLocked(locked);
            return;
        }

        isLocked = locked;

        if (isLocked)
            SetOpen(false);
    }

    private void OnValidate()
    {
        var visited = new HashSet<DoorInteractable> { this };
        for (DoorInteractable source = stateSource; source != null; source = source.stateSource)
        {
            if (visited.Add(source))
                continue;

            stateSource = null;
            Debug.LogWarning("Door state sources cannot form a circular link.", this);
            break;
        }
    }
}
