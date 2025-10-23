using UnityEngine;
using UnityEngine.UI;

namespace RPG
{
    /// <summary>開發期用：簡單把主屬性顯示在 UI Text 或 Console</summary>
    public class MainPointDebug : MonoBehaviour
    {
        public MainPointComponent main;
        public Text text;

        void Reset()
        {
            if (!main) main = GetComponentInParent<MainPointComponent>();
        }

        void Update()
        {
            if (!main) return;

            string s = $"ATK {main.Attack:0}  DEF {main.Defense:0}  AGI {main.Agility:0}  TEC {main.Technique:0}  HPs {main.HPStat:0}  MPs {main.MPStat:0}";
            if (text) text.text = s;
            else Debug.Log(s);
        }
    }
}
