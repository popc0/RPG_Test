using UnityEngine;
using UnityEditor;
using System.Reflection;
using RPG;

public static class ProjectileCreator
{
    // ====================================================================
    // 類型 1: Single (一般飛行投射物) -> 改用 ProjectileLinear
    // ====================================================================
    [MenuItem("Assets/Create/RPG/Projectile/Single (Flying)", false, 0)]
    public static void CreateSingle()
    {
        // ★ 改用 ProjectileLinear
        CreateProjectileBase<ProjectileLinear>("Projectile_Single", (visual) =>
        {
            var col = visual.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
        });
    }

    // ====================================================================
    // 類型 2: Area (定點爆炸/範圍傷害) -> 改用 ProjectileArea
    // ====================================================================
    [MenuItem("Assets/Create/RPG/Projectile/Area (Explosion)", false, 1)]
    public static void CreateArea()
    {
        // ★ 改用 ProjectileArea
        CreateProjectileBase<ProjectileArea>("Projectile_Area", (visual) =>
        {
            var col = visual.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
        });
    }

    // ====================================================================
    // 類型 3: Cone (揮砍/扇形掃描) -> 暫時維持 Projectile2D (等待 Cone 重構)
    // ====================================================================
    [MenuItem("Assets/Create/RPG/Projectile/Cone (Slash)", false, 2)]
    public static void CreateCone()
    {
        // 1. Root (負責邏輯旋轉)
        GameObject root = new GameObject("Projectile_Cone");
        var proj = root.AddComponent<ProjectileCone>(); // ★ 改用新腳本

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        // 2. Pivot (負責修正圖片朝向 & 作為旋轉軸心)
        GameObject pivot = new GameObject("Pivot_Adjuster");
        pivot.transform.SetParent(root.transform);
        pivot.transform.localPosition = Vector3.zero;

        // 3. Visual (負責實際顯示 & 碰撞範圍)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(pivot.transform);
        // ★ 預設往 X 軸推 1.0，這樣一生成就能看到「半徑」的效果
        visual.transform.localPosition = new Vector3(1f, 0f, 0f);

        visual.AddComponent<SpriteRenderer>();
        var col = visual.AddComponent<PolygonCollider2D>();
        col.isTrigger = true;

        // 4. Link (讓程式碼知道要旋轉哪個物件來修正朝向)
        proj.modelTransform = pivot.transform;

        // 5. Save
        SavePrefab(root, "Projectile_Cone");

        Debug.Log("已建立 ProjectileCone Prefab (結構: Root -> Pivot -> Visual)");
    }

    // ====================================================================
    // 核心共用邏輯 (泛型化 T : ProjectileBase)
    // ====================================================================
    private static void CreateProjectileBase<T>(string defaultName, System.Action<GameObject> onAddCollider)
        where T : ProjectileBase // ★ 限制 T 必須是 ProjectileBase 的子類
    {
        // 1. Root
        GameObject root = new GameObject(defaultName);

        // ★ 動態掛上指定的 T 類型 (Linear 或 Area)
        var proj = root.AddComponent<T>();

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;

        // 2. Visual
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.AddComponent<SpriteRenderer>();

        // 3. Collider
        onAddCollider?.Invoke(visual);

        // 4. Link (這裡需要一點反射或類型檢查，因為 modelTransform 只有 Linear/Area 有)
        // 但因為我們目前 Linear 和 Area 都有定義 modelTransform，雖然是在子類別裡
        // 簡單做法：嘗試獲取
        // 注意：ProjectileBase 本身沒有 modelTransform 欄位，所以無法直接 proj.modelTransform = ...
        // 我們可以在這裡做個轉型判斷

        if (proj is ProjectileLinear linear)
        {
            linear.modelTransform = visual.transform;
        }
        else if (proj is ProjectileArea area)
        {
            area.modelTransform = visual.transform;
        }
        else if (proj is Projectile2D p2d)
        {
            p2d.modelTransform = visual.transform;
        }

        Debug.Log($"已建立 {typeof(T).Name} Prefab");

        // 5. Save
        SavePrefab(root, defaultName);
    }

    // ====================================================================
    // 存檔輔助 (保持不變)
    // ====================================================================
    private static void SavePrefab(GameObject root, string defaultName)
    {
        string path = GetActiveFolderPath();
        if (string.IsNullOrEmpty(path)) path = "Assets";

        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{defaultName}.prefab");

        PrefabUtility.SaveAsPrefabAsset(root, fullPath);
        GameObject.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

        Debug.Log($"<color=LightSeaGreen>已存檔: {fullPath}</color>");
    }

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