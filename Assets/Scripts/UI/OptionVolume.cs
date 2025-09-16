using UnityEngine;
using UnityEngine.UI;

public class OptionVolume : MonoBehaviour
{
    [Range(0f, 1f)]
    public float volume = 1f;   // 初始音量（0~1）

    [Header("（可選）音量滑桿")]
    public Slider volumeSlider;

    void Start()
    {
        // 啟動時，若有存檔就讀出音量；否則用現值
        if (SaveSystem.TryLoad(out var data) && data != null && data.masterVolume >= 0f)
        {
            volume = Mathf.Clamp01(data.masterVolume);
            AudioListener.volume = volume;
        }
        else
        {
            // 沒存檔就以當前系統音量為準
            volume = Mathf.Clamp01(AudioListener.volume);
        }

        if (volumeSlider != null)
            volumeSlider.value = volume;
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        AudioListener.volume = volume; // 全域音量
        // Debug.Log($"音量調整為: {volume}");
    }
}
