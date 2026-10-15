using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class WorldInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private UnityEvent onInteract = new UnityEvent();

    public void Interact(PlayerInventory inventory)
    {
        onInteract.Invoke();
    }
}