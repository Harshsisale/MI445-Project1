using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class SwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isOn;
    [Tooltip("Optional item that must be equipped to operate this switch.")]
    [SerializeField] private ItemDefinition requiredHeldItem;
    [SerializeField] private string missingItemPrompt = "Press Left Click to offer sacrifice";
    [SerializeField] private UnityEvent<bool> onChanged = new UnityEvent<bool>();

    public string Prompt => isOn ? "Turn off" : "Turn on";

    public string GetInteractionPrompt(PlayerItemInteraction player)
    {
        return IsMissingRequiredItem(player)
            ? missingItemPrompt
            : "Left click - " + Prompt;
    }

    private void Start()
    {
        onChanged.Invoke(isOn);
    }

    public void Interact(PlayerItemInteraction player)
    {
        if (IsMissingRequiredItem(player))
            return;

        SetOn(!isOn);
    }

    private bool IsMissingRequiredItem(PlayerItemInteraction player)
    {
        return requiredHeldItem != null && (player == null || player.HeldItem != requiredHeldItem);
    }

    public void SetOn(bool on)
    {
        if (isOn == on)
            return;

        isOn = on;
        onChanged.Invoke(isOn);
    }
}
