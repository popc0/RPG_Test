using UnityEngine;
using UnityEditor;
using RPG;

[CustomEditor(typeof(SkillData))]
[CanEditMultipleObjects]
public class SkillDataEditor : Editor
{
    // ============================================================
    // 1. 變數宣告 (統一管理)
    // ============================================================

    // --- 識別 (Identity) ---
    SerializedProperty type;
    SerializedProperty rank;
    SerializedProperty familySerial;
    SerializedProperty skillID;
    SerializedProperty nextEvolution;

    // --- 基本參數 (Basic) ---
    SerializedProperty skillName;
    SerializedProperty icon;
    SerializedProperty baseCooldown;
    SerializedProperty baseMpCost;
    SerializedProperty castTime;
    SerializedProperty recoveryTime;
    SerializedProperty trackFirePoint;
    SerializedProperty trackAimDirection;

    // --- 學習門檻 (Requirements) ---
    SerializedProperty useAttackReq, reqAttackMin, useAttackCap, reqAttackMax;
    SerializedProperty useDefenseReq, reqDefenseMin, useDefenseCap, reqDefenseMax;
    SerializedProperty useAgilityReq, reqAgilityMin, useAgilityCap, reqAgilityMax;
    SerializedProperty useTechniqueReq, reqTechniqueMin, useTechniqueCap, reqTechniqueMax;
    SerializedProperty useHPReq, reqHPMin, useHPCap, reqHPMax;
    SerializedProperty useMPReq, reqMPMin, useMPCap, reqMPMax;

    // --- 戰鬥參數 (Combat) ---
    SerializedProperty targetProp; // Target
    SerializedProperty hitType;
    SerializedProperty targetLayer;
    SerializedProperty baseDamage;
    SerializedProperty baseRange;
    SerializedProperty baseAreaRadius;
    SerializedProperty baseConeAngle;

    // --- 投射物與揮舞 (Projectile & Swing) ---
    SerializedProperty projectilePrefab;
    SerializedProperty projectileSpeed;
    SerializedProperty isPiercing;
    SerializedProperty maxDuration;        // Editable (Duration/LifeTime)
    SerializedProperty swingDirection;

    // --- 狀態與排程 (Status & Sequence) ---
    SerializedProperty useCastingStatus, castingStatusEffects;
    SerializedProperty useActingStatus, actingStatusEffects;
    SerializedProperty useRecoveryStatus, recoveryStatusEffects;
    SerializedProperty sequence;

    // ============================================================
    // 2. 初始化 (OnEnable)
    // ============================================================
    void OnEnable()
    {
        // 1. Identity
        type = serializedObject.FindProperty("type");
        rank = serializedObject.FindProperty("rank");
        familySerial = serializedObject.FindProperty("familySerial");
        skillID = serializedObject.FindProperty("skillID");
        nextEvolution = serializedObject.FindProperty("nextEvolution");

        // 2. Basic
        skillName = serializedObject.FindProperty("SkillName");
        icon = serializedObject.FindProperty("Icon");
        baseCooldown = serializedObject.FindProperty("BaseCooldown");
        baseMpCost = serializedObject.FindProperty("BaseMpCost");
        castTime = serializedObject.FindProperty("CastTime");
        recoveryTime = serializedObject.FindProperty("RecoveryTime");
        trackFirePoint = serializedObject.FindProperty("TrackFirePoint");
        trackAimDirection = serializedObject.FindProperty("TrackAimDirection");

        // 3. Requirements (Batch)
        useAttackReq = serializedObject.FindProperty("UseAttackReq");
        reqAttackMin = serializedObject.FindProperty("ReqAttackMin");
        useAttackCap = serializedObject.FindProperty("UseAttackCap");
        reqAttackMax = serializedObject.FindProperty("ReqAttackMax");

        useDefenseReq = serializedObject.FindProperty("UseDefenseReq");
        reqDefenseMin = serializedObject.FindProperty("ReqDefenseMin");
        useDefenseCap = serializedObject.FindProperty("UseDefenseCap");
        reqDefenseMax = serializedObject.FindProperty("ReqDefenseMax");

        useAgilityReq = serializedObject.FindProperty("UseAgilityReq");
        reqAgilityMin = serializedObject.FindProperty("ReqAgilityMin");
        useAgilityCap = serializedObject.FindProperty("UseAgilityCap");
        reqAgilityMax = serializedObject.FindProperty("ReqAgilityMax");

        useTechniqueReq = serializedObject.FindProperty("UseTechniqueReq");
        reqTechniqueMin = serializedObject.FindProperty("ReqTechniqueMin");
        useTechniqueCap = serializedObject.FindProperty("UseTechniqueCap");
        reqTechniqueMax = serializedObject.FindProperty("ReqTechniqueMax");

        useHPReq = serializedObject.FindProperty("UseHPReq");
        reqHPMin = serializedObject.FindProperty("ReqHPMin");
        useHPCap = serializedObject.FindProperty("UseHPCap");
        reqHPMax = serializedObject.FindProperty("ReqHPMax");

        useMPReq = serializedObject.FindProperty("UseMPReq");
        reqMPMin = serializedObject.FindProperty("ReqMPMin");
        useMPCap = serializedObject.FindProperty("UseMPCap");
        reqMPMax = serializedObject.FindProperty("ReqMPMax");

        // 4. Combat
        targetProp = serializedObject.FindProperty("Target");
        hitType = serializedObject.FindProperty("HitType");
        targetLayer = serializedObject.FindProperty("TargetLayer");
        baseDamage = serializedObject.FindProperty("BaseDamage");
        baseRange = serializedObject.FindProperty("BaseRange");
        baseAreaRadius = serializedObject.FindProperty("BaseAreaRadius");
        baseConeAngle = serializedObject.FindProperty("BaseConeAngle");

        // 5. Projectile
        projectilePrefab = serializedObject.FindProperty("ProjectilePrefab");
        projectileSpeed = serializedObject.FindProperty("ProjectileSpeed");
        isPiercing = serializedObject.FindProperty("IsPiercing");
        maxDuration = serializedObject.FindProperty("MaxDuration");
        swingDirection = serializedObject.FindProperty("SwingDirection");

        // 6. Status & Sequence
        useCastingStatus = serializedObject.FindProperty("UseCastingStatus");
        castingStatusEffects = serializedObject.FindProperty("CastingStatusEffects");
        useActingStatus = serializedObject.FindProperty("UseActingStatus");
        actingStatusEffects = serializedObject.FindProperty("ActingStatusEffects");
        useRecoveryStatus = serializedObject.FindProperty("UseRecoveryStatus");
        recoveryStatusEffects = serializedObject.FindProperty("RecoveryStatusEffects");
        sequence = serializedObject.FindProperty("sequence");
    }

    // ============================================================
    // 3. 繪製介面 (OnInspectorGUI)
    // ============================================================
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SkillType st = (SkillType)type.enumValueIndex;
        bool isPassive = (st == SkillType.Passive);

        // --- 區塊 1: 識別與 ID ---
        EditorGUILayout.LabelField("【 識別與 ID 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(type, new GUIContent("技能類型"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(rank, new GUIContent("階級 (Rank)"));
            GUI.enabled = false;
            EditorGUILayout.PropertyField(familySerial, new GUIContent("流水號 (Random)"));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(skillID, new GUIContent("Unique ID"));
            GUI.enabled = true;

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(nextEvolution, new GUIContent("下一階進化"));
            if (!serializedObject.isEditingMultipleObjects && nextEvolution.objectReferenceValue == null)
            {
                if (GUILayout.Button("＋建立進化版", GUILayout.Width(85)))
                {
                    SkillAssetTool.CreateEvolutionAsset((SkillData)target);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("重新產生家族流水號 (Regenerate Family ID)"))
            {
                string msg = "確定要重新產生此技能的流水號嗎？\n\n" +
                             "★ 注意：所有屬於同一家族 (同流水號) 的技能都會一起更新！\n" +
                             "這將改變它們的 ID，請確保沒有存檔正在使用舊 ID。";
                if (EditorUtility.DisplayDialog("整組更新警告", msg, "確定更新", "取消"))
                {
                    SkillAssetTool.RegenerateFamilySerial((SkillData)target);
                }
            }
        }
        EditorGUILayout.Space();

        // --- 區塊 2: 基本參數 ---
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

        // --- 區塊 3: 瞄準與追蹤 (非被動) ---
        if (!isPassive)
        {
            EditorGUILayout.LabelField("【 瞄準與追蹤 】", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(trackFirePoint, new GUIContent("跟隨施放點 (移動)"));
                if (trackFirePoint.boolValue)
                    EditorGUILayout.HelpBox("角色移動時，發射點會跟著移動 (適合邊跑邊射)。", MessageType.None);
                else
                    EditorGUILayout.HelpBox("詠唱結束後鎖定位置 (適合定點轟炸)。", MessageType.None);

                EditorGUILayout.Space(2);

                EditorGUILayout.PropertyField(trackAimDirection, new GUIContent("跟隨瞄準方向 (旋轉)"));
                if (trackAimDirection.boolValue)
                    EditorGUILayout.HelpBox("滑鼠/搖桿轉動時，彈道會跟著轉 (適合掃射)。", MessageType.None);
                else
                    EditorGUILayout.HelpBox("詠唱結束後鎖定方向 (適合狙擊)。", MessageType.None);
            }
            EditorGUILayout.Space();
        }

        // --- 區塊 4: 學習門檻 ---
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

        // --- 區塊 5: 戰鬥執行 (核心) ---
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

                if (hit == HitType.Single)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("最大射程"));
                    EditorGUILayout.Space(5);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("--- 投射物設定 (Single) ---", EditorStyles.miniLabel);
                        EditorGUILayout.PropertyField(projectilePrefab);

                        if (projectilePrefab.objectReferenceValue != null)
                        {
                            EditorGUILayout.PropertyField(projectileSpeed, new GUIContent("飛行速度 (m/s)"));
                            EditorGUILayout.PropertyField(isPiercing, new GUIContent("是否穿透"));

                            // ★ Single: 唯讀顯示 (由 Range/Speed 算出)
                            GUI.enabled = false;
                            EditorGUILayout.PropertyField(maxDuration, new GUIContent("飛行時間 (自動計算)"));
                            GUI.enabled = true;
                        }
                    }
                }
                else if (hit == HitType.Area)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("施法距離"));
                    EditorGUILayout.PropertyField(baseAreaRadius, new GUIContent("爆炸半徑 (Radius)"));
                    EditorGUILayout.Space(5);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("--- 投射物設定 (Area) ---", EditorStyles.miniLabel);
                        EditorGUILayout.PropertyField(projectilePrefab, new GUIContent("爆炸 Prefab"));
                        if (projectilePrefab.objectReferenceValue != null)
                        {
                            EditorGUILayout.PropertyField(isPiercing, new GUIContent("是否穿透 (建議 True)"));
                            EditorGUILayout.PropertyField(maxDuration, new GUIContent("持續時間 (MaxDuration)"));
                        }
                    }
                }
                else if (hit == HitType.Cone)
                {
                    EditorGUILayout.PropertyField(baseRange, new GUIContent("扇形半徑 (刀長)"));
                    EditorGUILayout.PropertyField(baseConeAngle, new GUIContent("扇形角度 (總角度)"));
                    EditorGUILayout.Space(5);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("--- 投射物設定 (Cone/Slash) ---", EditorStyles.miniLabel);
                        EditorGUILayout.PropertyField(projectilePrefab, new GUIContent("揮砍 Prefab"));

                        if (projectilePrefab.objectReferenceValue != null)
                        {
                            EditorGUILayout.PropertyField(swingDirection, new GUIContent("揮舞方向"));
                            EditorGUILayout.PropertyField(projectileSpeed, new GUIContent("旋轉速度 (度/秒)"));
                            EditorGUILayout.PropertyField(isPiercing, new GUIContent("是否穿透 (建議 True)"));

                            // ★ Cone: 唯讀顯示 (由 Angle/Speed 算出)
                            GUI.enabled = false;
                            EditorGUILayout.PropertyField(maxDuration, new GUIContent("存活時間 (Auto Sync)"));
                            GUI.enabled = true;
                        }
                    }
                }
            }
            EditorGUILayout.Space();
        }

        // --- 區塊 6: 狀態效果 ---
        EditorGUILayout.LabelField("【 狀態效果 (Status Effects) 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (!isPassive) DrawStatusEffectList("Casting Status (詠唱)", useCastingStatus, castingStatusEffects);

            string actingLabel = isPassive ? "Passive Status (被動常駐)" : "Acting Status (執行/動作)";
            DrawStatusEffectList(actingLabel, useActingStatus, actingStatusEffects);

            if (!isPassive) DrawStatusEffectList("Recovery Status (後搖/復原)", useRecoveryStatus, recoveryStatusEffects);
        }
        EditorGUILayout.Space();

        // --- 區塊 7: 排程 ---
        if (!isPassive)
        {
            EditorGUILayout.LabelField("【 後續排程 】", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("設定技能施放後的接續動作 (Delay + Skill)", MessageType.Info);
            EditorGUILayout.PropertyField(sequence, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // --- 輔助方法 ---
    void DrawRequirement(string label, SerializedProperty use, SerializedProperty min, SerializedProperty useCap, SerializedProperty max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(use, GUIContent.none, GUILayout.Width(20));
        EditorGUILayout.LabelField(label, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        if (use.boolValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min:", GUILayout.Width(30));
            EditorGUILayout.PropertyField(min, GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("上限", GUILayout.Width(35));
            EditorGUILayout.PropertyField(useCap, GUIContent.none, GUILayout.Width(20));
            if (useCap.boolValue) EditorGUILayout.PropertyField(max, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
    }

    void DrawStatusEffectList(string label, SerializedProperty useProp, SerializedProperty listProp)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(useProp, new GUIContent(label), GUILayout.Width(180));
        EditorGUILayout.EndHorizontal();
        if (useProp.boolValue)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(listProp, new GUIContent("效果列表"), true);
            }
        }
    }
}