using UnityEngine;

public enum UITriggerAction { ShowExclusive, Push, Pop }

public class UITrigger : MonoBehaviour
{
    public UITriggerAction action = UITriggerAction.Push;
    public string targetId; // ShowExclusive/Push ¥Î¡FPop ¥i¯dªÅ

    public void Invoke()
    {
        if (UIOrchestrator.I == null) return;
        switch (action)
        {
            case UITriggerAction.ShowExclusive:
                if (!string.IsNullOrEmpty(targetId)) UIOrchestrator.I.ShowExclusive(targetId);
                break;
            case UITriggerAction.Push:
                if (!string.IsNullOrEmpty(targetId)) UIOrchestrator.I.Push(targetId);
                break;
            case UITriggerAction.Pop:
                UIOrchestrator.I.Pop();
                break;
        }
    }
}
