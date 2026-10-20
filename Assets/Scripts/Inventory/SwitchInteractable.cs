using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class SwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isOn;
    [SerializeField] private UnityEvent<bool> onChanged = new UnityEvent<bool>();

    public string Prompt => isOn ? "Turn off" : "Turn on";

    private void Start()
    {
        onChanged.Invoke(isOn);
    }

    public void Interact(PlayerItemInteraction player)
    {
        SetOn(!isOn);
    }

    public void SetOn(bool on)
    {
        if (isOn == on)
            return;

        isOn = on;
        onChanged.Invoke(isOn);
    }
}
