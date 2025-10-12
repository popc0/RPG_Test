using UnityEngine;

[DisallowMultipleComponent]
public class UIPanelTag : MonoBehaviour
{
    [Header("唯一識別（專案內唯一）")]
    public string id;

    [Header("參考元件")]
    public CanvasGroup group;
    public GameObject defaultFocus;

    [Header("行為設定")]
    public UIOrchestrator.PanelType type = UIOrchestrator.PanelType.Page;
    public bool manageVisibility = true;
    public bool restoreOnDeactivate = true;

    void Reset() { group = GetComponent<CanvasGroup>(); }
    void Awake() { if (group == null) group = GetComponent<CanvasGroup>(); }

    void OnEnable()
    {
        if (UIOrchestrator.I == null) return;
        if (group == null) group = GetComponent<CanvasGroup>();

        var p = UIOrchestrator.I.panels.Find(x => x.id == id);
        if (p == null)
        {
            p = new UIOrchestrator.PanelRef
            {
                id = id,
                group = group,
                defaultFocus = defaultFocus,
                type = type,
                manageVisibility = manageVisibility,
                restoreOnDeactivate = restoreOnDeactivate
            };
            p.initAlpha = group.alpha; p.initInteractable = group.interactable; p.initBlocks = group.blocksRaycasts;
            UIOrchestrator.I.panels.Add(p);
        }
        else
        {
            p.group = group;
            p.defaultFocus = defaultFocus;
            p.type = type;
            p.manageVisibility = manageVisibility;
            p.restoreOnDeactivate = restoreOnDeactivate;
        }
    }

    void OnDisable()
    {
        if (UIOrchestrator.I == null) return;
        UIOrchestrator.I.panels.RemoveAll(x => x.id == id);
    }
}
