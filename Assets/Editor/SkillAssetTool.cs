using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using RPG;

public static class SkillAssetTool
{
    // ============================================================
    // 功能 1：取得唯一的隨機流水號 (給 Inspector 按鈕用)
    // ============================================================
    public static int GetUniqueRandomSerial()
    {
        // 1. 收集目前專案中已用掉的號碼
        HashSet<int> existingSerials = new HashSet<int>();
        string[] guids = AssetDatabase.FindAssets("t:SkillData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (data != null)
            {
                existingSerials.Add(data.familySerial);
            }
        }

        // 2. 隨機生成直到不重複
        int newSerial = 0;
        int maxTry = 1000;
        do
        {
            newSerial = Random.Range(0, 999999);
            maxTry--;
        }
        while (existingSerials.Contains(newSerial) && maxTry > 0);

        if (maxTry <= 0) Debug.LogWarning("找不到可用的流水號，運氣也太好了吧？");

        return newSerial;
    }

    // ============================================================
    // 功能 2：建立進化版技能 (給 Inspector 按鈕用)
    // ============================================================
    public static void CreateEvolutionAsset(SkillData baseData)
    {
        if (baseData == null) return;

        // 1. 複製檔案
        SkillData newData = Object.Instantiate(baseData);

        // 2. 修改關鍵屬性
        newData.rank = baseData.rank + 1; // ★ Rank + 1
        newData.nextEvolution = null;     // 清空下一階
        newData.name = baseData.name;     // 暫存名字(用於生成檔名)

        // ★ 關鍵：手動更新 ID 字串，確保 ID 裡的 Rank 碼 (前兩位) 變成新的
        UpdateSkillID(newData);

        // 3. 決定路徑 (放在原檔案旁邊)
        string path = AssetDatabase.GetAssetPath(baseData);
        string dir = Path.GetDirectoryName(path);
        string baseName = Path.GetFileNameWithoutExtension(path);

        // 檔名加上 _Rx 後綴
        string filename = $"{baseName}_R{newData.rank}.asset";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{filename}");

        // 4. 寫入硬碟
        AssetDatabase.CreateAsset(newData, fullPath);

        // 5. 自動連結上一階
        baseData.nextEvolution = newData;
        EditorUtility.SetDirty(baseData);

        AssetDatabase.SaveAssets();
        Selection.activeObject = newData; // 自動選中新檔案

        Debug.Log($"<color=cyan>已建立進化技能：{filename} (ID: {newData.skillID})</color>");
    }

    // ============================================================
    // ★ 新增功能：批次更新整組家族的流水號
    // ============================================================
    public static void RegenerateFamilySerial(SkillData target)
    {
        if (target == null) return;

        // 1. 準備數據
        int oldSerial = target.familySerial;
        SkillType targetType = target.type;
        int newSerial = GetUniqueRandomSerial(); // 取得一個全新的亂數

        // 2. 搜尋所有 SkillData
        string[] guids = AssetDatabase.FindAssets("t:SkillData");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            // 條件：類型相同 且 流水號等於舊號碼 (代表是同一家族)
            if (data != null && data.type == targetType && data.familySerial == oldSerial)
            {
                // 3. 記錄 Undo (讓你可以 Ctrl+Z 後悔)
                Undo.RecordObject(data, "Regenerate Skill ID");

                // 4. 更新流水號
                data.familySerial = newSerial;

                // 5. 更新 ID 字串
                UpdateSkillID(data);

                // 6. 標記已修改
                EditorUtility.SetDirty(data);
                count++;
            }
        }

        // 7. 存檔
        AssetDatabase.SaveAssets();
        Debug.Log($"<color=Pink>已更新 {count} 個技能的流水號：{oldSerial} -> {newSerial}</color>");
    }

    // ============================================================
    // 輔助：更新 ID 字串格式 (Type + Rank + Serial)
    // ============================================================
    public static void UpdateSkillID(SkillData data)
    {
        if (data == null) return;
        string typeCode = "N";
        if (data.type == SkillType.Ultimate) typeCode = "U";
        if (data.type == SkillType.Passive) typeCode = "P";

        // 格式：Type(1) + Rank(2) + Serial(6)
        // 例如：N02839421 (Rank 2, 亂數 839421)
        data.skillID = $"{typeCode}{data.rank:D2}{data.familySerial:D6}";
    }
}