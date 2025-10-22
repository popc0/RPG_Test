using UnityEngine;
using System;

namespace RPG
{
    /// <summary>
    /// 可掛載的主屬性元件：主角、敵人都用同一個。
    /// 內含已定案的 MainPoint 公式；提供事件以便 HUD / 其他系統監聽。
    /// </summary>
    public class MainPointComponent : MonoBehaviour
    {
        [Header("主屬性")]
        public MainPoint MP = new MainPoint
        {
            Attack = 50,
            Defense = 20,
            Agility = 80,
            Technique = 80,
            HPStat = 20,
            MPStat = 20
        };

        public event Action OnMainPointChanged;

        // 便捷轉發（讀法更短）
        public float Attack => MP.Attack;
        public float Defense => MP.Defense;
        public float Agility => MP.Agility;
        public float Technique => MP.Technique;

        // 公式便捷取用（讓呼叫端更短）
        public float OutgoingDamage(float baseDamage) => MP.CalcOutgoingDamage(baseDamage);
        public float AfterDefense(float incoming) => MP.CalcIncomingAfterDefense(incoming);
        public float CooldownMul() => MP.CooldownMul();
        public float MpCostMul() => MP.MpCostMul();
        public float AreaScale() => MP.AreaScale();

#if UNITY_EDITOR
        private void OnValidate()
        {
            OnMainPointChanged?.Invoke();
        }
#endif
    }
}
