using UnityEngine;
using UnityEditor;
using System.Reflection; // 為了抓取當前路徑
using RPG; // 引用你的 Namespace

public static class ProjectileCreator
{
    [MenuItem("Assets/Create/RPG/Projectile Prefab", false, 0)]
    public static void CreateProjectile()
    {
        // 1. 建立一個暫時的 GameObject
        GameObject root = new GameObject("NewProjectile");

        // 2. 掛上 Projectile2D (並抓住參照)
        // ★ 修改：宣告變數 proj 來接住這個組件，等一下要設定它
        var proj = root.AddComponent<Projectile2D>();
        // 3. 掛上 Rigidbody2D (物理優化)
        // ★ 修正：設為 Kinematic，避免受重力影響掉下去，同時讓移動 collider 更有效率
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 4. 建立 Visual 子物件 (放圖片與碰撞)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.AddComponent<SpriteRenderer>(); // 方便你直接貼圖

        // 5. 掛上碰撞器 (設為 Trigger)
        // 你原本用 PolygonCollider2D，這是 OK 的，適合不規則形狀
        var col = visual.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;

        // ============================================================
        // ★ 新增：自動連結 Model Transform
        // ============================================================
        proj.modelTransform = visual.transform;
        Debug.Log("已自動將 Visual 子物件指定給 Projectile2D 的 Model Transform");

        // 6. 存檔流程
        string path = GetActiveFolderPath();
        if (string.IsNullOrEmpty(path)) path = "Assets";

        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewProjectile.prefab");

        PrefabUtility.SaveAsPrefabAsset(root, fullPath);
        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

        Debug.Log($"<color=LightSeaGreen>已建立投射物 Prefab (Kinematic): {fullPath}</color>");
    }

    // ★ 獨立的路徑抓取方法 (不需要依賴 SkillAssetTool)
    private static string GetActiveFolderPath()
    {
        System.Type projectWindowUtilType = typeof(ProjectWindowUtil);
        MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);

        if (getActiveFolderPath != null)
        {
            object obj = getActiveFolderPath.Invoke(null, null);
            return obj.ToString();
        }

        return "Assets";
    }
}