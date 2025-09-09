public interface IInteractable
{
    bool CanInteract();
    string GetPrompt();
    void Interact(UnityEngine.GameObject interactor);
}
