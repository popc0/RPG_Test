using UnityEngine;
using UnityEditor;
using RPG;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(StatusData))]
public class StatusDataEditor : Editor
{
    SerializedProperty statusID;
    SerializedProperty statusIcon;
    SerializedProperty effects;

    void OnEnable()
    {
        statusID = serializedObject.FindProperty("statusID");
        statusIcon = serializedObject.FindProperty("statusIcon");
        effects = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. 繪製基本資料
        EditorGUILayout.LabelField("【 狀態識別 】", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(statusID);
            EditorGUILayout.PropertyField(statusIcon);
        }

        EditorGUILayout.Space();

        // 2. 繪製效果清單
        EditorGUILayout.LabelField("【 效果清單 】", EditorStyles.boldLabel);

        // 顯示目前的清單內容
        for (int i = 0; i < effects.arraySize; i++)
        {
            SerializedProperty element = effects.GetArrayElementAtIndex(i);

            // 取得這個元素的實際型別名稱，用來當標題
            string typeName = "Unknown Effect";
            // 透過 managedReferenceFullTypename 取得型別字串
            string fullType = element.managedReferenceFullTypename;
            // 格式通常是 "Assembly Name TypeName"，我們取最後一段
            if (!string.IsNullOrEmpty(fullType))
            {
                var split = fullType.Split(' ');
                if (split.Length > 1)
                {
                    string rawName = split[split.Length - 1];
                    // 去掉 Namespace (RPG.)
                    typeName = rawName.Replace("RPG.", "");
                }
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.BeginHorizontal();
                // 標題 (例如 StatusControlEffect)
                EditorGUILayout.LabelField($"Effect {i + 1}: {typeName}", EditorStyles.boldLabel);

                // 刪除按鈕
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    effects.DeleteArrayElementAtIndex(i);
                    break; // 刪除後跳出迴圈避免索引錯誤
                }
                EditorGUILayout.EndHorizontal();

                // 繪製該效果的所有欄位
                EditorGUI.indentLevel++;

                // 使用 PropertyField 繪製物件內容，但要處理 managedReferenceValue 為 null 的情況
                // 如果已經有值，直接畫出來
                // 這裡使用 GetEnumerator 來遍歷子屬性，因為直接畫 element 可能會畫出折疊箭頭但沒有內容
                var copy = element.Copy();
                var endProp = element.GetEndProperty();
                bool enterChildren = true;

                while (copy.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(copy, endProp)) break;
                    EditorGUILayout.PropertyField(copy, true);
                    enterChildren = false;
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space(2);
        }

        // 3. 新增按鈕 (下拉選單)
        EditorGUILayout.Space();
        if (GUILayout.Button("＋ 新增效果 (Add Effect)"))
        {
            ShowAddEffectMenu();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ShowAddEffectMenu()
    {
        GenericMenu menu = new GenericMenu();

        // 使用 Reflection 找出所有繼承自 StatusEffectBase 的非抽象類別
        var effectTypes = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(StatusEffectBase).IsAssignableFrom(p) && !p.IsAbstract && p.IsClass);

        foreach (var type in effectTypes)
        {
            // 在選單中加入選項
            menu.AddItem(new GUIContent(type.Name), false, () => AddEffect(type));
        }

        menu.ShowAsContext();
    }

    void AddEffect(Type type)
    {
        // 1. 建立實例
        var instance = Activator.CreateInstance(type);

        // 2. 擴充陣列
        effects.arraySize++;

        // 3. 設定最後一個元素的 managedReferenceValue
        var element = effects.GetArrayElementAtIndex(effects.arraySize - 1);
        element.managedReferenceValue = instance;

        serializedObject.ApplyModifiedProperties();
    }
}