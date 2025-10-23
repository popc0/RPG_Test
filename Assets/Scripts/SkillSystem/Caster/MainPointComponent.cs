using UnityEngine;

namespace RPG
{
    /// <summary>掛在角色或敵人身上的主屬性容器</summary>
    public class MainPointComponent : MonoBehaviour
    {
        [Header("主屬性")]
        public float Attack = 100;
        public float Defense = 100;
        public float Agility = 100;
        public float Technique = 100;
        public float HPStat = 100;
        public float MPStat = 100;

        public MainPoint MP => new MainPoint
        {
            Attack = Attack,
            Defense = Defense,
            Agility = Agility,
            Technique = Technique,
            HPStat = HPStat,
            MPStat = MPStat
        };

        /// <summary>依防禦減傷（線性反向，或改 Balance）</summary>
        public float AfterDefense(float outgoingDamage)
        {
            // 線性反向（與你先前的共識）：實際減傷 = Defense * k
            // 用 Balance.DefenseReduction() 集中常數
            float reduceRatio = Balance.DefenseReduction(Defense);
            float final = Mathf.Max(0f, outgoingDamage * (1f - reduceRatio));
            return final;
        }
    }
}
