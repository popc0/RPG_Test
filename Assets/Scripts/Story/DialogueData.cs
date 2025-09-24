using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一段劇情的資料容器（可在 Project 視窗右鍵建立）
/// </summary>
[CreateAssetMenu(menuName = "Story/Dialogue Data", fileName = "NewDialogueData")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public class Choice
    {
        public string text;        // 選項文字
        public string gotoNodeId;  // 按下後跳到哪個節點（nodeId）
    }

    [Serializable]
    public class Line
    {
        [Header("流程")]
        public string nodeId = "start";  // 這一句的節點 id（需唯一）
        public string nextNodeId = "";   // 沒選項時走這個（留空代表結束）

        [Header("內容")]
        public string displayName;       // UI 顯示的角色名
        [TextArea(2, 6)]
        public string content;           // 台詞

        [Header("多人講話")]
        public List<string> speakerIds = new List<string>(); // 同句可多位，之後拿來做排序提到最上層

        [Header("選項（如有，將覆蓋 nextNodeId 流程）")]
        public List<Choice> choices = new List<Choice>();

        [Header("自動播放")]
        public float autoDelay = 1.2f;   // 勾自動播放時，這句停留的秒數
    }

    [Header("全段設定")]
    public string dialogueId = "dlg_001";
    [Tooltip("打字機每秒字數；<=0 表示直接顯示完整句")]
    public float typewriterCharsPerSec = 30f;

    [Header("內容")]
    public List<Line> lines = new List<Line>();

    /// <summary> 依 nodeId 找句子；找不到回傳 null。 </summary>
    public Line Find(string nodeId)
    {
        return lines.Find(l => l.nodeId == nodeId);
    }
}
