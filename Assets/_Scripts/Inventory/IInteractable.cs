public interface IInteractable
{
    string Prompt { get; }
    void Interact(PlayerItemInteraction player);
}
