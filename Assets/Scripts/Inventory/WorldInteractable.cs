using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class WorldInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Interact";
    [SerializeField] private UnityEvent onInteract = new UnityEvent();

    public string Prompt => prompt;

    public void Interact(PlayerItemInteraction player)
    {
        onInteract.Invoke();
    }
}
