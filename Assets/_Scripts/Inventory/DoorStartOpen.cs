using UnityEngine;

// Only attach to doors saved in their open pose. Restore the closed pose before
// DoorInteractable.Awake records it, then start open before the first frame.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(DoorInteractable))]
public class DoorStartOpen : MonoBehaviour
{
    [SerializeField] private Transform hinge;
    [SerializeField] private Vector3 closedLocalEulerAngles;

    private DoorInteractable door;
    private Quaternion openRotation;

    private void Awake()
    {
        door = GetComponent<DoorInteractable>();
        if (hinge == null)
            hinge = transform;

        openRotation = hinge.localRotation;
        hinge.localRotation = Quaternion.Euler(closedLocalEulerAngles);
    }

    private void Start()
    {
        if (door.IsLocked)
            return;

        door.SetOpen(true);
        hinge.localRotation = openRotation;
    }
}
