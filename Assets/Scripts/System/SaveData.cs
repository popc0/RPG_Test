using System;

[Serializable]
public class SaveData
{
    public string sceneName;
    public float playerX;
    public float playerY;
    public long savedAtUnix;   // 時間戳
    public float masterVolume; // 新增：主音量(0~1)
    // 新增：戰鬥數值
    public float playerHP;
    public float playerMP;
    public float playerMaxHP;
    public float playerMaxMP;

    public SaveData() { }

    public SaveData(string scene, float x, float y)
    {
        sceneName = scene;
        playerX = x;
        playerY = y;
        savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        masterVolume = 1f; // 預設值
    }

    public SaveData(string scene, float x, float y, float volume)
        : this(scene, x, y)
    {
        masterVolume = volume;
    }
}
