using UnityEngine;
using UnityEditor;
using RPG;

[CustomEditor(typeof(SkillData))]
[CanEditMultipleObjects]
public class SkillDataEditor : Editor
{
    
    // --- 屬性變數 ---
    SerializedProperty type, rank, familySerial, skillID;
    SerializedProperty nextEvolution, sequence;
    SerializedProperty skillName, icon, baseCooldown, baseMpCost, castTime, recoveryTime;
    // ★ 新增：瞄準追蹤
    SerializedProperty trackFirePoint, trackAimDirection;

    // 門檻
    SerializedProperty useAttackReq, reqAttackMin, useAttackCap, reqAttackMax;
    // 門檻 - 防禦
    SerializedProperty useDefenseReq, reqDefenseMin, useDefenseCap, reqDefenseMax;
    // 門檻 - 敏捷
    SerializedProperty useAgilityReq, reqAgilityMin, useAgilityCap, reqAgilityMax;
    // 門檻 - 技巧
    SerializedProperty useTechniqueReq, reqTechniqueMin, useTechniqueCap, reqTechniqueMax;
    // 門檻 - HP
    SerializedProperty useHPReq, reqHPMin, useHPCap, reqHPMax;
    // 門檻 - MP
    SerializedProperty useMPReq, reqMPMin, useMPCap, reqMPMax;

    // 戰鬥
    SerializedProperty targetProp, hitType, targetLayer, baseDamage, baseRange;
    SerializedProperty baseAreaRadius, baseConeAngle;

    // 投射物
    SerializedProperty  projectilePrefab, projectileSpeed;
    // 狀態效果變數
    SerializedProperty useCastingStatus, castingStatusEffects;
    SerializedProperty useActingStatus, actingStatusEffects;
    SerializedProperty useRecoveryStatus, recoveryStatusEffects;

    void OnEnable()
    {
        // 綁定資料欄位
        type = serializedObject.FindProperty("type");
        rank = serializedObject.FindProperty("rank");
        familySerial = serializedObject.FindProperty("familySerial");
        skillID = serializedObject.FindProperty("skillID");
        nextEvolution = serializedObject.FindProperty("nextEvolution");
        sequence = serializedObject.FindProperty("sequence");

        skillName = serializedObject.FindProperty("SkillName");
        icon = serializedObject.FindProperty("Icon");
        baseCooldown = serializedObject.FindProperty("BaseCooldown");
        baseMpCost = serializedObject.FindProperty("BaseMpCost");
        castTime = serializedObject.FindProperty("CastTime");

        // 綁定新的 RecoveryTime
        recoveryTime = serializedObject.FindProperty("RecoveryTime");

        // ★ 綁定新增的變數
        trackFirePoint = serializedObject.FindProperty("TrackFirePoint");
        trackAimDirection = serializedObject.FindProperty("TrackAimDirection");

        // 攻擊
        useAttackReq = serializedObject.FindProperty("UseAttackReq");
        reqAttackMin = serializedObject.FindProperty("ReqAttackMin");
        useAttackCap = serializedObject.FindProperty("UseAttackCap");
        reqAttackMax = serializedObject.FindProperty("ReqAttackMax");
        // 防禦
        useDefenseReq = serializedObject.FindProperty("UseDefenseReq");
        reqDefenseMin = serializedObject.FindProperty("ReqDefenseMin");
        useDefenseCap = serializedObject.FindProperty("UseDefenseCap");
        reqDefenseMax = serializedObject.FindProperty("ReqDefenseMax");
        // 敏捷
        useAgilityReq = serializedObject.FindProperty("UseAgilityReq");
        reqAgilityMin = serializedObject.FindProperty("ReqAgilityMin");
        useAgilityCap = serializedObject.FindProperty("UseAgilityCap");
        reqAgilityMax = serializedObject.FindProperty("ReqAgilityMax");
        // 技巧
        useTechniqueReq = serializedObject.FindProperty("UseTechniqueReq");
        reqTechniqueMin = serializedObject.FindProperty("ReqTechniqueMin");
        useTechniqueCap = serializedObject.FindProperty("UseTechniqueCap");
        reqTechniqueMax = serializedObject.FindProperty("ReqTechniqueMax");
        // HP
        useHPReq = serializedObject.FindProperty("UseHPReq");
        reqHPMin = serializedObject.FindProperty("ReqHPMin");
        useHPCap = serializedObject.FindProperty("UseHPCap");
        reqHPMax = serializedObject.FindProperty("ReqHPMax");
        // MP
        useMPReq = serializedObject.FindProperty("UseMPReq");
        reqMPMin = serializedObject.FindProperty("ReqMPMin");
        useMPCap = serializedObject.FindProperty("UseMPCap");
        reqMPMax = serializedObject.FindProperty("ReqMPMax");

        targetProp = serializedObject.FindProperty("Target");
        hitType = serializedObject.FindProperty("HitType");
        targetLayer = serializedObject.FindProperty("TargetLayer");
        baseDamage = serializedObject.FindProperty("BaseDamage");
        baseRange = serializedObject.FindProperty("BaseRange");
        baseAreaRadius = serializedObject.FindProperty("BaseAreaRadius");
        baseConeAngle = serializedObject.FindProperty("BaseConeAngle");

        projectilePrefab = serializedObject.FindProperty("ProjectilePrefab");
        projectileSpeed = serializedObject.FindProperty("ProjectileSpeed");

        // 綁定新的狀態效果屬性
        useCastingStatus = serializedObject.FindProperty("UseCastingStatus");
        castingStatusEffects = serializedObject.FindProperty("CastingStatusEffects");

        useActingStatus = serializedObject.FindProperty("UseActingStatus");
        actingStatusEffects = serializedObject.FindProperty("ActingStatusEffects");

        useRecoveryStatus = serializedObject.FindProperty("UseRecoveryStatus");
        recoveryStatusEffects = serializedObject.FindProperty("RecoveryStatusEffects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // 開始更新

        // 取得目前的技能類型
        SkillType st = (SkillType)type.enumValueIndex;
        bool isPassive = (st == SkillType.Passive);
        bool isNormal = (st == SkillType.Normal);
        bool isUltimate =(st == SkillType.Ultimate);

        // ========================================================
        // 1. 識別區塊
        // ========================================================
        EditorGUILayout.LabelField("【 識別與 ID 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(type, new GUIContent("技能類型"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(rank, new GUIContent("階級 (Rank)"));

            // 唯讀顯示流水號
            GUI.enabled = false;
            EditorGUILayout.PropertyField(familySerial, new GUIContent("流水號 (Random)"));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(skillID, new GUIContent("Unique ID"));
            GUI.enabled = true;

            // --- 進化連結 ---
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(nextEvolution, new GUIContent("下一階進化"));

            // 建立進化版按鈕
            if (!serializedObject.isEditingMultipleObjects && nextEvolution.objectReferenceValue == null)
            {
                if (GUILayout.Button("＋建立進化版", GUILayout.Width(85)))
                {
                    SkillAssetTool.CreateEvolutionAsset((SkillData)target);
                }
            }
            EditorGUILayout.EndHorizontal();

            // ============================================================
            // ★ 修改：重新產生按鈕 (連動整組)
            // ============================================================
            if (GUILayout.Button("重新產生家族流水號 (Regenerate Family ID)"))
            {
                string msg = "確定要重新產生此技能的流水號嗎？\n\n" +
                             "★ 注意：所有屬於同一家族 (同流水號) 的技能都會一起更新！\n" +
                             "這將改變它們的 ID，請確保沒有存檔正在使用舊 ID。";

                if (EditorUtility.DisplayDialog("整組更新警告", msg, "確定更新", "取消"))
                {
                    // 呼叫批次更新方法
                    SkillAssetTool.RegenerateFamilySerial((SkillData)target);
                }
            }
        }
        EditorGUILayout.Space();
        // ========================================================
        // 2. 基本參數
        // ========================================================
        EditorGUILayout.LabelField("【 基本參數 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(skillName);
            EditorGUILayout.PropertyField(icon);

            if (!isPassive)
            {
                EditorGUILayout.PropertyField(baseCooldown, new GUIContent("冷卻 (秒)"));
                EditorGUILayout.PropertyField(baseMpCost, new GUIContent("MP 消耗"));

                EditorGUILayout.PropertyField(castTime, new GUIContent("詠唱時間 (Cast Time)"));
                EditorGUILayout.PropertyField(recoveryTime, new GUIContent("後搖時間 (Recovery Time)")); 
            }
        }
        EditorGUILayout.Space();

        // ========================================================
        // ★ 新增區塊：瞄準與追蹤
        // ========================================================
        if (!isPassive)
        {
            EditorGUILayout.LabelField("【 瞄準與追蹤 】", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(trackFirePoint, new GUIContent("跟隨施放點 (移動)"));
                if (trackFirePoint.boolValue)
                {
                    EditorGUILayout.HelpBox("角色移動時，發射點會跟著移動 (適合邊跑邊射)。", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("詠唱結束後鎖定位置 (適合定點轟炸)。", MessageType.None);
                }

                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(trackAimDirection, new GUIContent("跟隨瞄準方向 (旋轉)"));
                if (trackAimDirection.boolValue)
                {
                    EditorGUILayout.HelpBox("滑鼠/搖桿轉動時，彈道會跟著轉 (適合掃射)。", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("詠唱結束後鎖定方向 (適合狙擊)。", MessageType.None);
                }
            }
            EditorGUILayout.Space();
        }
        // ========================================================
        // 3. 學習門檻 (條件式顯示)
        // ========================================================
        EditorGUILayout.LabelField("【 學習門檻 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawRequirement("攻擊力 (Attack)", useAttackReq, reqAttackMin, useAttackCap, reqAttackMax);
            DrawRequirement("防禦力 (Defense)", useDefenseReq, reqDefenseMin, useDefenseCap, reqDefenseMax);
            DrawRequirement("敏捷 (Agility)", useAgilityReq, reqAgilityMin, useAgilityCap, reqAgilityMax);
            DrawRequirement("技巧 (Technique)", useTechniqueReq, reqTechniqueMin, useTechniqueCap, reqTechniqueMax);
            DrawRequirement("HP (Health)", useHPReq, reqHPMin, useHPCap, reqHPMax);
            DrawRequirement("MP (Mana)", useMPReq, reqMPMin, useMPCap, reqMPMax);
        }
        EditorGUILayout.Space();

        // ========================================================
        // 4. 戰鬥參數 (根據 HitType 變換)
        // ========================================================
        if (!isPassive)
        {
            EditorGUILayout.LabelField("【 戰鬥執行 】", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(targetProp);
                EditorGUILayout.PropertyField(targetLayer);
                EditorGUILayout.PropertyField(baseDamage);

                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(hitType);

                HitType hit = (HitType)hitType.enumValueIndex;

                // 根據類型顯示不同欄位
                if (hit == HitType.Single)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("最大射程"));

                    EditorGUILayout.Space(5);

                    using (new EditorGUI.IndentLevelScope()) {
                        EditorGUILayout.LabelField("--- 投射物設定 ---", EditorStyles.miniLabel); // 加個小標題區隔
                        EditorGUILayout.PropertyField(projectilePrefab);
                        EditorGUILayout.PropertyField(projectileSpeed);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("IsPiercing"), new GUIContent("是否穿透"));
                        // 顯示自動計算的時間 (設為唯讀)
                        GUI.enabled = false;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("ProjectileDuration"), new GUIContent("飛行時間 (後搖可參考)"));
                        GUI.enabled = true;
                        }
                }
                else if (hit == HitType.Area)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("施法距離"));
                    EditorGUILayout.PropertyField(baseAreaRadius, new GUIContent("爆炸半徑 (Radius)"));
                }
                else if (hit == HitType.Cone)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("扇形長度"));
                    EditorGUILayout.PropertyField(baseConeAngle, new GUIContent("扇形角度 (Angle)"));
                }
            }
            EditorGUILayout.Space();
        }

        // ========================================================
        // 5. 狀態效果 (Status Effects) 
        // ========================================================
        EditorGUILayout.LabelField("【 狀態效果 (Status Effects) 】", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("連結 StatusData 來定義施法期間的特殊狀態 (如：移動限制、施法限制等)", MessageType.Info);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
            // 詠唱：非被動才顯示
            if (!isPassive)
            {
                DrawStatusEffectList("Casting Status (詠唱)", useCastingStatus, castingStatusEffects);
            }
            // 執行：始終顯示 (被動技通常用這個來掛常駐效果)
            string actingLabel = isPassive ? "Passive Status (被動常駐)" : "Acting Status (執行/動作)";
            DrawStatusEffectList(actingLabel, useActingStatus, actingStatusEffects);
            // 後搖：非被動才顯示
            if (!isPassive)
            {
                DrawStatusEffectList("Recovery Status (後搖/復原)", useRecoveryStatus, recoveryStatusEffects);
            }
        }
        EditorGUILayout.Space();
        // ========================================================
        // 6. 技能排程 (Sequence)
        // ========================================================
        if (!isPassive)
        {
            EditorGUILayout.LabelField("【 後續排程 】", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("設定技能施放後的接續動作 (Delay + Skill)", MessageType.Info);

            // 使用預設的 List 繪製方式
            EditorGUILayout.PropertyField(sequence, true);
        }
        serializedObject.ApplyModifiedProperties(); // 應用所有更動
    }

    // 輔助繪製方法：畫出一整組門檻設定 (啟用勾選 + 數值 + 上限勾選 + 上限數值)
    void DrawRequirement(string label, SerializedProperty use, SerializedProperty min, SerializedProperty useCap, SerializedProperty max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(use, GUIContent.none, GUILayout.Width(20)); // 勾選框
        EditorGUILayout.LabelField(label, GUILayout.Width(100)); // 標籤名
        EditorGUILayout.EndHorizontal();
        if (use.boolValue)
        {
            // 顯示最小值輸入框
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
            EditorGUILayout.PropertyField(min, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            // 顯示上限勾選
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("上限", GUILayout.Width(35));
            EditorGUILayout.PropertyField(useCap, GUIContent.none, GUILayout.Width(20));

            if (useCap.boolValue)
            {
                // 顯示最大值輸入框
                EditorGUILayout.PropertyField(max, GUIContent.none);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    // 新增輔助繪製方法，用來畫出啟用勾選和狀態列表
    void DrawStatusEffectList(string label, SerializedProperty useProp, SerializedProperty listProp)
    {
        EditorGUILayout.BeginHorizontal();
        // 繪製啟用勾選框
        EditorGUILayout.PropertyField(useProp, new GUIContent(label), GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();

        // 如果啟用被勾選，則在一個內嵌的區塊中繪製 List
        if (useProp.boolValue)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUI.IndentLevelScope()) // 增加縮排讓列表看起來是從屬於上方的勾選
            {
                EditorGUILayout.PropertyField(listProp, new GUIContent("效果列表"), true);
            }
        }
    }
}