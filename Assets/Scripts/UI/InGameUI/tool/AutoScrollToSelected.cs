using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollToSelected : MonoBehaviour
{
    public float scrollSpeed = 10f; // 滾動的平滑速度

    private ScrollRect scrollRect;
    private RectTransform scrollRectTransform;
    private RectTransform contentPanel;
    private GameObject lastSelected;

    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        contentPanel = scrollRect.content;
    }

    void Update()
    {
        // 取得當前被 EventSystem 選取的物件
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // 如果沒有選取任何東西，或者選取的跟上次一樣，就不做運算
        if (currentSelected == null || currentSelected == lastSelected)
        {
            return;
        }

        // 檢查選取的物件是否是 Content 的子物件 (確認是在這個 ScrollView 裡面的按鈕)
        if (currentSelected.transform.IsChildOf(contentPanel))
        {
            // 開始計算並滾動
            UpdateScrollPosition(currentSelected.GetComponent<RectTransform>());
        }

        lastSelected = currentSelected;
    }

    void UpdateScrollPosition(RectTransform selectedTarget)
    {
        if (selectedTarget == null) return;

        // --- 計算目標位置 ---

        // 1. 將選取目標的座標轉換到 ScrollRect 的本地座標空間
        Vector3 targetPositionInScroll = scrollRectTransform.InverseTransformPoint(selectedTarget.position);

        // 2. 取得 Viewport 的高度 (可視範圍)
        float viewportHeight = scrollRectTransform.rect.height;

        // 3. 計算目標應該在的位置 (這裡我們試著讓按鈕置中)
        // targetPositionInScroll.y 是負值 (因為 UI 座標系通常向下為負)，所以要修正
        // 我們要移動 Content 的 anchoredPosition.y

        // 這裡需要一點數學：計算 Content 應該移動多少，才能讓 Target 出現在中間
        // 簡單的做法是利用 Content 的目前位置加上 Target 相對於 ScrollRect 的偏差

        float targetY = -selectedTarget.localPosition.y;

        // 修正：如果使用了 LayoutGroup，座標計算會比較複雜
        // 最穩定的方法是直接操作 NormalizedPosition，或者使用 Snap 算法
        // 下面使用 "Snap To Target" (瞬移或平滑移動 Content)

        Canvas.ForceUpdateCanvases(); // 強制更新 UI 佈局確保座標正確

        // 計算 Content 應該移動到的 Y 軸位置 (讓按鈕置中)
        // 公式： - (按鈕的 Local Y) - (Viewport 高度的一半) 
        // 注意：這取決於你的 Content Pivot 設定，假設 Pivot 是 (0.5, 1) Top-Center

        float newContentY = -selectedTarget.localPosition.y - (viewportHeight * 0.5f);

        // 限制滾動範圍，不要讓 Content 滾過頭 (露出空白背景)
        float minPosition = 0;
        float maxPosition = contentPanel.rect.height - viewportHeight;
        newContentY = Mathf.Clamp(newContentY, minPosition, maxPosition);

        // 直接設定位置 (如果要平滑效果，可以在 Update 裡用 Mathf.Lerp 處理)
        // 為了簡單演示，這裡直接使用平滑移動的 Coroutine 或在 Update 插值
        // 這裡我們先直接設定，看看效果
        // contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, newContentY);

        // --- 啟動平滑滾動 ---
        StartCoroutine(SmoothScroll(newContentY));
    }

    System.Collections.IEnumerator SmoothScroll(float targetY)
    {
        float duration = 0.2f; // 滾動時間
        float time = 0;
        Vector2 startPos = contentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetY);

        while (time < duration)
        {
            contentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        contentPanel.anchoredPosition = targetPos;
    }
}