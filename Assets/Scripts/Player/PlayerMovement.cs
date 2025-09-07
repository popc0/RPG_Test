using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;

    [Header("模型")]
    [SerializeField] private GameObject modelLR;      // 向下象限用（右下、左下）
    [SerializeField] private GameObject modelUp;      // 向上象限用（右上、左上）

    [Header("翻轉目標")]
    [SerializeField] private Transform flipTargetLR;  // LR 模型的可見根
    [SerializeField] private Transform flipTargetUp;  // Up 模型的可見根

    [Header("動畫狀態名稱（兩個 Animator 都要有相同命名）")]
    [SerializeField] private string walkRU = "walk_ru";
    [SerializeField] private string walkLU = "walk_lu";   // 會用翻轉得到
    [SerializeField] private string walkRD = "walk_rd";
    [SerializeField] private string walkLD = "walk_ld";   // 會用翻轉得到
    [SerializeField] private string idleRU = "idle_ru";
    [SerializeField] private string idleLU = "idle_lu";   // 會用翻轉得到
    [SerializeField] private string idleRD = "idle_rd";
    [SerializeField] private string idleLD = "idle_ld";   // 會用翻轉得到

    private Rigidbody2D rb;
    private Animator animLR;
    private Animator animUp;
    private Animator currentAnim;

    // 輸入僅負責移動；動畫判定用位移量
    private Vector2 input;
    private Vector2 lastPos;
    private Vector2 moveDelta;        // 這一幀的位移量（FixedUpdate 計算）
    private Vector2 lastDir = new Vector2(1f, -1f); // 初始右下

    // 紀錄上一次的水平、垂直象限（-1 = 左/下，+1 = 右/上）
    private int lastHorizSign = +1; // 初始向右
    private int lastVertSign = -1; // 初始向下

    // 目前所在象限（決定使用哪個模型與翻轉）
    private enum Quad { RU, LU, RD, LD }
    private Quad currentQuad = Quad.RD; // 初始右下
    private string currentState = null;  // 避免每幀重播

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (modelLR != null) animLR = modelLR.GetComponentInChildren<Animator>(true);
        if (modelUp != null) animUp = modelUp.GetComponentInChildren<Animator>(true);

        // 預設顯示 LR、隱藏 Up，並設當前 Animator
        UseModel(Quad.RD);

        // 若未指定 flip 目標，嘗試用各自 Animator 的 Transform
        if (flipTargetLR == null && animLR != null) flipTargetLR = animLR.transform;
        if (flipTargetUp == null && animUp != null) flipTargetUp = animUp.transform;

        lastPos = rb.position;
    }

    void Update()
    {
        // 你的移動輸入（可換成你現有的控制方案）
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        HandleAnimation(); // 動畫用上一個 FixedUpdate 的 moveDelta 決定
    }

    void FixedUpdate()
    {
        // 實際移動
        if (rb != null)
            rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);

        // 計算位移量（動畫用）
        var pos = rb.position;
        moveDelta = pos - lastPos;
        lastPos = pos;

        // 平滑：若移動量非常小，視為沒有移動
        if (moveDelta.sqrMagnitude < 0.0001f)
            moveDelta = Vector2.zero;
    }

    void HandleAnimation()
    {
        // 以位移量為主（非輸入）
        Vector2 dir = moveDelta;

        if (dir == Vector2.zero)
        {
            // 靜止：播放當前象限對應的待機
            PlayIdle(currentQuad);
            return;
        }

        // 判定主軸：X 為主或 Y 為主
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // X 軸移動：水平象限由當前 dir.x 決定；垂直象限沿用上一個值
            int sx = Mathf.Sign(dir.x) >= 0 ? +1 : -1;
            int sy = lastVertSign; // 沿用上一個「上/下」

            lastHorizSign = sx; // 更新水平記憶
            // 轉成象限並套用
            Quad q = ToQuad(sx, sy);
            UseModel(q);
            PlayWalk(q);
        }
        else
        {
            // Y 軸移動：垂直象限由當前 dir.y 決定；水平象限沿用上一個值
            int sy = Mathf.Sign(dir.y) >= 0 ? +1 : -1;
            int sx = lastHorizSign; // 沿用上一個「左/右」

            lastVertSign = sy; // 更新垂直記憶
            Quad q = ToQuad(sx, sy);
            UseModel(q);
            PlayWalk(q);
        }
    }

    // 把符號轉成象限
    Quad ToQuad(int sx, int sy)
    {
        // sx: +1=右, -1=左 ; sy: +1=上, -1=下
        if (sy >= 0)
            return (sx >= 0) ? Quad.RU : Quad.LU;
        else
            return (sx >= 0) ? Quad.RD : Quad.LD;
    }

    // 依象限選用模型並設定翻轉
    void UseModel(Quad q)
    {
        // 上象限用 Up 模型；下象限用 LR 模型
        bool useUp = (q == Quad.RU || q == Quad.LU);

        if (modelUp != null) modelUp.SetActive(useUp);
        if (modelLR != null) modelLR.SetActive(!useUp);

        currentAnim = useUp ? animUp : animLR;
        currentQuad = q;

        // 設定翻轉：左象限翻轉，右象限不翻轉
        bool isLeft = (q == Quad.LU || q == Quad.LD);
        if (useUp) ApplyFlip(flipTargetUp, isLeft);
        else ApplyFlip(flipTargetLR, isLeft);
    }

    // 播放走路
    void PlayWalk(Quad q)
    {
        switch (q)
        {
            case Quad.RU: PlayOnce(walkRU); break;
            case Quad.LU: PlayOnce(walkLU); break; // 用翻轉得到
            case Quad.RD: PlayOnce(walkRD); break;
            case Quad.LD: PlayOnce(walkLD); break; // 用翻轉得到
        }
    }

    // 播放待機
    void PlayIdle(Quad q)
    {
        switch (q)
        {
            case Quad.RU: PlayOnce(idleRU); break;
            case Quad.LU: PlayOnce(idleLU); break; // 用翻轉得到
            case Quad.RD: PlayOnce(idleRD); break;
            case Quad.LD: PlayOnce(idleLD); break; // 用翻轉得到
        }
    }

    // 僅在狀態改變時播放
    void PlayOnce(string stateName)
    {
        if (currentAnim == null) return;
        if (currentState == stateName) return;
        currentAnim.Play(stateName);
        currentState = stateName;
    }

    // 水平翻轉
    void ApplyFlip(Transform target, bool flip)
    {
        if (target == null) return;
        var s = target.localScale;
        float tx = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        if (!Mathf.Approximately(s.x, tx))
        {
            s.x = tx;
            target.localScale = s;
        }
    }
}
