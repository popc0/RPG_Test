using UnityEngine;

[CreateAssetMenu(menuName = "GameData/TeleporterData (Same Scene)")]
public class TeleporterData : ScriptableObject
{
    [TextArea] public string promptText = "按 E 傳送";
    // 後續若要擴充顯示圖示、音效、特效，放這裡
}
