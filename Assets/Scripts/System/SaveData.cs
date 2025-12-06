using System;
using System.Collections.Generic;

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
    //  新增：用於儲存 PageMain 的上次開啟頁面索引
    public int pageMainLastPageIndex = 0;

    // [新增] 等級系統資料
    public int playerLevel = 1;         // 等級
    public float playerCurrentExp = 0f;  // 當前經驗值
    public int playerUnspentPoints = 0;  // 未分配屬性點

    // -----------------------------------------------------------
    // [新增] 儲存已分配的屬性點 (只存加點值，不含基礎值)
    // -----------------------------------------------------------
    public float statAttack;
    public float statDefense;
    public float statAgility;
    public float statTechnique;
    public float statHPStat;
    public float statMPStat;

    // ============================================================
    // [新增] 技能系統存檔
    // ============================================================

    // 1. 固定技能 (存 skillID)
    public string fixedNormalSkillID;   
    public string fixedUltimateSkillID; 

    // 2. 技能組結構 (純資料版)
    [Serializable]
    public struct SavedSkillGroup
    {
        public string groupName;
        public string normalSkillID;   // Slot 1
        public string ultimateSkillID; // Slot 3
    }

    // 3. 技能組清單
    public List<SavedSkillGroup> skillGroups = new List<SavedSkillGroup>();
    public int currentSkillGroupIndex = 0;

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
