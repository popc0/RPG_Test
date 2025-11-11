using UnityEngine;

/// <summary>
/// 只根據 PlayerMoveControl.LastMove 來決定顯示哪個模型/播放哪個動畫。
/// 掛在 Player/Move_Core 上。
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("來源：移動控制 (同物件或父層)")]
    public PlayerMoveControl moveControl;

    [Header("模型/動畫物件")]
    public GameObject modelDownWalk;
    public GameObject modelUp;
    public GameObject modelDownIdle;

    [Header("翻轉根物件")]
    public Transform flipTargetDownWalk;
    public Transform flipTargetUp;
    public Transform flipTargetDownIdle;

    [Header("動畫名稱")]
    public string walkRU = "walk_ru";
    public string walkLU = "walk_lu";
    public string walkRD = "walk_rd";
    public string walkLD = "walk_ld";
    public string idleRU = "idle_ru";
    public string idleLU = "idle_lu";
    public string idleRD = "idle_rd";
    public string idleLD = "idle_ld";

    [Header("待機延遲 (秒)")]
    public float idleBuffer = 0.08f;

    Animator animDownWalk, animUp, animDownIdle, currentAnim;
    string currentState; float idleTimer; Facing currentFacing = Facing.RD;
    int lastHorizSign = +1, lastVertSign = -1;

    private enum Facing { RU, LU, RD, LD }

    void Awake()
    {
        if (!moveControl) moveControl = GetComponent<PlayerMoveControl>();

        if (modelDownWalk) animDownWalk = modelDownWalk.GetComponentInChildren<Animator>(true);
        if (modelUp) animUp = modelUp.GetComponentInChildren<Animator>(true);
        if (modelDownIdle) animDownIdle = modelDownIdle.GetComponentInChildren<Animator>(true);

        ActivateOnly(modelDownWalk);
        currentAnim = animDownWalk;
    }

    void Update()
    {
        Vector2 delta = (moveControl != null) ? moveControl.LastMove : Vector2.zero;

        if (delta == Vector2.zero)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleBuffer) { ShowForIdle(currentFacing); PlayIdle(currentFacing); }
            return;
        }
        idleTimer = 0f;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            int sx = Mathf.Sign(delta.x) >= 0 ? +1 : -1;
            int sy = lastVertSign; lastHorizSign = sx;
            var f = ToFacing(sx, sy); ShowForWalk(f); PlayWalk(f);
        }
        else
        {
            int sy = Mathf.Sign(delta.y) >= 0 ? +1 : -1;
            int sx = lastHorizSign; lastVertSign = sy;
            var f = ToFacing(sx, sy); ShowForWalk(f); PlayWalk(f);
        }
    }

    Facing ToFacing(int sx, int sy)
    {
        return (sy >= 0) ? ((sx >= 0) ? Facing.RU : Facing.LU)
                       : ((sx >= 0) ? Facing.RD : Facing.LD);
    }

    void ShowForWalk(Facing f)
    {
        currentFacing = f;
        if (f == Facing.RU || f == Facing.LU) { ActivateOnly(modelUp); ApplyFlip(flipTargetUp, f == Facing.LU); currentAnim = animUp; }
        else { ActivateOnly(modelDownWalk); ApplyFlip(flipTargetDownWalk, f == Facing.LD); currentAnim = animDownWalk; }
    }
    void ShowForIdle(Facing f)
    {
        currentFacing = f;
        if (f == Facing.RU || f == Facing.LU) { ActivateOnly(modelUp); ApplyFlip(flipTargetUp, f == Facing.LU); currentAnim = animUp; }
        else { ActivateOnly(modelDownIdle); ApplyFlip(flipTargetDownIdle, f == Facing.LD); currentAnim = animDownIdle; }
    }

    void PlayWalk(Facing f)
    {
        switch (f) { case Facing.RU: PlayOnce(walkRU); break; case Facing.LU: PlayOnce(walkLU); break; case Facing.RD: PlayOnce(walkRD); break; case Facing.LD: PlayOnce(walkLD); break; }
    }
    void PlayIdle(Facing f)
    {
        switch (f) { case Facing.RU: PlayOnce(idleRU); break; case Facing.LU: PlayOnce(idleLU); break; case Facing.RD: PlayOnce(idleRD); break; case Facing.LD: PlayOnce(idleLD); break; }
    }

    void PlayOnce(string state)
    {
        if (!currentAnim) return; if (currentState == state) return; currentAnim.Play(state); currentState = state;
    }

    void ApplyFlip(Transform t, bool flip)
    {
        if (!t) return; var s = t.localScale; float tx = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x); if (!Mathf.Approximately(s.x, tx)) { s.x = tx; t.localScale = s; }
    }

    void ActivateOnly(GameObject go)
    {
        if (modelDownWalk) modelDownWalk.SetActive(go == modelDownWalk);
        if (modelUp) modelUp.SetActive(go == modelUp);
        if (modelDownIdle) modelDownIdle.SetActive(go == modelDownIdle);
    }
}
