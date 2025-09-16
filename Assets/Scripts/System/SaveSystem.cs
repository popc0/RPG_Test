using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // 之後要多存檔槽就改這裡（例如 slot2.json）
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "save_slot1.json");

    public static void Save(SaveData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
#if UNITY_EDITOR
            Debug.Log("[SaveSystem] Saved: " + FilePath);
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Save failed: " + e);
        }
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;
        try
        {
            if (!File.Exists(FilePath)) return false;
            var json = File.ReadAllText(FilePath);
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Load failed: " + e);
            return false;
        }
    }

    public static bool Exists() => File.Exists(FilePath);

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SaveSystem] Delete failed: " + e);
        }
    }
}
