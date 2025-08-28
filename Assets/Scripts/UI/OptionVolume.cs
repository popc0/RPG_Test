using UnityEngine;
using UnityEngine.UI;

public class OptionVolume : MonoBehaviour
{
    [Range(0f, 1f)]
    public float volume = 1f;   // 初始音量（0~1）

    public void SetVolume(float value)
    {
        volume = value;
        AudioListener.volume = volume; // 全域音量
        Debug.Log($"音量調整為: {volume}");
    }
}
