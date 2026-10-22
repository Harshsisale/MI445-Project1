using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float openSpeed = 120f;

    private Quaternion closedRotation;
    private bool isOpen;

    public string Prompt => isOpen ? "Close door" : "Open door";

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
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
    }
}
