using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [TextArea] public string prompt = "«ö E ¤¬°Ê";
    public bool interactable = true;

    public virtual bool CanInteract() => interactable;
    public virtual string GetPrompt() => prompt;
    public abstract void Interact(GameObject interactor);
}
